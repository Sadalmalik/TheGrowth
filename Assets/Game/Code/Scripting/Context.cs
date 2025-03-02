using System;
using System.Collections.Generic;

namespace Sadalmalik.TheGrowth
{
    public interface IContextData
    {
    }

    public class Context
    {
        public static Context Global;
        
        private Dictionary<Type, IContextData> _data = new Dictionary<Type, IContextData>();

        public Context Parent { get; private set; }

        public Context(params IContextData[] initial)
        {
            Parent = Global;
            foreach (var data in initial)
            {
                _data[data.GetType()] = data;
            }
        }
        public Context(Context parent = null, params IContextData[] initial)
        {
            Parent = parent ?? Global;
            foreach (var data in initial)
            {
                _data[data.GetType()] = data;
            }
        }

        public void Add<TData>(TData data) where TData : IContextData
        {
            if (_data.ContainsKey(typeof(TData)))
                throw new Exception($"Context already contains data: {typeof(TData).Name}");

            _data[typeof(TData)] = data;
        }

        public TData GetOptional<TData>() where TData : class, IContextData
        {
            if (_data.TryGetValue(typeof(TData), out var data))
                return (TData)data;

            return Parent?.GetOptional<TData>();
        }

        public TData GetRequired<TData>() where TData : class, IContextData
        {
            var data = GetOptional<TData>();
            if (data == null)
                throw new Exception($"Context does not contains data: {typeof(TData).Name}");

            return data;
        }
    }
}