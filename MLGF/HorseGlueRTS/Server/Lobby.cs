using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Server.GameModes;
using Shared;
using System.IO;

namespace Server
{
    class Lobby
    {
        private byte idToGive;
        public List<NetConnection> clients;
        private GameServer myServer;
        public byte MaxSlots;
        public string Name;

        public Lobby(GameServer server)
        {
            MaxSlots = 8;
            idToGive = 0;
            myServer = server;
            clients = new List<NetConnection>();
            Name = "Diamond Tiaras Death Room";
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


            memory = new MemoryStream();
            writer = new BinaryWriter(memory);
            writer.Write((byte)LobbyProtocol.SendAllPlayers);
            writer.Write(AllPlayersData());
            myServer.SendLobbyData(memory.ToArray());

            SendData(new byte[1]{MaxSlots}, LobbyProtocol.SendMaxSlots );

            memory = new MemoryStream();
            writer = new BinaryWriter(memory);
            writer.Write(Name);
            SendData(memory.ToArray(), LobbyProtocol.SendLobbyName);
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
                        UpdatePlayer(LobbyPlayer);
                    }
                    break;
                case LobbyProtocol.IsReady:
                    {
                        LobbyPlayer.IsReady = reader.ReadBoolean();
                        UpdatePlayer(LobbyPlayer);
                    }
                    break;
                case LobbyProtocol.StartGame:
                    break;
                case LobbyProtocol.SetHost:
                    break;
                case LobbyProtocol.ChangeName:
                    {
                        LobbyPlayer.Name = reader.ReadString();
                        UpdatePlayer(LobbyPlayer);
                    }
                    break;
            }
        }

        const byte REQUIRED_PLAYERS_TO_START_GAME = 2;
        public void Update()
        {
            bool hasHost = false;
            uint readyCount = 0;
            foreach (var netConnection in clients)
            {
                var LobbyPlayer = (LobbyPlayer) netConnection.Tag;
                if (LobbyPlayer.IsHost)
                {
                    hasHost = true;
                }
                if (LobbyPlayer.IsReady) readyCount++;
            }
            if(hasHost == false && clients.Count != 0)
            {
                var LobbyPlayer = (LobbyPlayer) clients[0].Tag;
                LobbyPlayer.IsHost = true;
                UpdatePlayer(LobbyPlayer);
            }


            if(clients.Count >= REQUIRED_PLAYERS_TO_START_GAME && readyCount == clients.Count)
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            SendData(new byte[0], LobbyProtocol.SwitchingToGame, true);
            myServer.SwitchToGame(new StandardMelee(myServer, REQUIRED_PLAYERS_TO_START_GAME));
        }

        public void UpdatePlayer(LobbyPlayer player)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(player.Id);
            writer.Write(player.ToBytes());

            SendData(memory.ToArray(), LobbyProtocol.UpdatePlayer);
        }

        public void SendData(byte[] data, LobbyProtocol protocol, bool direct = false)
        {
            var memory = new MemoryStream();
            
            var writer = new BinaryWriter(memory);
            writer.Write((byte) protocol);
            writer.Write(data);
            myServer.SendLobbyData(memory.ToArray(), direct);
        }

        public byte[] AllPlayersData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) clients.Count);
            foreach (var netConnection in clients)
            {
                var lobbyPlayer = (LobbyPlayer) netConnection.Tag;
                writer.Write(lobbyPlayer.ToBytes());
            }


            return memory.ToArray();
        }
    }
}
