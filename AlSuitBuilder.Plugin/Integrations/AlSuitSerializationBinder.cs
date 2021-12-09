using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlSuitBuilder.Plugin.Integrations
{
    internal class AlSuitSerializationBinder : SerializationBinder
    {
        private Regex genericRe = new Regex("^(?<gen>[^\\[]+)\\[\\[(?<type>[^\\]]*)\\](,\\[(?<type>[^\\]]*)\\])*\\]$", RegexOptions.Compiled);

        private Regex subtypeRe = new Regex("^(?<tname>.*)(?<aname>(,[^,]+){4})$", RegexOptions.Compiled);

        public override Type BindToType(string assemblyName, string typeName)
        {
            Match match = genericRe.Match(typeName);
            if (match.Success)
            {
                Type flatTypeMapping = GetFlatTypeMapping(match.Groups["gen"].Value);
                Type[] typeArguments = match.Groups["type"].Captures.Cast<Capture>().Select(delegate (Capture c)
                {
                    Match match2 = subtypeRe.Match(c.Value);
                    return BindToType(match2.Groups["aname"].Value.Substring(1).Trim(), match2.Groups["tname"].Value.Trim());
                }).ToArray();
                return flatTypeMapping.MakeGenericType(typeArguments);
            }

            return GetFlatTypeMapping(typeName);
        }

        private Type GetFlatTypeMapping(string typeName)
        {
            Type type = typeof(AlSuiteBuilder.Shared.Utils).Assembly.GetType(typeName);
            type = (((object)type == null) ? GetType().Assembly.GetType(typeName) : type);
            return ((object)type == null) ? Type.GetType(typeName) : type;
        }
    }
}
