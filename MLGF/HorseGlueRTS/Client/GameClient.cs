using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Client.GameModes;
using Lidgren.Network;
using Shared;

namespace Client
{
    internal class GameClient
    {
        private readonly List<uint> bitsPerSecondList;
        private readonly Stopwatch bitsPerSecondTimer;
        public GameModeBase GameMode;
        public InputHandler InputHandler;

        private uint bitsToAdd;
        private NetClient client;

        private Thread networkThread;

        public GameClient()
        {
            GameMode = null;
            InputHandler = new InputHandler(this);

            bitsPerSecondTimer = new Stopwatch();
            bitsPerSecondTimer.Restart();
            bitsPerSecondList = new List<uint>();

            bitsToAdd = 0;
        }

        public void Connect(string ip, int port)
        {
            var config = new NetPeerConfiguration("HORSEGLUERTS");
            client = new NetClient(config);
            client.Start();
            client.Connect(ip, port);

            //networkThread = new Thread(netThreadLoop);
            //networkThread.Start();
        }

        public void SendData(byte[] data)
        {
            NetOutgoingMessage message = client.CreateMessage();
            message.Write(data);
            client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void Update(float ms)
        {
            while (ms > 0)
            {
                if (ms > Globals.MAXUPDATETIME)
                {
                    GameMode.Update(Globals.MAXUPDATETIME);
                    ms -= Globals.MAXUPDATETIME;
                }
                else
                {
                    GameMode.Update(ms);
                    ms = 0;
                }
            }

            updateNetwork();

            if (bitsPerSecondTimer.ElapsedMilliseconds >= 1000)
            {
                bitsPerSecondList.Add(bitsToAdd);
                float avg = 0;
                foreach (uint i in bitsPerSecondList)
                {
                    avg += i;
                }
                avg /= bitsPerSecondList.Count;

                Console.WriteLine(bitsToAdd + ":" + avg);

                bitsToAdd = 0;
                bitsPerSecondTimer.Restart();
            }
        }

        private void netThreadLoop()
        {
            while (true)
            {
                updateNetwork();
                Thread.Sleep(1);
            }
        }

        private void updateNetwork()
        {
            NetIncomingMessage message = null;

            while ((message = client.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Error:
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        if (client.ConnectionStatus == NetConnectionStatus.Disconnected)
                        {
                            //networkThread.Abort();
                        }
                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        break;
                    case NetIncomingMessageType.Data:
                        {
                            bitsToAdd += (uint) message.LengthBytes;

                            var memory = new MemoryStream(message.ReadBytes(message.LengthBytes));
                            var reader = new BinaryReader(memory);

                            while (memory.Position < memory.Length)
                            {
                                var protocol = (Protocol) reader.ReadByte();

                                switch (protocol)
                                {
                                    case Protocol.GameData:
                                        GameMode.ParseData(memory);
                                        break;
                                    case Protocol.Chat:
                                        break;
                                }
                            }
                            memory.Close();
                            reader.Close();
                        }
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
                client.Recycle(message);
            }
        }
    }
}