using AlSuitBuilder.Plugin.Integrations;
using AlSuitBuilder.Shared;
using AlSuitBuilder.Shared.Messages.Server;
using AlSuitBuilder.Shared.Messages.Client;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using AlSuitBuilder.Plugin.Actions.Queue;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Reflection;

namespace AlSuitBuilder.Plugin
{

    public class SuitBuilderPlugin
    {


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hhwnd, uint msg, IntPtr wparam, UIntPtr lparam);

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;

        public static SuitBuilderPlugin Current { get { return _instance; } }

        internal SuitBuilderType PluginType { get; private set; } = SuitBuilderType.Unknown;
        internal SuitBuilderState PluginState { get; private set; } = SuitBuilderState.Unknown;
        internal NetServiceHost PluginHost { get; private set; } = null;
        public string SwapCharacter { get; internal set; }

        private static SuitBuilderPlugin _instance;
        private int _characterSlots;
        private Character[] _characters;
        private string _directory;
        private CoreManager _core = null;

        private List<QueuedAction> _actionQueue = new List<QueuedAction>();

        private NetworkProxy _net = new NetworkProxy();
        private bool _runningAllegiance = false;

        public void Startup(NetServiceHost host, int characterSlots, Character[] characters)
        {
            if (_instance != null)
            {
                return;
            }
            _instance = this;
            _characterSlots = characterSlots;
            _characters = characters;

            string asm = string.Empty;

            try
            {
                asm = Assembly.GetCallingAssembly().Location;

            _directory = new FileInfo(asm).DirectoryName;
            }
            catch (Exception ex)
            {
                Utils.WriteToChat("Unable to parse directory [asm]" + asm + " [ex]" + ex.Message);
            }

            PluginHost = host;
            PluginHost.Actions.AddChatText("Builders Startup", 1);
            Utils.WriteLog("Startup - queue clear");
            _actionQueue.Clear();

            _core = CoreManager.Current;
            _core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff += CharacterFilter_Logoff;
            _core.CommandLineText += _core_CommandLineText;
            _core.ChatBoxMessage += _core_ChatBoxMessage;


            if (_core.CharacterFilter.LoginStatus == 3)
            {
                _net = new NetworkProxy();

                _net.OnWelcomeMessage += Net_OnWelcomeMessage;
                _net.OnGiveItemMessage += Net_OnGiveItemMessage;

                _net.Startup();
            }


        }


        private string _lastPatron = string.Empty;

