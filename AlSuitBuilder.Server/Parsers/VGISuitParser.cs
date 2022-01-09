using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlSuitBuilder.Server.Parsers
{
    internal class VGISuitParser : RegexParser
    {
        public VGISuitParser() : base(new List<Regex>() {
            new Regex(@"(?<character>[A-Za-z\-' ]+), (?<item>[A-Za-z ']*), (?<set>[A-Za-z']* Set){0,1}, AL (?<armorlevel>[0-9]*), (?<cantrips>[A-Za-z ,]*), Wield Lvl (?<wieldreq>[0-9]*), Diff [0-9]+, BU [0-9]+", RegexOptions.Compiled)
        })
        {
        }

        public override WorkItem Process(string line)
        {
            var exp = SupportedRegex.FirstOrDefault(r => r.IsMatch(line));
            if (exp == null) return null;

            var groups = exp.Match(line).Groups;

            if (groups.Count == 0)
                return null;

            var workItem = new WorkItem();

            workItem.ItemName = groups["item"].Value.Trim();
            workItem.Character = groups["character"].Value.Trim();


            workItem.Value = 0;


            var requirements = groups["cantrips"].Value;

            var setName = groups["set"].Value;
            if (!string.IsNullOrWhiteSpace(setName))
            {
                requirements += "," + setName + " Set";
            }

            ProcessRequirements(workItem, requirements);

            return workItem;
        }
    }
}
