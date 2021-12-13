using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Plugin.Actions.Queue
{
    internal class GenericWorkAction : QueuedAction
    {
        public GenericWorkAction(Action action) : base(action)
        {

        }
    }
}
