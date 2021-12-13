using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlSuitBuilder.Shared.Messages.Client
{
    [Serializable]
    public class ReadyForWorkMessage : INetworkMessage
    {
        public string Account { get; set; }
        public string Character { get; set; }
        public string Server { get; set; }

        public string[] AllCharacters { get; set; }
    }
}
