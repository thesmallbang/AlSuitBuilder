using AlSuitBuilder.Plugin.Extensions;
using AlSuiteBuilder.Shared;
using AlSuiteBuilder.Shared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBNetworking.Lib;

namespace AlSuitBuilder.Plugin.Integrations
{
    internal class NetworkProxy
    {
        private ConcurrentQueue<Action> GameThreadActionQueue = new ConcurrentQueue<Action>();

        private UBNetworking.UBClient IntegratedClient;
        private bool _started;

        public delegate void DOnWelcomeMessage(WelcomeMessage welcomeMessage);
        public event DOnWelcomeMessage OnWelcomeMessage;

        public void Startup()
        {


            if (_started)
                return;

            try
            {
                Action<Action> runOnMainThread = (a) => { GameThreadActionQueue.Enqueue(a); };
                Action<string> log = (s) => { runOnMainThread.Invoke(() => Utils.WriteLog("Network:" + s)); };

                IntegratedClient = new UBNetworking.UBClient("127.0.0.1", 16753, log, runOnMainThread, new AlSuitSerializationBinder());
                IntegratedClient.AddMessageHandler<WelcomeMessage>(WelcomeMessageHandler);
                _started = true;
            }
            catch (Exception ex)
            {
                Utils.WriteLog("ClientIssue " + ex.Message);
            }
        }

        internal void Tick()
        {
            Action nextAction = null;
            GameThreadActionQueue.TryDequeue(out nextAction);
            if (nextAction != null)
                nextAction.Invoke();
        }

        private void WelcomeMessageHandler(MessageHeader header, WelcomeMessage message)
        {
            if (message == null)
                return;

            OnWelcomeMessage?.Invoke(message);
        }

        internal void Shutdown()
        {
            GameThreadActionQueue.Enqueue(() =>  IntegratedClient?.Dispose());
        }

        internal void Send(INetworkMessage message)
        {
            IntegratedClient.SendMessage(message);
        }

    }
}
