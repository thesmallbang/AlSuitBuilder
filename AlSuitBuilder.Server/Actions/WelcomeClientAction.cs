using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Actions
{
    internal class WelcomeClientAction : UnclearableAction
    {
        private int _clientId;

        public WelcomeClientAction(int clientId)
        {
            _clientId = clientId;
        }

        public override void Execute()
        {

            var clientInfo = Program.GetClientInfo(_clientId);

            clientInfo.ServerClient.SendObject(new UBNetworking.Lib.MessageHeader() { TargetClientId = 0, Type = UBNetworking.Lib.MessageHeaderType.Serialized, SendingClientId = _clientId },
                   new WelcomeMessage() { ServerState = Program.BuildInfo != null ? Shared.SuitBuilderState.Building : Shared.SuitBuilderState.Idle });
        }

    }
}
