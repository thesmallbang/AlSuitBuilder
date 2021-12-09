using AlSuitBuilder.Plugin.Integrations;
using AlSuitBuilder.Shared;
using AlSuiteBuilder.Shared;
using AlSuiteBuilder.Shared.Messages;
using AlSuiteBuilder.Shared.Messages.Client;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;

namespace AlSuitBuilder.Plugin
{
    public class SuitBuilderPlugin
    {


        public static SuitBuilderPlugin Current { get { return _instance; } }

        internal SuitBuilderType PluginType { get; private set; } = SuitBuilderType.Unknown;
        internal SuitBuilderState PluginState { get; private set; } = SuitBuilderState.Unknown;
        internal NetServiceHost PluginHost { get; private set; } = null;

        private static SuitBuilderPlugin _instance;
        private CoreManager _core = null;

        private Queue<Action> _actionQueue = new Queue<Action>();

        private NetworkProxy _net = new NetworkProxy();

        public void Startup(NetServiceHost host)
        {
            if (_instance != null)

            {
                return;
            }
            _instance = this;

            PluginHost = host;
            PluginHost.Actions.AddChatText("Builders Startup", 1);
            _actionQueue.Clear(); 

            _core = CoreManager.Current;
            _core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff += CharacterFilter_Logoff;
            _net.OnWelcomeMessage += Net_OnWelcomeMessage;

            _net.Startup();


        }


        /// <summary>
        /// The message message should be sent from the server upon a successful connection.
        /// </summary>
        /// <param name="welcomeMessage"></param>
        private void Net_OnWelcomeMessage(WelcomeMessage welcomeMessage)
        {
            Utils.WriteToChat("Welcome received.");

            var state = SuitBuilderState.Idle;

            if (welcomeMessage.ServerState == SuitBuilderState.Building)
            {
                // send a request for action
                state = SuitBuilderState.Waiting;
               AddAction(()=> SendReadyForWork());
            }

            SetState(state);                    
        }

        private void AddAction(Action action)
        {
            Utils.WriteToChat("Action added");
            _actionQueue.Enqueue(action);
        }

        public void Tick()
        {
            _net.Tick();

            try
            {
                if (_actionQueue.Any())
                {
                    var action = _actionQueue.Dequeue();
                    action.Invoke();
                }
            }
            catch (Exception ex)
            {
                Utils.LogException(ex);
            }

        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            PluginHost.Actions.AddChatText("[AlSuitBuilder] Initializing", 1);
            _net.Startup();

        }

        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            _net?.Shutdown();
        }

       public void Shutdown()
        {

            _net?.Shutdown();

            _core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff -= CharacterFilter_Logoff;

            _net.OnWelcomeMessage -= Net_OnWelcomeMessage;
            PluginHost?.Actions.AddChatText("Builders Shutdown", 1);

        }


        private void SetState(SuitBuilderState state)
        {
      
            switch (state)
            {
                case SuitBuilderState.Unknown:
                case SuitBuilderState.Idle:
                    break;
                case SuitBuilderState.Building:
                    break;
                default:
                    break;
            }

            PluginState = state;
        }

        private void SendReadyForWork()
        {
            Utils.WriteToChat("Sending ready for work");
            _net.Send(new ReadyForWorkMessage() {Account = _core.CharacterFilter.AccountName, Server = _core.CharacterFilter.Server, Character = _core.CharacterFilter.Name });
        }

    }
}
