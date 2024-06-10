﻿namespace composite.unity.Core
{
    public interface ICommandListener<TCommand> where TCommand : ICommand
    {
        void ReactCommand(TCommand command);
    }
}
