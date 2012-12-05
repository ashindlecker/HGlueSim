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
        private Dictionary<byte, LobbyPlayer> players;
        private GameClient myclient;
        private byte myId;
        private bool idSet;

        public Lobby(GameClient client)
        {
            idSet = false;
            myId = 0;

            players = new Dictionary<byte, LobbyPlayer>();
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
                case LobbyProtocol.UpdatePlayer:
                    {
                        var id = reader.ReadByte();
                        if (players.ContainsKey(id))
                        {
                            players[id].Load(memory);
                        }
                    }
                    break;
                case LobbyProtocol.SetTeam:

                    break;
                case LobbyProtocol.IsReady:
                    break;
                case LobbyProtocol.StartGame:
                    break;
                case LobbyProtocol.SetHost:
                    break;
                case LobbyProtocol.SetID:
                    {
                        myId = reader.ReadByte();
                        idSet = true;
                    }
                    break;
                default:
                    break;
            }
        }

        public void SendData(byte[] data, LobbyProtocol protocol)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)protocol);
            writer.Write(data);

            myclient.SendData(memory.ToArray());
        }
    }
}
