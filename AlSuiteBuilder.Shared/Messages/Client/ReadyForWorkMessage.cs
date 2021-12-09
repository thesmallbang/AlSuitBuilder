using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuiteBuilder.Shared.Messages.Client
{
    [Serializable]
    public class ReadyForWorkMessage : INetworkMessage
    {
        public string Account;
        public string Character;
        public string Server;
    }
}
