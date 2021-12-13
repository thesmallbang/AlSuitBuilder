using System;

namespace AlSuitBuilder.Server.Actions
{
    public abstract class UnclearableAction : IServerAction
    {
        public abstract void Execute();
    }

}