using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlSuitBuilder.Server.Parsers
{
    internal class MagParser : RegexParser
    {
        public MagParser() : base(new List<Regex>() {
           // new Regex(@"(?<character>[A-Za-z\-' ]+), (?<item>[A-Za-z ']*), (?<set>[A-Za-z']* Set){0,1}, AL (?<armorlevel>[0-9]*), (?<cantrips>[A-Za-z ,]*), Wield Lvl (?<wieldreq>[0-9]*), Diff [0-9]+, BU [0-9]+", RegexOptions.Compiled),
            new Regex(@"(?<character>[A-Za-z\-' ]+), (?<item>[A-Za-z ']*,) ?(?<set>[A-Za-z']* Set, ?)?(AL (?<armorlevel>[0-9]*), ?)?(?<cantrips>[A-Za-z ,]*,) (Wield Lvl (?<wieldreq>[0-9]*),)? ?([A-Za-z0-9 ]+ to Activate, ?)?(Diff (?<diff>[0-9]+), ?)?(Craft (?<craft>[0-9]+), ?)?(Value (?<value>[0-9,]+),)? ?(BU (?<burden>[0-9]+),?)? ?\[?(?<rating>[A-Z0-9]+)?\]?", RegexOptions.Compiled)
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

            workItem.ItemName = groups["item"].Value.Replace(",","").Trim();
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
