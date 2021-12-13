using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Shared.Messages.Client
{
    [Serializable]
    public class InitiateBuildMessage : INetworkMessage
    {
        public string SuitName;

    }
}
