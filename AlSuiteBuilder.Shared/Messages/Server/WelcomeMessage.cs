using AlSuitBuilder.Shared;
using System;

namespace AlSuitBuilder.Shared.Messages.Server
{
    [Serializable]
    public class WelcomeMessage  : INetworkMessage
    {
        public SuitBuilderState ServerState { get; set; }


    }
}