        private void _core_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {

            if (!_runningAllegiance || _allegianceTree.Count == 0)
                return;


            var infoOn = "Allegiance information for ";


            if (e.Text.StartsWith(infoOn))
            {
                var name = e.Text.Substring(infoOn.Length);
                name = name.Substring(name.IndexOf(" ") + 1);
                name = name.Substring(0, name.Length - 2);
                
                bool online = name.EndsWith("*");
                name = name.Replace(" *", "");

                var match = _allegianceTree.FirstOrDefault(o => o.Name == name);
                if (match == null)
                {
                    Utils.WriteToChat("Invalid parsing...");
                    return;
                }
                match.Scanned = true;
                match.Online = online;
                _lastPatron = match.Name;

            }

            if (e.Text.StartsWith("      "))
            {
                var name = e.Text.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    var ranks = new List<string>() { "Void Lord", "High King", "High Queen", "Grand Duke", "Grand Duchess" };
                    var rankSpaces = 0;

                    if (ranks.Contains(name))
                        rankSpaces = 1;


                    name = name.Substring(name.IndexOf(' ') + 1);

                    if (rankSpaces > 0)
                        name = name.Substring(name.IndexOf(' ') + 1);


                    bool online = name.EndsWith("*");
                    name = name.Replace(" *", "");

                    Utils.WriteToChat("Adding an allegiance member to track " + name);
                    _allegianceTree.Add(new AllegianceTrackItem() { Name = name, Online = online, Scanned = false, Patron = _lastPatron });
                }
            }



        }

        private void _core_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            if (e == null || string.IsNullOrEmpty(e.Text))
                return;

            if (!e.Text.StartsWith("/alb "))
                return;

            e.Eat = true;
            AddAction(new GenericWorkAction(() => ProcessChatCommand(e.Text)));
        }

        private void ProcessChatCommand(string commandText)
        {

            var parts = commandText.Split(' ');
            if (parts.Length < 2)
            {
                Utils.WriteToChat("Invalid /alb command");
                return;
            }

            var cmd = parts[1];
            switch (cmd)
            {
                case "online":
                    
                    if (parts.Length < 4)
                    {

                        Utils.WriteToChat("Invalid format. /alb online filename.csv playerToStartWith");
                        return;
                    }
                    _allegianceFile = parts[2].Trim();
                    if (!_allegianceFile.ToLower().EndsWith(".csv"))
                        _allegianceFile += ".csv";

                    _allegianceFile = Path.Combine(_directory, _allegianceFile);

                       
                    _allegianceTree.Clear();
                    _runningAllegiance = true;


                    _allegianceTree.Add(new AllegianceTrackItem() { Name = String.Join(" ", parts.Skip(3).ToArray()) });
                    break;
                case "build":
                    if (parts.Length != 3)
                    {
                        Utils.WriteToChat("Format: /alb build suitname");
                        return;
                    }
                    Utils.WriteToChat("Building " + parts[2]);
                    AddAction(new GenericWorkAction(() => SendInitiateBuild(parts[2])));
                    break;
                case "cancel":
                    Utils.WriteToChat("Cancelling current job if it exists");
                    AddAction(new GenericWorkAction(() => SendCancelBuild()));

                    break;
                default:
                    Utils.WriteToChat("Unknown /alb command");
                    break;
            }
        }


        private List<AllegianceTrackItem> _allegianceTree = new List<AllegianceTrackItem>();
        private string _allegianceFile;

        private class AllegianceTrackItem
        {
            public string Name { get; set; }
            public bool Online { get; set; }
            public bool Scanned { get; set; }
            public string Patron { get; set; }
        }

        private void RequestAllegianceInfo(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Alastrius";

            Utils.WriteToChat($"[CMD] /allegiance info {name}");
            Utils.DispatchChatToBoxWithPluginIntercept($"/allegiance info {name}");
            // add in a fake delay to back up the request queue a bit
            AddAction(new DelayedAction(2000, () => { CheckFinalizeAllegiance(0); }));
        }

        private void Net_OnGiveItemMessage(GiveItemMessage message)
        {
            AddAction(new WaitForIDAction(() => ProcessGiveItem(message)));
        }

        public void ProcessGiveItem(GiveItemMessage message, int retryNumber = 0)
        {
            Utils.WriteToChat("Working request to deliver " + message.ItemName);
            Nullable<int> objectId = null;

            if (retryNumber > 5)
            {
                Utils.Decal_DispatchOnChatCommand($"/w {message.DeliverTo}, Item Failed. {message.ItemName} maximum retries attempted.");
                AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                return;
            }

            try
            {

                var objectIds = FindItemsByName(message.ItemName);

                if (!objectIds.Any())
                {
                    AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                    Utils.Decal_DispatchOnChatCommand($"/w {message.DeliverTo}, Item Failed. {message.ItemName} is not on me.");
                    return;
                }

                var triggedID = false;
                objectId = ItemsWithRequirements(objectIds, message.RequiredSpells.ToList(), message.MaterialId, message.SetId, out triggedID);

                if (triggedID)
                {
                    AddAction(new WaitForIDAction(() => ProcessGiveItem(message, retryNumber + 1)));
                    return;
                }


                if (objectId == null)
                {
                    Utils.Decal_DispatchOnChatCommand($"/w {message.DeliverTo}, Item Failed. {message.ItemName} is not on me with matching requirements.");
                    AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                    return;
                }

                var destination = FindTargetPlayer(message.DeliverTo);
                if (destination == null || destination == -1)
                {
                    if (retryNumber >= 4) return;

                    Utils.WriteToChat($"Destination player[{message.DeliverTo}] not found. Attempting /hom and retry in 20 seconds.");
                    AddAction(new GenericWorkAction(() => Utils.DispatchChatToBoxWithPluginIntercept($"/w {message.DeliverTo}, I am not nearby to deliver {message.ItemName}[{objectId}]. I will recall and try again soon.")));
                    AddAction(new DelayedAction(100, () => Utils.Decal_DispatchOnChatCommand("/hom")));
                    AddAction(new DelayedAction(200, () => Utils.DispatchChatToBoxWithPluginIntercept("/hom")));
                    AddAction(new DelayedAction(300, () => Utils.Decal_DispatchOnChatCommand("/hom")));
                    AddAction(new DelayedAction(400, () => Utils.DispatchChatToBoxWithPluginIntercept("/hom")));

                    AddAction(new DelayedAction(20000, () => ProcessGiveItem(message, 4)));
                    return;
                }

                Utils.WriteToChat("Attempting give");
                CoreManager.Current.Actions.GiveItem(objectId.Value, destination.Value);

                AddAction(new DelayedAction(5000, () =>
                {

                    if (CoreManager.Current.WorldFilter.GetInventory().Any(o => o.Id == objectId.Value))
                    {
                        AddAction(new GenericWorkAction(() => ProcessGiveItem(message, ++retryNumber)));
                    }
                    else
                    {
                        AddAction(new GenericWorkAction(() => SendGiveComplete(true, message)));
                    }


                }));



            }
            catch (Exception ex)
            {
                Utils.WriteToChat(ex.Message);
            }

        }

        private void SendGiveComplete(bool success, GiveItemMessage giveMessage)
        {
            _net.Send(new WorkResultMessage() { WorkId = giveMessage.WorkId, Success = success });
        }


        private QueuedAction GetQueuedAction()
        {
            if (!_actionQueue.Any())
                return null;



            // temporary stuffing allegiance in here
            var next = _actionQueue.FirstOrDefault(o => o.CanExecute());
            if (next == null)
            {
                return null;
            }

            Console.WriteLine("Removing an action with id " + next.Id);
            _actionQueue.RemoveAll(o => o.Id == next.Id);

            return next;

        }

        /// <summary>
        /// The message message should be sent from the server upon a successful connection.
        /// </summary>
        /// <param name="welcomeMessage"></param>
        private void Net_OnWelcomeMessage(WelcomeMessage welcomeMessage)
        {
            Utils.WriteToChat("Welcome received.");

            var state = SuitBuilderState.Idle;

            // send a request for action
            state = SuitBuilderState.Waiting;
            AddAction(new GenericWorkAction(() => SendReadyForWork()));

            SetState(state);
        }

        public void AddAction(QueuedAction action)
        {
            Utils.WriteToChat("Action added");
            _actionQueue.Add(action);
        }


        public void Tick()
        {
            _net.Tick();

            try
            {
                if (_actionQueue.Any())
                {
                    var action = GetQueuedAction();
                    if (action != null)
                    {
                        Utils.WriteLog("Executing an action from queue");
                        action.Execute();
                    }

                }
                else
                {
                    if (_runningAllegiance && _allegianceTree.Any(o => !o.Scanned))
                        _actionQueue.Add(new DelayedAction(500, () => RequestAllegianceInfo(_allegianceTree.First(o => !o.Scanned).Name)));
                }
            }
            catch (Exception ex)
            {
                Utils.LogException(ex);
            }

        }


        private void CheckFinalizeAllegiance(int counter)
        {

            Utils.WriteToChat("checking finalize for " + counter);

            if (counter >= 1)
            {
                if (_allegianceTree.All(o => o.Scanned))
                {
                    _runningAllegiance = false;
                    Utils.WriteToChat("[ONLINE] Exporting to csv...");


                    var sb = new StringBuilder();
                    foreach (var member in _allegianceTree.Where(o=>o.Online))
                    {
                        sb.AppendLine($"{member.Name},{member.Patron}");
                    }
                    File.WriteAllText(_allegianceFile, sb.ToString());
                    Utils.WriteToChat("[ONLINE] Completed");

                    _allegianceTree.Clear();
                }
               
                return;
            }

            counter++;

            if (_allegianceTree.All(o => o.Scanned))
                AddAction(new DelayedAction(5000, () => { CheckFinalizeAllegiance(counter); }));
           



        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            PluginHost.Actions.AddChatText("[AlSuitBuilder] Initializing", 1);
            _net = new NetworkProxy();
            _net.OnWelcomeMessage += Net_OnWelcomeMessage;
            _net.OnGiveItemMessage += Net_OnGiveItemMessage;

            _net.Startup();

        }

        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            _net?.Shutdown();

            if (!string.IsNullOrEmpty(SwapCharacter))
            {
                var swapto = SwapCharacter;
                AddAction(new DelayedAction(20000, () => { Utils.WriteLog("Delay Action Called"); LoginCharacter(swapto); }));
                SwapCharacter = String.Empty;


            }
        }


        private const int XPixelOffset = 121;
        private const int YTopOfBox = 209;
        private const int YBottomOfBox = 532;

        public bool LoginCharacter(string name)
        {

            Utils.WriteLog($"Running LoginCharacter({name}) against " + _characters.Length);
            for (int i = 0; i < _characters.Length; i++)
            {
                Utils.WriteLog($"Checking {name} vs {_characters[i].Name}");
                if (_characters[i].Name.StartsWith("+") && !name.StartsWith("+"))
                {
                    if (String.Compare(_characters[i].Name.TrimStart('+'), name, StringComparison.OrdinalIgnoreCase) == 0)
                        return LoginByIndex(i);
                }
                else
                {
                    if (String.Compare(_characters[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                        return LoginByIndex(i);
                }
            }

            Utils.WriteLog("No match");
            return false;
        }

        private bool LoginByIndex(int index)
        {


            if (index >= _characters.Count())
                return false;

            float characterNameSize = (YBottomOfBox - YTopOfBox) / (float)_characterSlots;

            int yOffset = (int)(YTopOfBox + (characterNameSize / 2) + (characterNameSize * index));

            Utils.WriteLog("Sending first click");
            // Select the character
            SendMouseClick(XPixelOffset, yOffset);

            // Click the Enter button
            Utils.WriteLog("Sending second click");
            SendMouseClick(0x015C, 0x0185);

            return true;
        }

        private void SendMouseClick(int x, int y)
        {
            int loc = (y * 0x10000) + x;

            PostMessage(CoreManager.Current.Decal.Hwnd, WM_MOUSEMOVE, (IntPtr)0x00000000, (UIntPtr)loc);
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_LBUTTONDOWN, (IntPtr)0x00000001, (UIntPtr)loc);
            PostMessage(CoreManager.Current.Decal.Hwnd, WM_LBUTTONUP, (IntPtr)0x00000000, (UIntPtr)loc);
        }

        public void Shutdown()
        {

            _net?.Shutdown();

            _core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff -= CharacterFilter_Logoff;

            _net.OnWelcomeMessage -= Net_OnWelcomeMessage;
            _net.OnGiveItemMessage -= Net_OnGiveItemMessage;
            _core.CommandLineText -= _core_CommandLineText;
            _core.ChatBoxMessage -= _core_ChatBoxMessage;


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

            var characters = new List<string>();
            for (int i = 0; i < _core.CharacterFilter.Characters.Count; i++)
            {
                characters.Add(_core.CharacterFilter.Characters[i].Name);
            }

            _net.Send(new ReadyForWorkMessage()
            {
                Account = _core.CharacterFilter.AccountName,
                Server = _core.CharacterFilter.Server,
                Character = _core.CharacterFilter.Name,
                AllCharacters = characters.ToArray()
            });
        }

        private void SendInitiateBuild(string buildName)
        {
            _net.Send(new InitiateBuildMessage() { SuitName = buildName });
        }
        private void SendCancelBuild()
        {

            _net.Send(new TerminateBuildMessage());
        }

        internal List<int> FindItemsByName(string name)
        {
            var matches = new List<int>();

            foreach (var wo in CoreManager.Current.WorldFilter.GetInventory())
            {
                if (String.Equals(wo.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(wo.Id);
                }
            }

            return matches;
        }

        internal int? ItemsWithRequirements(List<int> objectIds, List<int> requiredSpells, int materialId, int armorSetId, out bool triggeredId)
        {

            var ids = String.Join(",", objectIds.Select(o => o.ToString()).ToArray());
            var reqIds = String.Join(",", requiredSpells.Select(o => o.ToString()).ToArray());

            Utils.WriteToChat($"ItemRequirements: [IDs: { ids } ] [required: {reqIds}] [mat: {materialId}] [set: {armorSetId}]");

            if (requiredSpells == null)
                requiredSpells = new List<int>();

            var matches = CoreManager.Current.WorldFilter.GetInventory().Where(inv => objectIds.Contains(inv.Id));
            int? result = null;
            triggeredId = false;



            foreach (var item in matches.Where(m => !m.HasIdData).ToList())
            {

                triggeredId = true;
                CoreManager.Current.Actions.RequestId(item.Id);
            }

            if (triggeredId)
                return null;

            foreach (var item in matches)
            {

                if (materialId > 0 && item.Values(LongValueKey.Material) != materialId)
                    continue;

                var itemSpells = new List<int>();


                for (int i = 0; i < item.SpellCount; i++)
                {
                    itemSpells.Add(item.Spell(i));
                }

                var missing = requiredSpells.Except(itemSpells);

                if (missing.Any())
                {
                    Utils.WriteToChat("Item matched but not requirements " + String.Join(",", missing.Select(o => o.ToString()).ToArray()) + " had " + String.Join(",", itemSpells.Select(o => o.ToString()).ToArray()));
                    continue;
                }

                var foundSet = item.Values(LongValueKey.ArmorSet);

                if (armorSetId > 0 && foundSet != armorSetId)
                {
                    Utils.WriteToChat("Potential item found but wrong armor set");
                    continue;
                }

                result = item.Id;
                break;


            }

            return result;

        }

        internal int? FindTargetPlayer(string name)
        {

            var woc = CoreManager.Current.WorldFilter.GetByName(name);
            if (woc == null) return null;

            return woc.FirstOrDefault() != null ? woc.First().Id : -1;
        }



    }
}
