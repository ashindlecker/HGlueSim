using System;
using System.IO;
using System.Linq;
using Shared;

namespace Client
{
    internal class InputHandler
    {
        private readonly GameClient client;

        public InputHandler(GameClient _client)
        {
            client = _client;
        }

        public void SendBuildUnit(ushort[] entityIds, byte toProduce)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.CreateUnit);

            writer.Write(toProduce);

            writer.Write((byte) entityIds.Length);

            for (int i = 0; i < (byte) entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }

        public void SendEntityUseChange(ushort[] entityIds, ushort entityToUseId, bool resetRally)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.ChangeUseEntity);
            writer.Write(entityToUseId);
            writer.Write(resetRally);
            writer.Write((byte) entityIds.Length);

            for (int i = 0; i < (byte) entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }

        //Reset determins whether it's a "shift move" or a move that replaces all other moves
        public void SendMoveInput(float x, float y, ushort[] entityIds, bool reset = false, bool attackMove = true)
        {
            Console.WriteLine("SENT " + DateTime.Now.Ticks);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.Movement);

            writer.Write(x);
            writer.Write(y);
            writer.Write(reset);
            writer.Write(attackMove);
            writer.Write((byte) entityIds.Length);

            for (int i = 0; i < (byte) entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }


        public void SendSpellInput(float x, float y, string spell, ushort[] entityIds)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.SpellCast);

            writer.Write(spell);
            writer.Write(x);
            writer.Write(y);
            writer.Write((byte) entityIds.Length);

            for (int i = 0; i < (byte) entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }
    }
}