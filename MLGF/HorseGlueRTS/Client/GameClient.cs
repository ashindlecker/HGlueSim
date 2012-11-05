using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using Lidgren.Network;
using Client.GameModes;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Client
{
    class GameClient
    {
        private NetClient client;
        public GameModeBase GameMode;
        public InputHandler InputHandler;

        private Stopwatch bitsPerSecondTimer;
        private List<uint> bitsPerSecondList;
        private uint bitsToAdd;

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
            client.Socket.Blocking = false;

            //networkThread = new Thread(new ThreadStart(netThreadLoop));
            //networkThread.Start();
        }

        private void netThreadLoop()
        {
            while(true)
            {
                updateNetwork();
                Thread.Sleep(5);
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
                        if(client.ConnectionStatus == NetConnectionStatus.Disconnected)
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

                            var protocol = (Protocol) reader.ReadByte();

                            switch (protocol)
                            {
                                case Protocol.GameData:
                                    GameMode.ParseData(memory);
                                    break;
                                case Protocol.Chat:
                                    break;
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

        public void Update(float ms)
        {
            while (ms > 0)
            {
                if (ms > Shared.Globals.MAXUPDATETIME)
                {
                    GameMode.Update(Shared.Globals.MAXUPDATETIME);
                    ms -= Shared.Globals.MAXUPDATETIME;
                }
                else
                {
                    GameMode.Update(ms);
                    ms = 0;
                }
            }
            updateNetwork();

            if(bitsPerSecondTimer.ElapsedMilliseconds >= 1000)
            {
                bitsPerSecondList.Add(bitsToAdd);
                float avg = 0;
                foreach(var i in bitsPerSecondList)
                {
                    avg += i;
                }
                avg /= bitsPerSecondList.Count;

                Console.WriteLine(bitsToAdd + ":" + avg);

                bitsToAdd = 0;
                bitsPerSecondTimer.Restart();
            }
        }

        public void SendData(byte[] data)
        {
            NetOutgoingMessage message = client.CreateMessage();
            message.Write(data);
            client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
