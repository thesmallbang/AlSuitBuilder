using AlSuitBuilder.Shared;
using System;

namespace AlSuitBuilder.Shared.Messages.Server
{
    [Serializable]
    public class InitiateBuildResponseMessage : INetworkMessage
    {
        public string Message;
        public bool Accepted;


    }
}
