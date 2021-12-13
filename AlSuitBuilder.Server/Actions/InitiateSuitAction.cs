using AlSuitBuilder.Server.Data;
using AlSuitBuilder.Shared.Messages.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                            var endToTrim = requirementsText.LastIndexOf(")");

                            requirementsText = requirementsText.Substring(startToTrim, endToTrim - startToTrim);

                            workItem.Requirements = requirementsText.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

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
                        
                        workItems.RemoveAll(o=>o.Character == clientInfo.CharacterName);
                        var missing = workItems.Select(o => o.Character).Except(characters);
                        
                        if (missing.Any())
                        {
                            responseMessage = $"No client(s) running for {string.Join(",", missing)}";
                        }
                        else
                        {

                            success = true;
                            responseMessage = $"Starting Build [{_suitName}] Will attempt processing {workItems.Count} items.";

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
