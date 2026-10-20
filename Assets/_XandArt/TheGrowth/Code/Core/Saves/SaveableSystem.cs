using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// База системы сессии с секцией-DTO <typeparamref name="TSave"/>. Секции нет — <see cref="ReadDefaults"/>; секция
    /// старой версии проходит миграции из <see cref="RegisterMigrations"/> до <see cref="SaveVersion"/>.
    /// </summary>
    public abstract class SaveableSystem<TSave> : SharedObject, ISaveable where TSave : class, new()
    {
        public abstract string SaveKey { get; }

        public virtual int SaveVersion => 1;

        public virtual int LoadOrder => 0;

        object ISaveable.Write()
        {
            return Write();
        }

        void ISaveable.Read(SaveSection section)
        {
            if (section == null)
            {
                ReadDefaults();
                return;
            }

            var migrations = new SaveMigrations(SaveKey);
            RegisterMigrations(migrations);
            Read(section.Get<TSave>(SaveVersion, migrations));
        }

        protected abstract TSave Write();

        protected abstract void Read(TSave save);

        /// <summary>Новая игра: секции в сейве нет.</summary>
        protected virtual void ReadDefaults()
        {
        }

        /// <summary>Шаги миграции секции <c>vN → vN+1</c> (<see cref="SaveMigrations.Add"/>); при повышении <see cref="SaveVersion"/>.</summary>
        protected virtual void RegisterMigrations(SaveMigrations migrations)
        {
        }
    }
}
