using System;
using System.Collections.Generic;

namespace composite.unity.Core
{
    public abstract class DependencyContainerBase<T> where T : DependencyContainerBase<T>, new()
    {
        private readonly Dictionary<Type, object> _dependencies = new();

        #region INSTANCE
        private static T _instance;
        private static T Instance => _instance ??= new T();
        #endregion

        public static T CreateNewInstance() => _instance = new T();

        public static TDependency Register<TDependency>(TDependency dependency)
        {
            if (Instance._dependencies.TryGetValue(typeof(TDependency), out var service))
                throw new Exception($"Dependency ({service}) already registered.");
            
            Instance._dependencies.Add(typeof(TDependency), dependency);
            return dependency;
        }

        public static bool Contains(Type type) => Instance._dependencies.ContainsKey(type);

        public static bool Contains<TDependency>() => Instance._dependencies.ContainsKey(typeof(TDependency));

        public static object Get(Type type)
        {
            if (!Instance._dependencies.TryGetValue(type, out var service))
                throw new Exception($"Dependency ({service}) is not registered.");

            return service;
        }

        public static TDependency Get<TDependency>()
        {
            if (!Instance._dependencies.TryGetValue(typeof(TDependency), out var service))
                throw new Exception($"Dependency ({service}) is not registered.");

            return (TDependency)service;
        }

        public void Clear() => _dependencies.Clear();
    }
}