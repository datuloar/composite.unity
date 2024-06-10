namespace composite.unity.Core
{
    public interface ICommandHandler
    {
        void AddListener(object commandsListener);
        void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, ICommand;
        void CleanUp();
    }
}
