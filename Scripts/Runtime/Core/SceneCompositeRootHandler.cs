using composite.unity.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace composite.unity.Core
{
    [DefaultExecutionOrder(-10000)]
    public class SceneCompositeRootHandler : MonoBehaviour
    {
        private readonly List<ITickable> _tickables = new(256);
        private readonly List<IFixedTickable> _fixedTickables = new(256);
        private readonly List<ILateTickable> _lateTickables = new(256);

        [SerializeField] private List<CompositeRootBase> _order;
        [SerializeField] private List<MonoBehaviour> _cachedInjectables;
        [SerializeField] private List<MonoBehaviour> _cachedCommandListeners;

        private LocalDependencyContainer _localDependencyContainer;
        private LocalCommandHandler _localCommandHandler;

        private void OnValidate()
        {
            _order.RemoveAll(item => item == null);
        }

        private async void Awake()
        {
            _localDependencyContainer = LocalDependencyContainer.CreateNewInstance();
            _localCommandHandler = LocalCommandHandler.Instance.NewInstance();

            foreach (var compositeRoot in _order)
                compositeRoot.InstallBindings();

            InjectDependeciesToGameObjects();
            AddCachedCommandListeners();

            foreach (var compositeRoot in _order)
                await compositeRoot.PreInitialize();

            foreach (var installer in _order)
                await installer.Initialize();

            foreach (var installer in _order)
                await installer.Run();

            foreach (var compositeRoot in _order)
            {
                if (compositeRoot is ITickable tickable)
                    _tickables.Add(tickable);
            }

            foreach (var compositeRoot in _order)
            {
                if (compositeRoot is IFixedTickable fixedTickable)
                    _fixedTickables.Add(fixedTickable);
            }

            foreach (var compositeRoot in _order)
            {
                if (compositeRoot is ILateTickable lateTickable)
                    _lateTickables.Add(lateTickable);
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;

            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick(deltaTime);
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;

            for (int i = 0; i < _lateTickables.Count; i++)
                _lateTickables[i].LateTick(deltaTime);
        }

        private void OnDestroy()
        {
            foreach (var compositeRoot in _order)
                compositeRoot.OnBeforeDestroyed();

            _localCommandHandler.CleanUp();
            _localDependencyContainer.Clear();
        }

        private void InjectDependeciesToGameObjects()
        {
            foreach (var cachedGameObject in _cachedInjectables)
                DependencyInjector.Inject(cachedGameObject.gameObject);
        }

        private void AddCachedCommandListeners()
        {
            foreach (var cachedGameObject in _cachedCommandListeners)
                _localCommandHandler.AddListener(cachedGameObject);
        }

        private void FetchComponents()
        {
            FetchCachedObjects();
            FetchCompositeRoots();

            _order.RemoveAll(item => item == null);
        }

        private void FetchCompositeRoots()
        {
            var fetched = GetComponentsInChildren<CompositeRootBase>().ToList();

            foreach (var child in fetched)
            {
               if (!_order.Contains(child))
                   _order.Add(child);
            }
        }

        private void FetchCachedObjects()
        {
            _cachedInjectables.Clear();
            _cachedCommandListeners.Clear();

            var monoBehaviours = FindObjectsOfType<MonoBehaviour>(this);

            foreach (var monoBehaviour in monoBehaviours)
            {
                var type = monoBehaviour.GetType();
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(InjectAttribute), inherit: true);

                    if (attributes.Length > 0)
                    {
                        _cachedInjectables.Add(monoBehaviour);
                        break;
                    }
                }

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (implementedInterface.IsGenericType
                        && implementedInterface.GetGenericTypeDefinition() == typeof(ICommandListener<>))
                    {
                        _cachedCommandListeners.Add(monoBehaviour);
                        break;
                    }
                }
            }
        }
    }
}