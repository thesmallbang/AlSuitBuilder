using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AlSuitBuilder.Server.Data
{
    public class SpellData
    {
        public List<SpellDataItem> Spells = new List<SpellDataItem>();

        public SpellData()
        {

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AlSuitBuilder.Server.EmbeddedContent.Spells.csv"))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var fileLines = reader.ReadToEnd().Split('\n');

                        for (int i = 1; i < fileLines.Count(); i++)
                        {
                            var props = fileLines[i].Split(',');
                            Spells.Add(new SpellDataItem() { Id =  Convert.ToInt32(props[0]), Name = props[1] });
                        }

                    }
                }
            }


        }


        public string SpellNameById(int id)
        {
            var spell = Spells.FirstOrDefault(o => o.Id == id);
            return (spell == null) ? string.Empty : spell.Name;

        }

        public int SpellIdByName(string name)
        {
            var spell = Spells.FirstOrDefault(o => o.Name == name);
            if (spell != null)
                return spell.Id;
            
            Utils.WriteToChat($"Unable to find any SpellId for Cantrip with name: {name}");
            return -1;
        }

    }
}
