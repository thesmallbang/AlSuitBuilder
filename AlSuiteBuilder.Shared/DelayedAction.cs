using System;

namespace AlSuitBuilder.Shared
{
    public class DelayedAction : QueuedAction
    {
        private int _delay;
        private DateTime _executeTime;
        public DelayedAction(int delay, Action action) : base(action)
        {
            _delay = delay;
            _executeTime = DateTime.Now.AddMilliseconds(delay);
        }

        public override bool CanExecute()
        {
            
            var canEx = _executeTime <= DateTime.Now;
            //Utils.WriteLog("Checking if delayed action is usable: " + _delay + " - " + canEx);
            return canEx;
        }
    }
}
