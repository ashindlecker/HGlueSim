using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Shared;
using System.IO;

namespace Server
{
    class Lobby
    {

        private List<NetConnection> clients;
        private GameServer myServer;

        public Lobby(GameServer server)
        {
            myServer = server;
            clients = new List<NetConnection>();
        }

        public void AddConnection(NetConnection connection)
        {
            connection.Tag = new LobbyPlayer();
            clients.Add(connection);
        }

        public void ParseProtocol(LobbyProtocol protocol, MemoryStream memory, NetConnection connection)
        {
            var reader = new BinaryReader(memory);
            var LobbyPlayer = (LobbyPlayer) connection.Tag;

            switch (protocol)
            {
                case LobbyProtocol.SetTeam:
                    {
                        var team = reader.ReadByte();
                        LobbyPlayer.Team = team;
                    }
                    break;
                case LobbyProtocol.IsReady:
                    break;
                case LobbyProtocol.StartGame:
                    break;
                case LobbyProtocol.SetHost:
                    break;
            }
        }

        public void Update()
        {
            
        }

    }
}
