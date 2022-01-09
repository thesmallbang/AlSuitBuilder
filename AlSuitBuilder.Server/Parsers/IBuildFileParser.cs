using AlSuitBuilder.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Parsers
{
    internal interface IBuildFileParser
    {
        WorkItem Process(string line);
        bool IsValid(string line, out string msg);

    }
}
