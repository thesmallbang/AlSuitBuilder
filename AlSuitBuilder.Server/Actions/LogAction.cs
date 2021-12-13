using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AlSuitBuilder.Server.Actions
{
    internal class LogAction : UnclearableAction
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

        public override void Execute() => Console.WriteLine(_message);
    }
}
