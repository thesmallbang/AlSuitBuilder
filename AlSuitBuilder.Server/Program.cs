using AlSuiteBuilder.Shared.Messages;
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
                    //nc.RemoveMessageHandler<WelcomeMessage>(WelcomeMessageHandler);
                    _clientSubs.Remove(c);
                }
            });
        }

        private static void IntegratedServer_OnClientConnected(object sender, EventArgs e)
        {

            var newClients = IntegratedServer.Clients.Select(o => o.Value.ClientId).Except(_clientSubs).ToList();

            newClients.ForEach(c =>
            {
                var nc = IntegratedServer.Clients[c];
               // nc.AddMessageHandler<WelcomeMessage>(WelcomeMessageHandler);
                nc.SendObject(new UBNetworking.Lib.MessageHeader() { TargetClientId = 0, Type = UBNetworking.Lib.MessageHeaderType.Serialized, SendingClientId = c }, new WelcomeMessage() { Message = "Servers Rock." });
                _clientSubs.Add(c);
            });


        }

    }
}
