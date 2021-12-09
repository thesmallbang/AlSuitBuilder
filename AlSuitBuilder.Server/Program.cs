using AlSuitBuilder.Server.Actions;
using AlSuiteBuilder.Shared;
using AlSuiteBuilder.Shared.Messages;
using AlSuiteBuilder.Shared.Messages.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBNetworking;
using UBNetworking.Lib;

namespace AlSuitBuilder.Server
{
    internal class Program
    {
        static bool Running = true;

        public static UBServer IntegratedServer { get; private set; }
        private static ConcurrentQueue<IServerAction> _actionQueue = new ConcurrentQueue<IServerAction>();
        private static Dictionary<int, ClientInfo> _clientSubs = new Dictionary<int, ClientInfo>();


        private static List<WorkItem> PendingWork = new List<WorkItem>() { new WorkItem() { } };

        internal static ServerClient  GetClient(int clientId)
        {
            var client =  _clientSubs.FirstOrDefault(o=>o.Key == clientId);
            if (client.Key == 0)
                throw new Exception("Attempt to get client for invalid clientid");

            return client.Value.ServerClient;
        }
        internal static ClientInfo GetClientInfo(int clientId)
        {
            var client = _clientSubs.FirstOrDefault(o => o.Key == clientId);
            if (client.Key == 0)
                throw new Exception("Attempt to get client for invalid clientid");

            return client.Value;
        }

        static void Main(string[] args)
        {

            try
            {
                Action<string> logs = (s) => _actionQueue.Enqueue(LogAction.Create(s));
                IntegratedServer = new UBNetworking.UBServer("127.0.0.1", 16753, logs, new AlSerializationBinder());
                IntegratedServer.OnClientConnected += IntegratedServer_OnClientConnected;
                IntegratedServer.OnClientDisconnected += IntegratedServer_OnClientDisconnected;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
                return;
            }


            while (Running)
            {
                IServerAction nextAction = null;
                _actionQueue.TryDequeue(out nextAction);
                if (nextAction != null)
                    nextAction.GetAction().Invoke();
                System.Threading.Thread.Sleep(100);
            }

        }

        private static void IntegratedServer_OnClientDisconnected(object sender, EventArgs e)
        {
            var orphans = _clientSubs.Where(o => !IntegratedServer.Clients.Any(c => c.Key == o.Key)).ToList();
            orphans.ForEach(c =>
            {
                if (IntegratedServer.Clients.ContainsKey(c.Key))
                {
                    var nc = IntegratedServer.Clients[c.Key];
                    nc.RemoveMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);
                    _clientSubs.Remove(c.Key);
                }
            });
        }


        /// <summary>
        /// Client is all setup and is requesting work to do. (deliver an item)
        /// </summary>
        private static void ReadyForWorkMessageHandler(UBNetworking.Lib.MessageHeader header, ReadyForWorkMessage message)
        {

            var match = _clientSubs.FirstOrDefault(c => c.Key == header.SendingClientId);
            if (match.Key == 0)
                return;

            _clientSubs[match.Key] = new ClientInfo() { AccountName = message.Account, CharacterName = message.Character, ServerName = message.Server, ServerClient = 
                IntegratedServer.Clients[match.Key]};
            _actionQueue.Enqueue(GiveClientWorkAction.Create(header.SendingClientId));
        }

        private static void IntegratedServer_OnClientConnected(object sender, EventArgs e)
        {

            var newClients = IntegratedServer.Clients.Select(o => o.Key).Except(_clientSubs.Keys).ToList();

            newClients.ForEach(c =>
            {

                var nc = IntegratedServer.Clients[c];
                nc.AddMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);

                nc.SendObject(new UBNetworking.Lib.MessageHeader() { TargetClientId = 0, Type = UBNetworking.Lib.MessageHeaderType.Serialized, SendingClientId = c },
                    new WelcomeMessage() { ServerState = PendingWork.Any() ? Shared.SuitBuilderState.Building : Shared.SuitBuilderState.Idle });
                _clientSubs.Add(c, null);
            });


        }
    }
}
