using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Shared
{
    public class QueuedAction
    {
        private static int QueueId = 0;

        public DateTime QueuedTime { get; }
        public int Id;

        private Action _action;

        public QueuedAction(Action action)
        {
            QueueId++;
            Id = QueueId;
            QueuedTime = DateTime.Now;
            _action = action;
        }


        public virtual bool CanExecute()
        {
            return true;
        }

        public virtual void Execute()
        {
            if (_action == null)
                return;

            if (!CanExecute())
                return;

            _action.Invoke();
        }
    }
}
