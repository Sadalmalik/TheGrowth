using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheGrowth.Architecture.Utils;

namespace TheGrowth.Architecture.IOC
{
    public class Container
    {
        private static int m_Counter = 0;
        private Dictionary<Type, IShared> m_Shareds;

        public string Name { get; private set; }
        public bool Active { get; private set; }
        public Container Parent { get; private set; }
        public List<Container> Childs { get; private set; }
        public List<IShared> SharedObjects => m_Shareds.Values.ToList();

        public Container(Container parent = null, string name = null)
        {
            Name = name ?? $"Container#{m_Counter:0000}";
            m_Counter++;

            Active = false;
            Parent = parent;
            Childs = new List<Container>();
            m_Shareds = new Dictionary<Type, IShared>();

            if (Parent != null)
                Parent.Childs.Add(this);
        }

        public T Add<T>() where T : IShared, new()
        {
            return Add(new T());
        }

        public T Add<T>(T shared, bool addSubtypes = true) where T : IShared
        {
            Type type = typeof(T);
            if (m_Shareds.ContainsKey(type))
                throw new ContainerTypeException($"Container already contains instance of Type '{type.Name}'");
            m_Shareds.Add(type, shared);

            if (!addSubtypes)
                return shared;

            Type inter = typeof(ISharedInterface);
            var subtypes = type.GetInterfaces();
            foreach (var sub in subtypes)
            {
                if (sub == null || sub == inter || sub == typeof(IShared))
                    continue;

                if (!inter.IsAssignableFrom(sub))
                    continue;

                if (m_Shareds.ContainsKey(sub))
                    throw new ContainerTypeException($"Container already contains instance of Interface '{sub.Name}'");
                m_Shareds.Add(sub, shared);
            }

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

        public List<I> GetAll<I>(bool includeParents = false, List<I> objects = null)
        {
            Type target = typeof(I);

            if (objects == null)
                objects = new List<I>();

            if (includeParents)
                Parent.GetAll<I>(true, objects);

            foreach (var shared in m_Shareds.Values)
            {
                if (shared.GetType().GetInterfaces().Contains(target))
                    objects.Add((I) shared);
            }

            return objects;
        }

        public void Init(bool initObjects = true)
        {
            if (Parent is { Active: false })
                throw new ContainerInitializationException(
                    $"Can't initialize '{Name}': Parent container '{Parent.Name}' is not active!");

            foreach (var pair in m_Shareds)
                InjectAt(pair.Key, pair.Value);

            if (initObjects)
            {
                foreach (var pair in m_Shareds)
                    pair.Value.Init();
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

            foreach (var pair in m_Shareds)
            {
                var shared = pair.Value;
                shared.Dispose();
            }

            if (Parent != null)
                Parent.Childs.Remove(this);
        }
    }
}
