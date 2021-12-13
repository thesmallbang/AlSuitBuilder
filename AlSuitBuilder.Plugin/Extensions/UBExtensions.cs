using AlSuitBuilder.Shared;
using AlSuitBuilder.Shared.Messages;
using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UBNetworking;

namespace AlSuitBuilder.Plugin.Extensions
{
    internal static class UBExtensions
    {

        internal static void SendMessage(this UBClient client, INetworkMessage message)
        {
            Utils.WriteToChat("Sending a message of " + message.GetType().Name);
            client.SendObject(new UBNetworking.Lib.MessageHeader() {
            Type = UBNetworking.Lib.MessageHeaderType.Serialized,
            SendingClientId = client.ClientId,
            TargetClientId = 0,
            Flags = UBNetworking.Lib.MessageHeaderFlags.None
            }, message);
        }
    }
}
