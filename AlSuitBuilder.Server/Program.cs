using AlSuitBuilder.Server.Actions;
using AlSuitBuilder.Server.Data;
using AlSuitBuilder.Shared;
using AlSuitBuilder.Shared.Messages;
using AlSuitBuilder.Shared.Messages.Client;
using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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

        public static BuildInfo BuildInfo = null;

        public static SpellData SpellData;

        public static string BuildDirectory { get; private set; }

        internal static ServerClient GetClient(int clientId)
        {
            var client = _clientSubs.FirstOrDefault(o => o.Key == clientId);
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

        internal static List<int> GetClientIds()
        {
            return _clientSubs.Keys.ToList();
        }
        static void Main(string[] args)
        {

            try
            {

                BuildDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                Console.WriteLine($"Suit Directory: {BuildDirectory}");

                Console.WriteLine("Loading Spell data");
                // load our spell data
                SpellData = new SpellData();

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
                try
                {
                    IServerAction nextAction = null;
                    _actionQueue.TryDequeue(out nextAction);
                    if (nextAction != null)
                        nextAction.Execute();


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    BuildInfo?.Tick();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                System.Threading.Thread.Sleep(100);
            }

        }

        /// <summary>
        /// Client is all setup and is requesting work to do. (deliver an item)
        /// </summary>
        private static void ReadyForWorkMessageHandler(UBNetworking.Lib.MessageHeader header, ReadyForWorkMessage message)
        {


            Console.WriteLine("client is ready for work " + header.SendingClientId);

            var existing = _clientSubs.Where(o => o.Value.AccountName == message.Account).ToList();
            foreach (var cs in existing)
            {
                Console.WriteLine("Disconnecting a stale character");
                _clientSubs.Remove(cs.Key);
                
            }

            _clientSubs[header.SendingClientId] = new ClientInfo()
            {
                AccountName = message.Account,
                CharacterName = message.Character,
                ServerName = message.Server,
                OtherCharacters =
                message.AllCharacters.Except(new List<string>() { message.Character }).ToList(),
                ServerClient = IntegratedServer.Clients[header.SendingClientId]
            };

            if (BuildInfo != null)
            {
                // reset the work items for this character to no longer have a delay
                BuildInfo.WorkItems.Where(o => o.Character == message.Character).ToList().ForEach(o =>
                {
                    o.LastAttempt = DateTime.MinValue;
                });
            }

        }
        public static void AddAction(IServerAction action)
        {
            _actionQueue.Enqueue(action);
        }

        private static void IntegratedServer_OnClientConnected(object sender, EventArgs e)
        {

            var newClients = IntegratedServer.Clients.Select(o => o.Key).Except(_clientSubs.Keys).ToList();

            newClients.ForEach(c =>
            {

                var nc = IntegratedServer.Clients[c];
                nc.AddMessageHandler<ReadyForWorkMessage>(ReadyForWorkMessageHandler);
                nc.AddMessageHandler<WorkResultMessage>(WorkResultMessageHandler);
                nc.AddMessageHandler<InitiateBuildMessage>(InitiateBuildMessageHandler);
                nc.AddMessageHandler<TerminateBuildMessage>(TerminateBuildMessageHandler);
                _clientSubs.Add(c, new ClientInfo() { ServerClient = nc });
                _actionQueue.Enqueue(new WelcomeClientAction(c));
            });


        }

        private static void TerminateBuildMessageHandler(MessageHeader header, TerminateBuildMessage message)
        {
            ClearActionQueue();

            _actionQueue.Enqueue(new TerminateSuitAction());

        }

        private static void ClearActionQueue()
        {
            var initialCount = _actionQueue.Count;

            for (int i = 0; i < initialCount; i++)
            {
                _actionQueue.TryDequeue(out var action);
                if (action.GetType().IsSubclassOf(typeof(UnclearableAction)))
                    _actionQueue.Enqueue(action);

            }

        }

        private static void InitiateBuildMessageHandler(MessageHeader header, InitiateBuildMessage message)
        {
            _actionQueue.Enqueue(new InitiateSuitAction(header.SendingClientId, message.SuitName));
        }

        public static void CancelBuild()
        {
            Console.WriteLine("Cancelling build");
            BuildInfo = null;
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
                    nc.RemoveMessageHandler<WorkResultMessage>(WorkResultMessageHandler);
                    nc.RemoveMessageHandler<InitiateBuildMessage>(InitiateBuildMessageHandler);
                    nc.RemoveMessageHandler<TerminateBuildMessage>(TerminateBuildMessageHandler);
                    _clientSubs.Remove(c.Key);
                }
            });
        }

        private static void WorkResultMessageHandler(MessageHeader header, WorkResultMessage message)
        {

            if (BuildInfo == null)
                return;

            if (message.WorkId <= 0)
                return;

            // if (message.Success)
            
            var work = BuildInfo.WorkItems.FirstOrDefault(o=>o.Id == message.WorkId);
            if (work == null) return;

            Console.WriteLine("Removing " + work.Id);
            BuildInfo.WorkItems.RemoveAll(o => o.Id == work.Id);
            BuildInfo.WorkItems.Where(o => o.Character == work.Character).ToList().ForEach(o => o.LastAttempt = DateTime.MinValue);




        }

        public static void SendMessageToClient(int clientId, INetworkMessage message)
        {
            var client = IntegratedServer.Clients.FirstOrDefault(o => o.Key == clientId);
            if (client.Value == null)
                return;

            client.Value.SendObject(new MessageHeader()
            {
                TargetClientId = clientId,
                Type = MessageHeaderType.Serialized
            }, message);
        }
    }
}
