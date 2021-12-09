using AlSuitBuilder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuiteBuilder.Shared.Messages
{
    [Serializable]
    public class WelcomeMessage
    {
        public SuiteBuilderState ServerState { get; set; }


    }
}
