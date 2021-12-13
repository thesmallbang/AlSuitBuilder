using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Shared.Messages.Server
{

    [Serializable]
    public class SwitchCharacterMessage : INetworkMessage
    {
        public string Character;
    }
}
