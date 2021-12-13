using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Shared.Messages.Client
{
    [Serializable]
    public class WorkResultMessage : INetworkMessage
    {
        public bool Success;
        public int WorkId;
    }
}
