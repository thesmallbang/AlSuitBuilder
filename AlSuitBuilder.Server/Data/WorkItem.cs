using System;

namespace AlSuitBuilder.Server.Data
{
    internal class WorkItem
    {
        internal int Id;
        internal string Character;
        internal string ItemName;
        internal int[] Requirements;
        internal int MaterialId;
        internal int SetId;
        internal DateTime LastAttempt = DateTime.MinValue;
    }

   
}