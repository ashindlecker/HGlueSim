using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Shared;
using System.IO;
using Server.GameModes;
using System.Diagnostics;


namespace Server
{
    class GameServer
    {
        private NetServer server;
        private GameModeBase gameMode;

        public GameServer(int port)
        {
            var configuration = new NetPeerConfiguration("HORSEGLUERTS");
            configuration.Port = port;
            server = new NetServer(configuration);
        }

        public void SetGame(GameModeBase game)
        {
            gameMode = game;
        }

        public void Start()
        {
            server.Start();
            server.Socket.Blocking = false;
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

            NetIncomingMessage message = null;
            while ((message = server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        {
                            var memory = new MemoryStream(message.ReadBytes(message.LengthBytes));
                            var reader = new BinaryReader(memory);
                            var protocol = (Shared.Protocol) reader.ReadByte();

                            switch (protocol)
                            {
                                case Protocol.Input:
                                    gameMode.ParseInput(memory, message.SenderConnection);
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
                            var data = gameMode.HandShake();

                            var outmessage = server.CreateMessage();

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
        }

        public void SendGameData(byte[] data)
        {
            var message = server.CreateMessage();

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)Protocol.GameData);
            writer.Write(data);

            message.Write(memory.ToArray());

            server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            memory.Close();
            writer.Close();
        }
        

    }
}
