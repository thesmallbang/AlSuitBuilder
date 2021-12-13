using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Actions
{
    internal class ProcessWorkResultAction : IServerAction
    {
        private int _clientId;
        private int _workId;
        private bool _success;

        public ProcessWorkResultAction(int clientId, bool success, int workId)
        {
            _clientId = clientId;
            _workId = workId;
            _success = success;
        }

        public void Execute()
        {
            

        }
    }
}
