using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Shared
{
    public enum SuitBuilderState
    {
        Unknown = 0,
        // idle but expecting a response to know what to do next
        Waiting = 10,
        // no plugins should be doing anything in this state except waiting for a suit to start being built
        Idle = 100,
        // a suit is being built 
        Building = 1000,
        

    }
}
