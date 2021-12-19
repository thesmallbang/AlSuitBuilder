using System;

namespace AlSuitBuilder.Shared
{
    public class WorkItem
    {
        public int Id;
        public string Character;
        public string ItemName;
        public int[] Requirements;
        public int MaterialId;
        public int SetId;
        public DateTime LastAttempt = DateTime.MinValue;
    }

   
}