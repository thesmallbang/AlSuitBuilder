using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Shared.Messages.Server
{

    [Serializable]
    public class GiveItemMessage : INetworkMessage
    {
        public int WorkId;
        public string DeliverTo;
        public string ItemName;
        public int SetId;
        public int MaterialId;
        public int[] RequiredSpells;

    }
}
