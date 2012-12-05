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
        private byte idToGive;
        private List<NetConnection> clients;
        private GameServer myServer;

        public Lobby(GameServer server)
        {
            idToGive = 0;
            myServer = server;
            clients = new List<NetConnection>();
        }

        public void AddConnection(NetConnection connection)
        {
            var lobbyPlayer = new LobbyPlayer();
            lobbyPlayer.Id = idToGive;
            connection.Tag = lobbyPlayer;
            clients.Add(connection);
            idToGive++;

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
            writer.Write((byte) LobbyProtocol.SetID);
            writer.Write(lobbyPlayer.Id);
            myServer.SendLobbyData(memory.ToArray(), connection);
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

        public void UpdatePlayer(LobbyPlayer player)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(player.Id);
            writer.Write(player.ToBytes());

            SendData(memory.ToArray(), LobbyProtocol.UpdatePlayer);
        }

        public void SendData(byte[] data, LobbyProtocol protocol)
        {
            var memory = new MemoryStream();
            
            var writer = new BinaryWriter(memory);
            writer.Write((byte) protocol);
            writer.Write(data);
            myServer.SendLobbyData(memory.ToArray());
        }
    }
}
