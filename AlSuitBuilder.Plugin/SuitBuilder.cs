using AlSuitBuilder.Plugin.Integrations;
using AlSuitBuilder.Shared;
using AlSuiteBuilder.Shared;
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

        internal SuiteBuilderType PluginType { get; private set; } = SuiteBuilderType.Unknown;
        internal SuiteBuilderState PluginState { get; private set; } = SuiteBuilderState.Offline;
        internal NetServiceHost PluginHost { get; private set; } = null;

        private static SuitBuilderPlugin _instance;
        private CoreManager _core = null;

        private NetworkProxy net = new NetworkProxy();

        public void Startup(NetServiceHost host)
        {
            if (_instance != null)

            {
                return;
            }
            _instance = this;

            PluginHost = host;
            PluginHost.Actions.AddChatText("Builders Startup", 1);
            net.Startup();

            _core = CoreManager.Current;
            _core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff += CharacterFilter_Logoff;
        }

        public void Tick()
        {
            net.Tick();
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            PluginHost.Actions.AddChatText("[AlSuitBuilder] Initializing", 1);
            net.Startup();

        }

        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            net?.Shutdown();
        }

       

        public void Shutdown()
        {

            net?.Shutdown();

            _core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff -= CharacterFilter_Logoff;
            PluginHost?.Actions.AddChatText("Builders Shutdown", 1);

        }


    }
}
