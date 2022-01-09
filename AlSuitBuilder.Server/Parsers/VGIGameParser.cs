using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Parsers
{
    internal class VGIGameParser : RegexParser
    {
        public VGIGameParser() : base(new List<Regex>() {
            new Regex(@"(\[VGI\] )?(?<item>[A-Za-z ]+ )(w(?<work>[\d]+)) (?<crap>[A-Za-z \+0-9%]+)?(\[\D+ ?\D+? ?(?<wield>[0-9]+) to \w+\] ?)+(\[(?<cantrips>[A-Za-z, ']*)\] )?(\[(?<set>[A-Za-z0-9']+) set\] ?)?(Value (?<value>\d+)p ?)?\(Last on (?<character>[A-Za-z\d\-' ]*)\)", RegexOptions.Compiled),
            new Regex(@"(?<item>[A-Za-z ']*), (?<set>[A-Za-z']* Set){0,1}, AL (?<armorlevel>[0-9]*), (?<cantrips>[A-Za-z ,]*), Wield (.*), Diff ([0-9]+), (.*) \(Last on (?<character>[A-Za-z0-9 \-']*)\)", RegexOptions.Compiled)
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



            workItem.Value = string.IsNullOrWhiteSpace(groups["value"].Value) ? 0 : Convert.ToInt32(groups["value"].Value.Trim());


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
