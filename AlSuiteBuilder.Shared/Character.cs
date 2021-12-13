using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlSuitBuilder.Shared
{
    public struct Character
    {
        public readonly int Id;

        public readonly string Name;

        public readonly TimeSpan DeleteTimeout;

        public Character(int id, string name, int timeout)
        {
            Id = id;

            Name = name;

            DeleteTimeout = TimeSpan.FromSeconds(timeout);
        }
    }
}
