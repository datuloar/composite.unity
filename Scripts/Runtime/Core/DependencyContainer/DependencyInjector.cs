using System;
using System.Reflection;
using UnityEngine;

namespace composite.unity.Core
{
    public class DependencyInjector
    {
        private static readonly BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        public static T CreateInstanceWithInject<T>() where T : class
        {
            var instanceType = typeof(T);
            var constructor = GetSingleConstructor(instanceType);
            var args = GetArgs(constructor.GetParameters());

            return (T)constructor.Invoke(args);
        }

        private static ConstructorInfo GetSingleConstructor(Type instanceType)
        {
            var constructors = instanceType.GetConstructors(_bindingFlags);

            if (constructors.Length != 1)
                throw new InvalidOperationException($"Type {instanceType.Name} must have exactly one constructor.");

            return constructors[0];
        }

        public static void Inject(object instance)
        {
            var type = instance.GetType();
            var methods = type.GetMethods(_bindingFlags);

            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(InjectAttribute)))
                    InvokeMethod(method, instance);
            }
        }

        public static void Inject(GameObject gameObject, bool includeChilds = false)
        {
            var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();

            foreach (var monoBehaviour in monoBehaviours)
                Inject(monoBehaviour);

            if (includeChilds)
            {
                foreach (Transform item in gameObject.transform)
                    Inject(item.gameObject, true);
            }
        }

        private static void InvokeMethod(MethodInfo method, object target) =>
            method.Invoke(target, GetArgs(method.GetParameters()));

        private static object[] GetArgs(ParameterInfo[] parameters)
        {
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                object dependency = GetDependency(type);

                if (dependency != null)
                    args[i] = dependency;
                else
                    throw new Exception($"Missing dependency {type.Name} for injection!");
            }

            return args;
        }

        private static object GetDependency(Type type)
        {
            if (GlobalDependencyContainer.Contains(type))
                return GlobalDependencyContainer.Get(type);

            if (LocalDependencyContainer.Contains(type))
                return LocalDependencyContainer.Get(type);

            return null;
        }
    }
}
