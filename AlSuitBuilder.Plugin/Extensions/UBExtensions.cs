using AlSuiteBuilder.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBNetworking;

namespace AlSuitBuilder.Plugin.Extensions
{
    internal static class UBExtensions
    {

        internal static void SendMessage(this UBClient client, INetworkMessage message)
        {
            client.SendObject(new UBNetworking.Lib.MessageHeader() {
            Type = UBNetworking.Lib.MessageHeaderType.Serialized,
            SendingClientId = client.ClientId,
            TargetClientId = 0,
            Flags = UBNetworking.Lib.MessageHeaderFlags.None
            }, message);
        }
    }
}
