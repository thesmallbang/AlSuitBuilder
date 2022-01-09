using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Parsers
{
    public abstract class RegexParser : IBuildFileParser
    {

        protected List<Regex> SupportedRegex;


        public RegexParser(List<Regex> supportedRegex)
        {
            SupportedRegex = supportedRegex;
        }

        public virtual bool IsValid(string line, out string msg)
        {
            msg = String.Empty;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            return SupportedRegex.Any(x => x.IsMatch(line));

        }

        public abstract WorkItem Process(string line);
       

        public virtual void ProcessRequirements(WorkItem workItem, string requirements)
        {
            workItem.Requirements = requirements.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Program.SpellData.SpellIdByName(x.Trim())).Where(p => p != -1).ToArray();

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
                if (requirements.Contains(info.Value))
                {
                    workItem.SetId = info.Key;
                    break;
                }
            }
        }
    }
}
