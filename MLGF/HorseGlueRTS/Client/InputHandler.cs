using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shared;
namespace Client
{
    class InputHandler
    {
        private GameClient client;

        public InputHandler(GameClient _client)
        {
            client = _client;
        }

        //Reset determins whether it's a "shift move" or a move that replaces all other moves
        public void SendMoveInput(float x, float y, ushort[] entityIds, bool reset = false, bool attackMove = true)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.Movement);

            writer.Write(x);
            writer.Write(y);
            writer.Write(reset);
            writer.Write(attackMove);
            writer.Write((byte)entityIds.Length);

            for(var i = 0; i < (byte)entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }


        public void SendSpellInput(float x, float y, byte spell, ushort[] entityIds)
        {

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)Protocol.Input);
            writer.Write((byte)InputSignature.SpellCast);

            writer.Write(spell);
            writer.Write(x);
            writer.Write(y);
            writer.Write((byte)entityIds.Length);

            for (var i = 0; i < (byte)entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }

        public void SendEntityUseChange(ushort[] entityIds, ushort entityToUseId)
        {

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)Protocol.Input);
            writer.Write((byte) InputSignature.ChangeUseEntity);
            writer.Write(entityToUseId);
            writer.Write((byte)entityIds.Length);

            for (var i = 0; i < (byte)entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }

        public void SendBuildUnit(ushort[] entityIds, byte toProduce)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Protocol.Input);
            writer.Write((byte) InputSignature.CreateUnit);

            writer.Write(toProduce);

            writer.Write((byte)entityIds.Length);

            for (var i = 0; i < (byte)entityIds.Count(); i++)
            {
                writer.Write(entityIds[i]);
            }

            client.SendData(memory.ToArray());

            writer.Close();
            memory.Close();
        }
    }
}
