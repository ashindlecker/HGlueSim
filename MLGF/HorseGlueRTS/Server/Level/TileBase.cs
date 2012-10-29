using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shared;
namespace Server.Level
{
    class TileBase : Shared.STileBase, ISavable
    {

        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Type);
            writer.Write(Solid);

            return memory.ToArray();
        }
    }
}
