using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Shared.Messages.Client
{
    [Serializable]
    public class WorkResultMessage : INetworkMessage
    {
        public bool Success;
        public int WorkId;
    }
}
