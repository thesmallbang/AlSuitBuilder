using AlSuitBuilder.Shared;
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

        private object pluginInstance;
        private Assembly pluginAssembly;
        private Type pluginType;
        private FileSystemWatcher pluginWatcher = null;

        private int characterSlots = 0;
        readonly List<Character> characters = new List<Character>();

        private bool pluginsReady = false;
        private MethodInfo tickMethod;
        private bool isLoaded;

        // limit how often hot reload is triggered since we are referencing build output it is firing multiple times too quickly.
        private Timer assemblyTimer = new Timer(3000);
        private DateTime _lastTick;


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




                assemblyTimer.Elapsed += AssemblyTimer_Elapsed;
                assemblyTimer.Enabled = true;
                assemblyTimer.AutoReset = true;
                assemblyTimer.Start();

                // subscribe to built in decal events
                Core.PluginInitComplete += Core_PluginInitComplete;
                Core.PluginTermComplete += Core_PluginTermComplete;
                ServerDispatch += SuitCollectorHotReload_ServerDispatch;
                Core.RenderFrame += Core_RenderFrame;

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

        private void Core_RenderFrame(object sender, EventArgs e)
        {
            if (_lastTick < DateTime.Now.AddMilliseconds(-100))
            {
                 _lastTick = DateTime.Now;
                tickMethod?.Invoke(pluginInstance, null);
            }
        }

        private void AssemblyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isLoaded && pluginsReady)
            {
                assemblyTimer.Stop();
                LoadPluginAssembly();
            }

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {

        }

        /// <summary>
        /// This is called when the filter is shut down. This happens once when the game is closing.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                ServerDispatch -= SuitCollectorHotReload_ServerDispatch;
                Core.PluginInitComplete -= Core_PluginInitComplete;
                Core.PluginTermComplete -= Core_PluginTermComplete;
                Core.RenderFrame -= Core_RenderFrame;
                UnloadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

       
        #endregion

        private void Core_PluginInitComplete(object sender, EventArgs e)
        {
            try
            {
                pluginsReady = true;
                LoadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

        internal void SuitCollectorHotReload_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0xF658) // Character List
            {
                characterSlots = Convert.ToInt32(e.Message["slotCount"]);

                characters.Clear();

                MessageStruct charactersStruct = e.Message.Struct("characters");

                for (int i = 0; i < charactersStruct.Count; i++)
                {
                    
                    int character = Convert.ToInt32(charactersStruct.Struct(i)["character"]);
                    string name = Convert.ToString(charactersStruct.Struct(i)["name"]);
                    int deleteTimeout = Convert.ToInt32(charactersStruct.Struct(i)["deleteTimeout"]);
                    Utils.WriteLog("adding char " + name + " index " + i);
                    characters.Add(new Character(character, name, deleteTimeout));
                }

                characters.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
            }
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            try
            {
                //Utils.WriteLog("Unloading assembly");
                //pluginsReady = false;
                //UnloadPluginAssembly();
            }
            catch (Exception ex) { Utils.LogException(ex); }
        }

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
                    Host,
                    characterSlots,
                    characters.ToArray()
                }) ;

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
