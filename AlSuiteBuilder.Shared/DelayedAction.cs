using System;

namespace AlSuitBuilder.Shared
{
    public class DelayedAction : QueuedAction
    {
        private DateTime _executeTime;
        public DelayedAction(int delay, Action action) : base(action)
        {
            _executeTime = DateTime.Now.AddMilliseconds(delay);
        }

        public override bool CanExecute()
        {
            return _executeTime <= DateTime.Now;
        }
    }
}
