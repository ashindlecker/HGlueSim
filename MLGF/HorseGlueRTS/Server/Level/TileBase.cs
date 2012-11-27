using System.IO;
using Shared;

namespace Server.Level
{
    internal class TileBase : STileBase, ISavable
    {
        #region ISavable Members

        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Type);
            writer.Write(Solid);

            return memory.ToArray();
        }

        #endregion
    }
}