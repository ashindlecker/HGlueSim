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
        public byte MaxSlots;
        public string Name;
        public string Description;

        public bool FLAG_IsSwitchedToGame;
        public bool FLAG_StartGameState;

        public Dictionary<byte, LobbyPlayer> Players
        {
            get { return players; }
        }

        public Lobby(GameClient client)
        {
            Name = "Lobby Name Not Set";
            Description = "Description Not Set";
            idSet = false;
            myId = 0;

            players = new Dictionary<byte, LobbyPlayer>();
            myclient = client;
            MaxSlots = 0;

            FLAG_IsSwitchedToGame = false;
            FLAG_IsSwitchedToGame = false;
        }

        public void Update()
        {
            
        }

        public void ParseData(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            var lobbyProtocol = (LobbyProtocol) reader.ReadByte();
            Console.WriteLine(lobbyProtocol);
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
                case LobbyProtocol.SendAllPlayers:
                    {
                        var playerCount = reader.ReadByte();
                        for (var i = 0; i < playerCount; i++)
                        {
                            var playerAdd = new LobbyPlayer();
                            playerAdd.Load(memory);
                            if (players.ContainsKey(playerAdd.Id) == false)
                                players.Add(playerAdd.Id, playerAdd);
                        }
                    }
                    break;
                case LobbyProtocol.ChangeName:
                    break;
                case LobbyProtocol.SendMaxSlots:
                    {
                        MaxSlots = reader.ReadByte();
                    }
                    break;
                case LobbyProtocol.SendLobbyName:
                    {
                        Name = reader.ReadString();
                        Description = reader.ReadString();
                    }
                    break;
                case LobbyProtocol.SwitchingToGame:
                    {
                        FLAG_IsSwitchedToGame = true;
                    }
                    break;
                    case LobbyProtocol.StartGameState:
                    {
                        FLAG_StartGameState = true;
                    }
                    break;
                default:
                    break;
            }
        }

        public void SetReady(bool value)
        {
            SendData(new byte[1]{Convert.ToByte(value)}, LobbyProtocol.IsReady );
        }

        public void StartGameSwitchHandShake()
        {
            SendData(new byte[0]{}, LobbyProtocol.LoadedGameState );
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
