using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shared;


namespace Client
{
    class Lobby
    {
        
        Lis
        private GameClient myclient;

        public Lobby(GameClient client)
        {
            myclient = client;
        }

        public void Update()
        {
            
        }

        public void ParseData(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            var lobbyProtocol = (LobbyProtocol) reader.ReadByte();
            switch (lobbyProtocol)
            {
                case LobbyProtocol.SetTeam:
                    break;
                case LobbyProtocol.IsReady:
                    break;
                case LobbyProtocol.StartGame:
                    break;
                case LobbyProtocol.SetHost:
                    break;
                default:
                    break;
            }
        }
    }
}
