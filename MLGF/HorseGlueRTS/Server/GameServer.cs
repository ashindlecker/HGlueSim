using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lidgren.Network;
using Server.GameModes;
using Shared;

namespace Server
{
    internal class GameServer
    {
        private readonly List<byte> sendBuffer;
        private readonly NetServer server;
        private GameModeBase gameMode;
        private Thread netThread;
        private Lobby lobby;

        public enum ServerStates : byte
        {
            InLobby,
            InGame,
        }

        public ServerStates ServerState;

        public GameServer(int port)
        {
            var configuration = new NetPeerConfiguration("HORSEGLUERTS");
            configuration.Port = port;
            sendBuffer = new List<byte>();
            server = new NetServer(configuration);
            ServerState = ServerStates.InLobby;
            gameMode = null;
            lobby = new Lobby(this);
        }

        private void SendData(byte[] data, byte type, bool directSend = false)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(type);
            writer.Write(data);

            if (!directSend)
                sendBuffer.AddRange(memory.ToArray());
            else
            {
                NetOutgoingMessage outMessage = server.CreateMessage();
                outMessage.Write(memory.ToArray());
                server.SendToAll(outMessage, NetDeliveryMethod.ReliableOrdered);
            }

            memory.Close();
            writer.Close();
        }

        private void SendData(byte[] data, byte type, NetConnection connection)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(type);
            writer.Write(data);

            NetOutgoingMessage outMessage = server.CreateMessage();
            outMessage.Write(memory.ToArray());
            server.SendMessage(outMessage, connection, NetDeliveryMethod.ReliableOrdered);

            memory.Close();
            writer.Close();

        }

        public void SendGameData(byte[] data, bool directSend = false)
        {
            SendData(data, (byte) Protocol.GameData, directSend);
        }

        public void SendLobbyData(byte[] data, bool directSend = false)
        {
            SendData(data, (byte)Protocol.LobbyData, directSend);
        }

        public void SendGameData(byte[] data, NetConnection connection)
        {
            SendData(data, (byte)Protocol.GameData, connection);
        }

        public void SendLobbyData(byte[] data, NetConnection connection)
        {
            SendData(data, (byte)Protocol.LobbyData, connection);
        }

        public void SwitchToGame(GameModeBase gameMode)
        {
            if(ServerState != ServerStates.InLobby) return;
            ServerState = ServerStates.InGame;
            SetGame(gameMode);

            foreach (var netConnection in lobby.clients)
            {
                gameMode.AddConnection(netConnection);
            }
        }

        public void SwitchToLobby()
        {
            if (ServerState != ServerStates.InGame) return;
            ServerState = ServerStates.InLobby;
        }

        public void SetGame(GameModeBase game)
        {
            gameMode = game;
        }

        public void Start()
        {
            server.Start();
            server.Socket.Blocking = false;
            //netThread = new Thread(netThreadLoop);
            //netThread.Start();
        }

        public void Update(float ms)
        {
            if(ServerState == ServerStates.InGame)
            while (ms > 0)
            {
                if (ms > Globals.MAXUPDATETIME)
                {
                    gameMode.Update(Globals.MAXUPDATETIME);
                    ms -= Globals.MAXUPDATETIME;
                }
                else
                {
                    gameMode.Update(ms);
                    ms = 0;
                }
            }
            if(ServerState == ServerStates.InLobby)
                lobby.Update();

            netUpdate();
        }

        private void netThreadLoop()
        {
            while (true)
            {
                netUpdate();
                Thread.Sleep(1);
            }
        }

        private void netUpdate()
        {
            NetIncomingMessage message = null;
            while ((message = server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        {
                            var memory = new MemoryStream(message.ReadBytes(message.LengthBytes));
                            var reader = new BinaryReader(memory);

                            switch (ServerState)
                            {
                                case ServerStates.InLobby:
                                    {
                                        var protocol = (LobbyProtocol)reader.ReadByte();
                                        lobby.ParseProtocol(protocol, memory, message.SenderConnection);
                                    }
                                    break;
                                case ServerStates.InGame:
                                    {
                                        var protocol = (Protocol)reader.ReadByte();

                                        switch (protocol)
                                        {
                                            case Protocol.Input:
                                                gameMode.ParseInput(memory, message.SenderConnection);
                                                break;
                                        }
                                    }
                                    break;
                            }

                            reader.Close();
                            memory.Close();
                        }
                        break;
                    case NetIncomingMessageType.Error:
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        switch (ServerState)
                        {
                            case ServerStates.InLobby:
                                {
                                    if(message.SenderConnection.Status == NetConnectionStatus.Connected)
                                        lobby.AddConnection(message.SenderConnection);
                                }
                                break;
                            case ServerStates.InGame:
                                {
                                    gameMode.OnStatusChange(message.SenderConnection, message.SenderConnection.Status);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        break;
                    case NetIncomingMessageType.Receipt:
                        break;
                    case NetIncomingMessageType.DiscoveryRequest:
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        break;
                    case NetIncomingMessageType.VerboseDebugMessage:
                        break;
                    case NetIncomingMessageType.DebugMessage:
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        break;
                    case NetIncomingMessageType.NatIntroductionSuccess:
                        break;
                    case NetIncomingMessageType.ConnectionLatencyUpdated:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                server.Recycle(message);
            }

            if (sendBuffer.Count > 0)
            {
                NetOutgoingMessage outmessage = server.CreateMessage();

                outmessage.Write(sendBuffer.ToArray());

                server.SendToAll(outmessage, NetDeliveryMethod.ReliableOrdered);
                sendBuffer.Clear();
            }
        }

        private void addLobbyClientsToGame()
        {
            
        }
    }
}