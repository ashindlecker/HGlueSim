using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using System.IO;

namespace Client.Level
{
    class TileBase : STileBase, ILoadable
    {
        public void LoadFromBytes(MemoryStream data)
        {
            var reader = new BinaryReader(data);

            Type = (TileType) reader.ReadByte();
            Solid = reader.ReadBoolean();
        }
    }
}
