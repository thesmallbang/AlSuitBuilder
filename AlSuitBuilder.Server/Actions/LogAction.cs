using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Actions
{
    internal class LogAction : IServerAction
    {
        private string _message;

        private LogAction() {}


        public static LogAction Create(string message)
        {
            return new LogAction()
            {
                _message = message
            };
        }

        public Action GetAction() => () => Console.WriteLine(_message);
    }
}
