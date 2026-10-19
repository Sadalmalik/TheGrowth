using System;
using System.Collections.Generic;
using System.Reflection;
using TheGrowth.Architecture.Utils;

namespace TheGrowth.Architecture.IOC
{
    /// <summary>
    /// Контейнер зависимостей: объект регистрируется под своим типом и под интерфейсами-наследниками
    /// <see cref="ISharedInterface"/>, поиск идёт по цепочке родителей. Инжект, <see cref="IShared.Init"/> и
    /// <see cref="IShared.Dispose"/> выполняются по одному разу на экземпляр, сколько бы ключей у экземпляра ни было:
    /// инжект и <c>Init</c> — в порядке регистрации, <c>Dispose</c> — в обратном (поздние системы обычно зависят от ранних).
    /// </summary>
    public class Container
    {
        private static int m_Counter = 0;
        private readonly Dictionary<Type, IShared> m_Shareds;
        private readonly List<IShared> m_Instances;

        public string Name { get; private set; }
        public bool Active { get; private set; }
        public Container Parent { get; private set; }
        public List<Container> Childs { get; private set; }

        /// <summary>Зарегистрированные экземпляры, каждый один раз, в порядке регистрации.</summary>
        public List<IShared> SharedObjects => new List<IShared>(m_Instances);

        public Container(Container parent = null, string name = null)
        {
            Name = name ?? $"Container#{m_Counter:0000}";
            m_Counter++;

            Active = false;
            Parent = parent;
            Childs = new List<Container>();
            m_Shareds = new Dictionary<Type, IShared>();
            m_Instances = new List<IShared>();

            if (Parent != null)
                Parent.Childs.Add(this);
        }

        public T Add<T>() where T : IShared, new()
        {
            return Add(new T());
        }

        /// <summary>
        /// Регистрация под типом <typeparamref name="T"/> и (при <paramref name="addSubtypes"/>) под его
        /// интерфейсами-наследниками <see cref="ISharedInterface"/>. Занятый ключ — исключение, ничего не регистрируется.
        /// Повторная регистрация того же экземпляра под другими ключами не создаёт второго экземпляра.
        /// </summary>
        public T Add<T>(T shared, bool addSubtypes = true) where T : IShared
        {
            if (shared == null)
                throw new ArgumentNullException(nameof(shared));

            var keys = GetKeys(typeof(T), addSubtypes);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!m_Shareds.ContainsKey(key))
                    continue;

                throw new ContainerTypeException(i == 0
                    ? $"Container already contains instance of Type '{key.Name}'"
                    : $"Container already contains instance of Interface '{key.Name}'");
            }

            foreach (var key in keys)
                m_Shareds.Add(key, shared);

            if (IndexOfInstance(shared) < 0)
                m_Instances.Add(shared);

            return shared;
        }

        public T Get<T>() where T : ISharedInterface
        {
            return (T) Get(typeof(T));
        }

        public object Get(Type type)
        {
            if (m_Shareds.TryGetValue(type, out var shared))
                return shared;

            if (Parent == null)
                throw new ContainerTypeException($"Container doesn't contains element of Type '{type.Name}'");
            return Parent.Get(type);
        }

        /// <summary>Экземпляры типа <typeparamref name="I"/>, каждый один раз; с родителями — родительские первыми.</summary>
        public List<I> GetAll<I>(bool includeParents = false, List<I> objects = null)
        {
            if (objects == null)
                objects = new List<I>();

            if (includeParents && Parent != null)
                Parent.GetAll(true, objects);

            foreach (var shared in m_Instances)
            {
                if (shared is I item)
                    objects.Add(item);
            }

            return objects;
        }

        public void Init(bool initObjects = true)
        {
            if (Parent is { Active: false })
                throw new ContainerInitializationException(
                    $"Can't initialize '{Name}': Parent container '{Parent.Name}' is not active!");

            foreach (var shared in m_Instances)
                InjectAt(shared.GetType(), shared);

            if (initObjects)
            {
                foreach (var shared in m_Instances)
                    shared.Init();
            }

            Active = true;
        }

        public void InjectAt(object target)
        {
            InjectAt(target.GetType(), target);
        }

        public void InjectAt<T>(T target)
        {
            InjectAt(typeof(T), target);
        }

        public void InjectAt(Type type, object target)
        {
            var containerType = typeof(Container);

            var fields = type.GetAllFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Attribute attr = Attribute.GetCustomAttribute(field, typeof(Inject));
                if (attr == null)
                    continue;

                object other = (field.FieldType == containerType) ? this : Get(field.FieldType);
                field.SetValue(target, other);
            }
        }

        public void Dispose()
        {
            foreach (var child in Childs)
            {
                if (child.Active)
                    throw new ContainerInitializationException(
                        $"Can't dispose '{Name}': Child container '{child.Name}' is active!");
            }

            Active = false;

            for (var i = m_Instances.Count - 1; i >= 0; i--)
                m_Instances[i].Dispose();

            if (Parent != null)
                Parent.Childs.Remove(this);
        }

        private static List<Type> GetKeys(Type type, bool addSubtypes)
        {
            var keys = new List<Type> { type };
            if (!addSubtypes)
                return keys;

            Type inter = typeof(ISharedInterface);
            foreach (var sub in type.GetInterfaces())
            {
                if (sub == null || sub == inter || sub == typeof(IShared))
                    continue;

                if (inter.IsAssignableFrom(sub))
                    keys.Add(sub);
            }

            return keys;
        }

        private int IndexOfInstance(IShared shared)
        {
            for (var i = 0; i < m_Instances.Count; i++)
            {
                if (ReferenceEquals(m_Instances[i], shared))
                    return i;
            }

            return -1;
        }
    }
}
