using System;
using System.Collections.Generic;

namespace composite.unity.Core
{
    public class LocalCommandHandler : ICommandHandler
    {
        private readonly Dictionary<Type, List<object>> _listenersMap = new(256);

        #region INSTANCE
        private static LocalCommandHandler _instance;
        public static LocalCommandHandler Instance => _instance ??= new();
        #endregion

        public void AddListener(object commandsListener)
        {
            var type = commandsListener.GetType();

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType
                    && implementedInterface.GetGenericTypeDefinition() == typeof(ICommandListener<>))
                {
                    var commandType = implementedInterface.GetGenericArguments()[0];

                    if (!_listenersMap.TryGetValue(commandType, out var listeners))
                    {
                        listeners = new List<object>();
                        _listenersMap[commandType] = listeners;
                    }

                    listeners.Add(commandsListener);
                }
            }
        }

        public void RemoveListener(object commandsListener)
        {
            var type = commandsListener.GetType();

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType
                    && implementedInterface.GetGenericTypeDefinition() == typeof(ICommandListener<>))
                {
                    var commandType = implementedInterface.GetGenericArguments()[0];

                    if (_listenersMap.TryGetValue(commandType, out var listeners))
                        listeners.Remove(commandsListener);
                }
            }
        }

        public void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, ICommand
        {
            var commandType = typeof(TCommand);

            if (_listenersMap.TryGetValue(commandType, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    if (listener is ICommandListener<TCommand> commandListener)
                        commandListener.ReactCommand(command);
                }
            }
        }

        public void CleanUp() => _listenersMap.Clear();

        public LocalCommandHandler NewInstance() => _instance = new();
    }
}
