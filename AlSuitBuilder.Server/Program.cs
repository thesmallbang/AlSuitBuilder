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

namespace AlSuitBuilder.Server
{
    internal class Program
    {
        static bool Running = true;

        public static UBServer IntegratedServer { get; private set; }
        private static ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();
        private static List<int> _clientSubs = new List<int>();


        private static List<WorkItem> PendingWork = new List<WorkItem>() { new WorkItem() { } };
        

        static void Main(string[] args)
        {

            try
            {
                Action<string> logs = (s) => { _actionQueue.Enqueue(() => Console.WriteLine("SVRLOG: " + s));  };
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
                Action nextAction = null;
                _actionQueue.TryDequeue(out nextAction);
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
                    nc.OnMessageReceived -= Nc_OnMessageReceived;
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
            _actionQueue.Enqueue(() =>  Console.WriteLine($"Client ready for work: {message.Account} {message.Server} {message.Character}"));
        }

        private static void IntegratedServer_OnClientConnected(object sender, EventArgs e)
        {

            var newClients = IntegratedServer.Clients.Select(o => o.Value.ClientId).Except(_clientSubs).ToList();

            newClients.ForEach(c =>
            {
                
                var nc = IntegratedServer.Clients[c];
                nc.OnMessageReceived += Nc_OnMessageReceived;
                nc.AddMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);
                
                nc.SendObject(new UBNetworking.Lib.MessageHeader() { TargetClientId = 0, Type = UBNetworking.Lib.MessageHeaderType.Serialized, SendingClientId = c }, 
                    new WelcomeMessage() { ServerState = PendingWork.Any() ? Shared.SuitBuilderState.Building : Shared.SuitBuilderState.Idle });
                _clientSubs.Add(c);
            });


        }

        private static void Nc_OnMessageReceived(object sender, UBNetworking.Lib.OnMessageEventArgs e)
        {
           _actionQueue.Enqueue(() =>  Console.WriteLine("MsgRcv: " + e.Header.Type.ToString()));
            _actionQueue.Enqueue(() => {   });
        }
    }
}
