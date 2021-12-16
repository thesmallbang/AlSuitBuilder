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
        private CoreManager _core = null;

        private List<QueuedAction> _actionQueue = new List<QueuedAction>();

        private NetworkProxy _net = new NetworkProxy();

        public void Startup(NetServiceHost host, int characterSlots, Character[] characters)
        {
            if (_instance != null)
            {
                return;
            }
            _instance = this;
            _characterSlots = characterSlots;
            _characters = characters;

            PluginHost = host;
            PluginHost.Actions.AddChatText("Builders Startup", 1);
            Utils.WriteLog("Startup - queue clear");
            _actionQueue.Clear();

            _core = CoreManager.Current;
            _core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
            _core.CharacterFilter.Logoff += CharacterFilter_Logoff;
            _core.CommandLineText += _core_CommandLineText;


            if (_core.CharacterFilter.LoginStatus == 3)
            {
                _net = new NetworkProxy();

                _net.OnWelcomeMessage += Net_OnWelcomeMessage;
                _net.OnGiveItemMessage += Net_OnGiveItemMessage;

                _net.Startup();
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
                Utils.WriteToChat("Timed out give");
                AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                return;
            }

            try
            {

                var objectIds = FindItemsByName(message.ItemName);

                if (!objectIds.Any())
                {
                    AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                    Utils.WriteToChat($"No item matches for {message.ItemName}");
                    return;
                }

                var triggedID = false;
                objectId = ItemsWithRequirements(objectIds, message.RequiredSpells.ToList(), message.MaterialId, message.SetId, out triggedID);

                if (triggedID)
                {
                    AddAction(new WaitForIDAction(() => ProcessGiveItem(message, retryNumber+1)));
                    return;
                }


                if (objectId == null)
                {
                    Utils.WriteToChat("Matching item was not found on character");
                    AddAction(new GenericWorkAction(() => SendGiveComplete(false, message)));
                    return;
                }

                var destination = FindTargetPlayer(message.DeliverTo);
                if (destination == null)
                {
                    Utils.WriteToChat($"Destination player[{message.DeliverTo}] not found");
                    return;
                }
                                

                    Utils.WriteToChat("Attempting give");
                    CoreManager.Current.Actions.GiveItem(objectIds[0], destination.Value);

                    AddAction(new DelayedAction(2000, () =>
                    {

                        if (CoreManager.Current.WorldFilter.GetInventory().Any(o => o.Id == objectId))
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

            var next = _actionQueue.FirstOrDefault(o => o.CanExecute());
            if (next == null)
                return null;

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
            }
            catch (Exception ex)
            {
                Utils.LogException(ex);
            }

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
                AddAction(new DelayedAction(10000, () => { Utils.WriteLog("Delay Action Called");  LoginCharacter(swapto); }));
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
                    Utils.WriteToChat("Adding an initial item match by name");
                    matches.Add(wo.Id);
                }
            }

            return matches;
        }

        internal int? ItemsWithRequirements(List<int> objectIds, List<int> requiredSpells, int materialId, int armorSetId, out bool triggeredId)
        {

            if (requiredSpells == null)
                requiredSpells = new List<int>();

            var matches = CoreManager.Current.WorldFilter.GetInventory().Where(inv => objectIds.Contains(inv.Id));
            int? result = null;
            triggeredId = false;


            Utils.WriteToChat("Need ID for " + matches.Where(m => !m.HasIdData).ToList().Count + " of " + matches.Count());
           

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
