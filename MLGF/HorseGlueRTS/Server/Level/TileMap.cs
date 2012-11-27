using System.IO;
using Shared;

namespace Server.Level
{
    internal class TileMap : STileMap, ISavable
    {
        #region ISavable Members

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

        #endregion

        protected override STileBase GetTileFromGID(TiledMap.TileLayer layer, uint id)
        {
            return new TileBase();
        }
    }
}