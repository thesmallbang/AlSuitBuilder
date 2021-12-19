using AlSuitBuilder.Server.Data;
using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AlSuitBuilder.Shared;

namespace AlSuitBuilder.Server.Actions
{
    internal class InitiateSuitAction : IServerAction
    {
        private int _clientId;
        private string _suitName;

        public InitiateSuitAction(int clientId, string suitName)
        {
            _clientId = clientId;
            _suitName = suitName;
        }

        public void Execute()
        {

            var clientInfo = Program.GetClientInfo(_clientId);
            if (clientInfo == null)
                return;

            var success = false;
            var responseMessage = string.Empty;

            if (Program.BuildInfo != null)
            {
                responseMessage = "Build already in progress";
            }
            else
            {


                var filename = Path.Combine(Program.BuildDirectory, _suitName.Replace(".alb", "") + ".alb");
                if (!File.Exists(filename))
                {
                    responseMessage = "Suit not found " + filename;
                }
                else
                {

                    var lines = File.ReadAllLines(filename);
                    var workItems = new List<WorkItem>();
                    var id = 0;
                    foreach (var line in lines)
                    {

                        var parts = line.Replace("Item on ", "").Split(':');
                        if (parts.Length != 3)
                            continue;

                        var requirementsText = parts[2];
                        var requirements = new List<string>();
                        var workItem = new WorkItem()
                        {
                            Id = ++id,
                            Character =  parts[0].Trim(),
                            ItemName = parts[1].Trim(),
                        };
                        if (requirementsText.EndsWith(")"))
                        {
                            var startToTrim = requirementsText.IndexOf("(");

                            requirementsText = requirementsText.Substring(0,  startToTrim);
                            workItem.Requirements = requirementsText.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Program.SpellData.SpellIdByName(x.Trim())).Where(p=>p != -1).ToArray();
                                                        
                            foreach (var info in Shared.Dictionaries.MaterialInfo)
                            {
                                if (workItem.ItemName.StartsWith(info.Value))
                                {
                                    workItem.MaterialId = info.Key;
                                    workItem.ItemName = workItem.ItemName.Substring(info.Value.Length + 1);
                                    break;
                                }
                            }

                            foreach (var info in Shared.Dictionaries.SetInfo)
                            {
                                if (requirementsText.Contains(info.Value))
                                {
                                    workItem.SetId = info.Key;
                                    break;
                                }
                            }

                        }

                        workItems.Add(workItem);

                    }


                    if (!workItems.Any())
                    {
                        responseMessage = "Suit file was found but no valid items were found. Please make sure the format is correct";
                    }
                    else
                    {

                        var characters = new List<string>();
                        foreach (var clientId in Program.GetClientIds())
                        {
                            var client = Program.GetClientInfo(clientId);
                            if (client == null)
                                continue;

                            characters.Add(client.CharacterName);
                            characters.AddRange(client.OtherCharacters);
                        }

                        var itemsOnBuilderChar = workItems.Select(o => o.Character == clientInfo.CharacterName ? o : null).Where(o => o != null);
                        if (itemsOnBuilderChar.Count() > 0)
                        {
                            foreach (var itemOnChar in itemsOnBuilderChar)
                                Utils.WriteWorkItemToLog($"Excluding due to being on current character ({clientInfo.CharacterName})", itemOnChar, true);
                        }

                        workItems.RemoveAll(o => o.Character == clientInfo.CharacterName);
                        var missing = workItems.Select(o => o.Character).Except(characters);
                        
                        if (missing.Any())
                        {
                            responseMessage = $"No client(s) running for {string.Join(",", missing)}";
                        }
                        else
                        {

                            success = true;
                            responseMessage = $"Starting Build [{_suitName}] Will attempt processing {workItems.Count} items.";
                            foreach (var workItem in workItems)
                                Utils.WriteWorkItemToLog($"Parsed successfully from suit", workItem, true);

                            Program.BuildInfo = new BuildInfo()
                            {
                                InitiatedId = _clientId,
                                DropCharacter = clientInfo.CharacterName,
                                StartTime = DateTime.Now,
                                Name = _suitName,
                                WorkItems = workItems
                            };
                        }

                    }
                }


            }

            Program.SendMessageToClient(_clientId, new InitiateBuildResponseMessage()
            {
                Accepted = success,
                Message = responseMessage
            });

        }
    }
}
