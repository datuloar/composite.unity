using composite.unity.Common;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace composite.unity.Core
{
    public abstract class CompositeRootBase : MonoBehaviour, ICoroutineRunner
    {
        protected ICommandHandler LocalCommander => LocalCommandHandler.Instance;

        public virtual void InstallBindings() { }

        public async virtual Task PreInitialize() { }

        public async virtual Task Initialize() { }

        public async virtual Task Run() { }

        public virtual void OnBeforeDestroyed() { }

        protected TDependency Get<TDependency>()
        {
            if (GlobalDependencyContainer.Contains<TDependency>())
                return GlobalDependencyContainer.Get<TDependency>();

            if (LocalDependencyContainer.Contains<TDependency>())
                return LocalDependencyContainer.Get<TDependency>();

            throw new InvalidOperationException($"Dependency of type {typeof(TDependency)} is not registered.");
        }

        protected TDependency GetGlobal<TDependency>() =>
            GlobalDependencyContainer.Get<TDependency>();
        protected TDependency GetLocal<TDependency>() =>
            LocalDependencyContainer.Get<TDependency>();

        #region GLOBAL BIND

        protected TDependency CreateAndBindAsGlobal<TDependency>() where TDependency : class
        {
            var instance = DependencyInjector.CreateInstanceWithInject<TDependency>()
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TDependency)}");

            return BindAsGlobal<TDependency>(instance);
        }

        protected TDependencyInterface CreateAndBindAsGlobal<TDependency, TDependencyInterface>() where TDependency : class
        {
            var instance = DependencyInjector.CreateInstanceWithInject<TDependency>()
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TDependency)}");

            if (instance is not TDependencyInterface dependency)
                throw new InvalidOperationException($"Dependency {typeof(TDependency)} missing interface {typeof(TDependencyInterface)}");

            return BindAsGlobal<TDependencyInterface>(dependency);
        }

        protected TDependency BindAsGlobal<TDependency>(TDependency dependency)
        {
            if (GlobalDependencyContainer.Contains<TDependency>())
                return GlobalDependencyContainer.Get<TDependency>();

            return GlobalDependencyContainer.Register(dependency);
        }
        #endregion

        #region BIND LOCAL

        protected TDependencyInterface CreateAndBindAsLocal<TDependency, TDependencyInterface>(bool addToSceneCommander = false) where TDependency : class
        {
            var instance = DependencyInjector.CreateInstanceWithInject<TDependency>()
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TDependency)}");

            if (instance is not TDependencyInterface dependency)
                throw new InvalidOperationException($"Dependency {typeof(TDependency)} missing interface {typeof(TDependencyInterface)}");

            return BindAsLocal<TDependencyInterface>(dependency, addToSceneCommander);
        }

        protected TDependency CreateAndBindAsLocal<TDependency>(bool addToSceneCommander = false) where TDependency : class
        {
            var instance = DependencyInjector.CreateInstanceWithInject<TDependency>()
                ?? throw new InvalidOperationException($"Failed to create instance of {typeof(TDependency)}");

            return BindAsLocal<TDependency>(instance, addToSceneCommander);
        }

        protected TDependency BindAsLocal<TDependency>(TDependency dependency, bool addToSceneCommander = false)
        {
            if (GlobalDependencyContainer.Contains<TDependency>())
                return GlobalDependencyContainer.Get<TDependency>();

            if (LocalDependencyContainer.Contains<TDependency>())
                return LocalDependencyContainer.Get<TDependency>();

            var registeredDependency = LocalDependencyContainer.Register(dependency);

            if (addToSceneCommander)
                LocalCommander.AddListener(registeredDependency);

            return registeredDependency;
        }
        #endregion
    }
}