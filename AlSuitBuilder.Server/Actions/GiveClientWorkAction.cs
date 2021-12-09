using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSuitBuilder.Server.Actions
{

    internal class BaseServerAction : IServerAction
    {
        public virtual Action GetAction()
        {
            return null;
        }
    }


    internal class GiveClientWorkAction : IServerAction
    {

        private int _clientId;

        private GiveClientWorkAction() {}

        public static GiveClientWorkAction Create(int clientId)
        {
            return new GiveClientWorkAction() { _clientId = clientId };
        }

        public Action GetAction()
        {
            return () =>
            {

                var clientInfo = Program.GetClientInfo(_clientId);

                if (clientInfo == null) return;

                Console.WriteLine($"Giving work to {clientInfo.AccountName}\\{clientInfo.CharacterName} ");


            };
        }
    }
}
