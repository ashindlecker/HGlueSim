using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using System.IO;
using Shared;

namespace Server.Level
{
    class TileMap : Shared.STileMap, ISavable
    {
        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(MapSize.X);
            writer.Write(MapSize.Y);
            
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    writer.Write(((TileBase) Tiles[x, y]).ToBytes());
                }
            }

            return memory.ToArray();
        }
    }
}
