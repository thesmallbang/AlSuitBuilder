using System;

namespace AlSuitBuilder.Server.Data
{
    internal class WorkItem
    {
        internal int Id;
        internal string Character;
        internal string ItemName;
        internal string[] Requirements;
        internal DateTime LastAttempt = DateTime.MinValue;
    }

   
}