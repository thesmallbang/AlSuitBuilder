﻿using AlSuitBuilder.Server.Actions;
using AlSuitBuilder.Server.Data;
using AlSuitBuilder.Shared;
using AlSuitBuilder.Shared.Messages.Client;
using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server
{
    internal class BuildInfo
    {

        // name aka filename
        public string Name { get; set; }

        // initiator for now
        public string DropCharacter { get; set; }

        // the first account done with their items will be designated as the relay character for anything found on the initiators account at the end of the build.
        public string RelayCharacter { get; set; }

        public List<WorkItem> WorkItems { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private List<int> CompletedIds = new List<int>();

        public int InitiatedId { get; set; }



        internal void Tick()
        {


            if (WorkItems.Count == 0)
            {
                Console.WriteLine("Build complete");
                Program.SendMessageToClient(InitiatedId, new InitiateBuildResponseMessage() { Accepted= true, Message = "Build completed" });
                Program.BuildInfo = null;
                return;
            }

            var clientIds = Program.GetClientIds().Except(CompletedIds).ToList();
            if (clientIds.Count == 0)
                return;


            try
            {
                foreach (var clientId in clientIds)
                {
                    var client = Program.GetClientInfo(clientId);
                    if (client == null || string.IsNullOrEmpty(client.CharacterName))
                        continue;

                    var clientWork = WorkItems.Where(o => o.Character == client.CharacterName && o.LastAttempt < DateTime.Now.AddMinutes(-1)).ToList();
                    if (!clientWork.Any())
                    {
                        if (client.OtherCharacters != null && client.OtherCharacters.Any())
                        {
                            clientWork = WorkItems.Where(o => client.OtherCharacters.ToList().Contains(o.Character) && o.LastAttempt < DateTime.Now.AddMinutes(-1)).ToList();
                            if (clientWork.Any())
                            {
                                clientWork.ForEach(o=> o.LastAttempt = DateTime.Now);

                                Console.WriteLine("Sending switch from " + client.CharacterName + " to " + clientWork.First().Character);
                                Program.SendMessageToClient(clientId, new SwitchCharacterMessage() { Character = clientWork.First().Character });
                                // queue up character change
                                continue;
                            }

                            // client has no work on any chars ..

                            if (!WorkItems.Where(o => o.Character == client.CharacterName ||  client.OtherCharacters.ToList().Contains(o.Character)).Any())
                            {
                                Console.WriteLine("Completed Account " + client.CharacterName);
                                CompletedIds.Add(clientId);
                            }
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("No othercharacters for " + client.CharacterName);
                            continue;
                        }
                    }

                    var workItem = clientWork.FirstOrDefault();
                    if (workItem != null)
                    {

                        var itemName = workItem.ItemName;
                        var materialId = 0;

                        foreach (var info in Shared.Dictionaries.MaterialInfo)
                        {
                            if (itemName.StartsWith(info.Value))
                            {
                                materialId = info.Key;
                                itemName = itemName.Substring(info.Value.Length + 1);
                                break;
                            }
                        }


                        // set related work items to not attempt yet.
                        clientWork.ForEach(o => o.LastAttempt = DateTime.Now);

                        Console.WriteLine("Sending work to client for " + workItem.Id + " - " + itemName);
                        Program.SendMessageToClient(clientId, new GiveItemMessage()
                        {
                            WorkId = workItem.Id,
                            ItemName = itemName,
                            MaterialId = materialId,
                            RequiredSpells = workItem.Requirements.Select(r => Program.SpellData.SpellIdByName(r)).Where(rid => rid != -1).ToArray(),
                            DeliverTo = Program.BuildInfo.DropCharacter
                        });
                    }


                }
            }
            catch (Exception ex)
            {

                Utils.LogException(ex);
            }

        }
    }
}