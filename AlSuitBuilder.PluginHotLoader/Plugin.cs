using AlSuiteBuilder.Shared;
using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;

namespace AlSuitBuilder.PluginHotLoader
{
    [FriendlyName("SuitCollector_HotReload")]
    public class SuitCollectorHotReload : FilterBase
    {
        public string AccountName;
        public string CharacterName;
        public string ServerName;
        public Dictionary<int, string> Characters = new Dictionary<int, string>();

        private object pluginInstance;
        private Assembly pluginAssembly;
        private Type pluginType;
        private FileSystemWatcher pluginWatcher = null;

        private bool pluginsReady = false;
        private MethodInfo tickMethod;
        private bool isLoaded;

        private Timer tickTimer = new Timer(100);
        private Timer assemblyTimer = new Timer(3000);



        /// <summary>
        /// Namespace of the plugin we want to hot reload
        /// </summary>
        public static string PluginAssemblyNamespace { get { return "AlSuitBuilder.Plugin.SuitBuilderPlugin"; } }

        /// <summary>
        /// File name of the plugin we want to hot reload
        /// </summary>
        public static string PluginAssemblyName { get { return "AlSuitBuilder.Plugin.dll"; } }

        /// <summary>
        /// Assembly directory (contains both loader and plugin dlls)
        /// </summary>
        public static string PluginAssemblyDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(SuitCollectorHotReload)).Location);
            }
        }

        /// <summary>
        /// Full path to plugin assembly
        /// </summary>
        public string PluginAssemblyPath
        {
            get
            {
                return System.IO.Path.Combine(PluginAssemblyDirectory, PluginAssemblyName);
            }
        }

        #region FilterBase overrides
        /// <summary>
        /// This is called when the filter is started up.  This happens when ac client is first started
        /// </summary>
        protected override void Startup()
        {
            try
            {
                File.AppendAllText(@"C:\temp\aclog\loggy.log", "PluginLoader Startup" + DateTime.Now.ToString());

                tickTimer.Elapsed += Timer_Elapsed;
                tickTimer.Enabled = true;
                tickTimer.Start();
                assemblyTimer.Elapsed += AssemblyTimer_Elapsed;
                assemblyTimer.Enabled = true;
                assemblyTimer.AutoReset = true;
                assemblyTimer.Start();

                // subscribe to built in decal events
                ServerDispatch += FilterCore_ServerDispatch;
                ClientDispatch += FilterCore_ClientDispatch;
                Core.PluginInitComplete += Core_PluginInitComplete;
                Core.PluginTermComplete += Core_PluginTermComplete;

                // watch the PluginAssemblyName for file changes
                pluginWatcher = new FileSystemWatcher();
                pluginWatcher.Path = PluginAssemblyDirectory;
                pluginWatcher.NotifyFilter = NotifyFilters.LastWrite;
                pluginWatcher.Filter = PluginAssemblyName;
                pluginWatcher.Changed += PluginWatcher_Changed; ;
                pluginWatcher.EnableRaisingEvents = true;

            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        private void AssemblyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isLoaded && pluginsReady)
                LoadPluginAssembly();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            tickMethod?.Invoke(pluginInstance, null);
        }

        /// <summary>
        /// This is called when the filter is shut down. This happens once when the game is closing.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                Core.PluginInitComplete -= Core_PluginInitComplete;
                Core.PluginTermComplete -= Core_PluginTermComplete;
                UnloadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }
        #endregion

        #region Decal Event Handlers
        /// <summary>
        /// Decal ServerDispatch event handler.  This is called when ac client recieves a network message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterCore_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            try
            {
                // see https://acemulator.github.io/protocol/ for protocol documentation
                switch (e.Message.Type)
                {
                    case 0xF658: // LoginCharacterSet S2C
                        AccountName = e.Message.Value<string>("zonename");
                        int characterCount = e.Message.Value<int>("characterCount");
                        MessageStruct characters = e.Message.Struct("characters");
                        Characters.Clear();

                        for (int i = 0; i < characterCount; i++)
                        {
                            int id = characters.Struct(i).Value<int>("character");
                            string name = characters.Struct(i).Value<string>("name");
                            Characters.Add(id, name);
                        }
                        break;

                    case 0xF7E1: // Login_WorldInfo
                        ServerName = e.Message.Value<string>("server");
                        break;
                }
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        /// <summary>
        /// Handle decal ClientDispatch event.  This is called when ac client emits a network message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterCore_ClientDispatch(object sender, NetworkMessageEventArgs e)
        {
            try
            {
                if (e.Message.Type == 0xF657)
                { // SendEnterWorld C2S
                    int loginId = Convert.ToInt32(e.Message["character"]);

                    if (Characters.ContainsKey(loginId))
                    {
                        CharacterName = Characters[loginId];
                    }
                }
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        private void Core_PluginInitComplete(object sender, EventArgs e)
        {
            try
            {
                pluginsReady = true;
                LoadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            try
            {
                pluginsReady = false;
                UnloadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }
        #endregion

        #region Plugin Loading/Unloading
        internal void LoadPluginAssembly()
        {
            try
            {
                if (!pluginsReady)
                    return;

                if (isLoaded)
                {
                    UnloadPluginAssembly();
                    Utils.WriteToChat($"Reloading {PluginAssemblyName}");
                }

                pluginAssembly = Assembly.Load(File.ReadAllBytes(PluginAssemblyPath));
                pluginType = pluginAssembly.GetType(PluginAssemblyNamespace);
                pluginInstance = Activator.CreateInstance(pluginType);

                var startupMethod = pluginType.GetMethod("Startup");
                startupMethod.Invoke(pluginInstance, new object[] {
                    Host
                });

                tickMethod = pluginType.GetMethod("Tick");
                

                isLoaded = true;
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        private void UnloadPluginAssembly()
        {
            try
            {
                if (pluginInstance != null && pluginType != null)
                {
                    isLoaded = false;
                    MethodInfo shutdownMethod = pluginType.GetMethod("Shutdown");
                    shutdownMethod.Invoke(pluginInstance, null);
                    pluginInstance = null;
                    pluginType = null;
                }
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        private void PluginWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                assemblyTimer.Stop();
                UnloadPluginAssembly();
                assemblyTimer.Start();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }
        #endregion
    }
}
