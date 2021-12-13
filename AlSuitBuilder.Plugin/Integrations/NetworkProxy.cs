using AlSuitBuilder.Plugin.Extensions;
using AlSuitBuilder.Shared;
using AlSuitBuilder.Shared.Messages;
using AlSuitBuilder.Shared.Messages.Server;
using Decal.Adapter;
using System;

using UBNetworking.Lib;

namespace AlSuitBuilder.Plugin.Integrations
{
    internal class NetworkProxy
    {

        private UBNetworking.UBClient IntegratedClient;
        private bool _started;

        public delegate void DOnWelcomeMessage(WelcomeMessage welcomeMessage);
        public event DOnWelcomeMessage OnWelcomeMessage;

        public delegate void DOnGiveItemMessage(GiveItemMessage giveItemMessage);
        public event DOnGiveItemMessage OnGiveItemMessage;


        public void Startup()
        {


            if (_started)
                return;

            try
            {
                Action<Action> runOnMainThread = (a) => { a.Invoke(); };
                Action<string> log = (s) => { runOnMainThread.Invoke(() => Utils.WriteLog("Network:" + s)); };
                IntegratedClient = new UBNetworking.UBClient("127.0.0.1", 16753, log, runOnMainThread, new AlSerializationBinder());
                IntegratedClient.AddMessageHandler<WelcomeMessage>(WelcomeMessageHandler);
                IntegratedClient.AddMessageHandler<GiveItemMessage>(GiveItemMessageHandler);
                IntegratedClient.AddMessageHandler<InitiateBuildResponseMessage>(InitiateBuildResponseMessageHandler);
                IntegratedClient.AddMessageHandler<SwitchCharacterMessage>(SwitchCharacterMessageHandler);

                _started = true;
            }
            catch (Exception ex)
            {
                Utils.WriteLog("ClientIssue " + ex.Message);
            }
        }

        private void SwitchCharacterMessageHandler(MessageHeader header, SwitchCharacterMessage message)
        {
            SuitBuilderPlugin.Current.SwapCharacter = message.Character;

            Utils.WriteToChat("Told to switch characters to " + message.Character);
            CoreManager.Current.Actions.Logout();            
        }

        private void InitiateBuildResponseMessageHandler(MessageHeader header, InitiateBuildResponseMessage message)
        {
            Utils.WriteToChat($"Response: Accepted = {message.Accepted}, Message = {message.Message}");
        }

        private void GiveItemMessageHandler(MessageHeader header, GiveItemMessage message)
        {
            if (message == null)
                return;

            OnGiveItemMessage?.Invoke(message);
        }

        internal void Tick()
        {
            //Action nextAction = null;
            //GameThreadActionQueue.TryDequeue(out nextAction);
            //if (nextAction != null)
            //    nextAction.Invoke();
        }

        private void WelcomeMessageHandler(MessageHeader header, WelcomeMessage message)
        {
            SuitBuilderPlugin.Current.SwapCharacter = string.Empty;
            if (message == null)
                return;

            OnWelcomeMessage?.Invoke(message);
        }

        internal void Shutdown()
        {
            IntegratedClient?.RemoveMessageHandler<WelcomeMessage>(WelcomeMessageHandler);
            IntegratedClient?.RemoveMessageHandler<GiveItemMessage>(GiveItemMessageHandler);
            IntegratedClient?.RemoveMessageHandler<InitiateBuildResponseMessage>(InitiateBuildResponseMessageHandler);
            IntegratedClient?.RemoveMessageHandler<SwitchCharacterMessage>(SwitchCharacterMessageHandler);
            IntegratedClient?.Dispose();
        }

        internal void Send(INetworkMessage message)
        {
            IntegratedClient.SendMessage(message);
        }




    }
}
