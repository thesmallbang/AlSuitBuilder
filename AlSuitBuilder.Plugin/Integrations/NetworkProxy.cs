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



        public void Startup()
        {


            if (_started)
                return;

            Action<Action> runOnMainThread = (a) => { GameThreadActionQueue.Enqueue(a); };

            try
            {
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

        private void WelcomeMessageHandler(MessageHeader arg1, WelcomeMessage arg2)
        {
            

        }

        internal void Shutdown()
        {
            GameThreadActionQueue.Enqueue(() =>  IntegratedClient?.Dispose());
        }

    }
}
