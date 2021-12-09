using AlSuiteBuilder.Shared.Messages;
using AlSuiteBuilder.Shared.Messages.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBNetworking;

namespace AlSuitBuilder.Server
{
    internal class Program
    {
        static bool Running = true;

        public static UBServer IntegratedServer { get; private set; }
        private static ConcurrentQueue<Action> GameThreadActionQueue = new ConcurrentQueue<Action>();
        private static List<int> _clientSubs = new List<int>();


        private static List<WorkItem> PendingWork = new List<WorkItem>();
        

        static void Main(string[] args)
        {

            try
            {
                Action<string> logs = (s) => { Console.WriteLine(s); };
                IntegratedServer = new UBNetworking.UBServer("127.0.0.1", 16753, logs);
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
                Action nextAction = null;
                GameThreadActionQueue.TryDequeue(out nextAction);
                if (nextAction != null)
                    nextAction.Invoke();
                System.Threading.Thread.Sleep(100);
            }

        }

        private static void IntegratedServer_OnClientDisconnected(object sender, EventArgs e)
        {
            var orphans = _clientSubs.Where(o => !IntegratedServer.Clients.Any(c => c.Value.ClientId == o)).ToList();
            orphans.ForEach(c =>
            {
                if (IntegratedServer.Clients.ContainsKey(c))
                {
                    var nc = IntegratedServer.Clients[c];
                    nc.RemoveMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);
                    _clientSubs.Remove(c);
                }
            });
        }


        /// <summary>
        /// Client is all setup and is requesting work to do. (deliver an item)
        /// </summary>
        private static void ReadyForWorkMessageHandler(UBNetworking.Lib.MessageHeader header, ReadyForWorkMessage message)
        {
            


        }

        private static void IntegratedServer_OnClientConnected(object sender, EventArgs e)
        {

            var newClients = IntegratedServer.Clients.Select(o => o.Value.ClientId).Except(_clientSubs).ToList();

            newClients.ForEach(c =>
            {
                var nc = IntegratedServer.Clients[c];
                nc.AddMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);
                nc.SendObject(new UBNetworking.Lib.MessageHeader() { TargetClientId = 0, Type = UBNetworking.Lib.MessageHeaderType.Serialized, SendingClientId = c }, 
                    new WelcomeMessage() { ServerState = Shared.SuitBuilderState.Idle });
                _clientSubs.Add(c);
            });


        }

    }
}
