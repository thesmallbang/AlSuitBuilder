using AlSuitBuilder.Shared;
using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Plugin.Actions.Queue
{
    internal class WaitForIDAction : DelayedAction
    {
        public WaitForIDAction(Action action) : base(2000,action)
        {

        }

        public override bool CanExecute()
        {
            return
                base.CanExecute()
                && CoreManager.Current.IDQueue.ActionCount == 0;
        }


    }



}
