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
        }

        public void SendGameData(byte[] data, bool directSend = false)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.GameData);
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
                        gameMode.OnStatusChange(message.SenderConnection, message.SenderConnection.Status);
                        if (message.SenderConnection.Status == NetConnectionStatus.Connected)
                        {
                            byte[] data = gameMode.HandShake();

                            NetOutgoingMessage outmessage = server.CreateMessage();

                            var memory = new MemoryStream();
                            var writer = new BinaryWriter(memory);

                            writer.Write((byte) Protocol.GameData);
                            writer.Write((byte) Gamemode.Signature.Handshake);
                            writer.Write(data);

                            outmessage.Write(memory.ToArray());
                            server.SendMessage(outmessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                            memory.Close();
                            writer.Close();
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
    }
}