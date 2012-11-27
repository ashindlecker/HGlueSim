using System.IO;
using Shared;

namespace Client.Level
{
    internal class TileBase : STileBase, ILoadable
    {
        #region ILoadable Members

        public void LoadFromBytes(MemoryStream data)
        {
            var reader = new BinaryReader(data);

            Type = (TileType) reader.ReadByte();
            Solid = reader.ReadBoolean();
        }

        #endregion
    }
}