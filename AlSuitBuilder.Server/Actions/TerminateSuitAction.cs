using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Server.Actions
{
    internal class TerminateSuitAction : UnclearableAction
    {

        public override void Execute()
        {
           Program.CancelBuild();
        }
    }
}
