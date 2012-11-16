using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using SettlersEngine;
using SFML.Graphics;

namespace Shared
{
    public class STileMap
    {
        public Vector2i MapSize;
        public Vector2i TileSize;
        public STileBase[,] Tiles;

        public STileMap()
        {
            MapSize = new Vector2i(0, 0);
            TileSize = new Vector2i(32, 32);
            Tiles = null;
        }


        public void SetMap<TYPE>(int sizeX, int sizeY) where TYPE : STileBase, new()
        {
            MapSize = new Vector2i(sizeX, sizeY);
            Tiles = new STileBase[sizeX,sizeY];

            for (var x = 0; x < Tiles.GetLength(0); x++)
            {
                for (var y = 0; y < Tiles.GetLength(1); y++)
                {
                    Tiles[x, y] = new TYPE();
                    Tiles[x, y].GridX = x;
                    Tiles[x, y].GridY = y;
                }
            }
        }

        public Vector2f ConvertCoords(Vector2f pos)
        {
            return new Vector2f(pos.X/TileSize.X, pos.Y/TileSize.Y);
        }

        public Vector2f ConvertCoords(STileBase tile)
        {
            return new Vector2f(tile.GridX*TileSize.X, tile.GridY*TileSize.Y);
        }

        public List<STileBase> GetTiles(FloatRect rect)
        {
            var point1 = ConvertCoords(new Vector2f(rect.Left, rect.Top));
            var point2 = ConvertCoords(new Vector2f(rect.Left + rect.Width, rect.Top + rect.Height));

            var ret = new List<STileBase>();

            for (var x = (int)point1.X; x < (int)point2.X; x++)
            {
                for (var y = (int) point1.Y; y < (int) point2.Y; y++)
                {
                    if (x >= 0 && x < Tiles.GetLength(0) && y >= 0 && y < Tiles.GetLength(1))
                        ret.Add(Tiles[x, y]);
                }
            }
            return ret;
        }

        public byte[,] GetPathMap()
        {
            var ret = new byte[Tiles.GetLength(0),Tiles.GetLength(1)];


            for (int y = 0; y < Tiles.GetLength(1); y++)
            {
                for (int x = 0; x < Tiles.GetLength(0);x++)
                {
                    Tiles[x, y].GridX = x;
                    Tiles[x, y].GridY = y;
                    ret[x, y] = 0;
                    if(Tiles[x,y].Solid)
                    {
                        ret[x, y] = 1;
                    }
                }
            }

            return ret;
        }

        public SettlersEngine.PathNode[,] GetPathNodeMap()
        {
            var ret = new SettlersEngine.PathNode[Tiles.GetLength(0), Tiles.GetLength(1)];

            for (int y = 0; y < Tiles.GetLength(1); y++)
            {
                for (int x = 0; x < Tiles.GetLength(0); x++)
                {
                    Tiles[x, y].GridX = x;
                    Tiles[x, y].GridY = y;
                    ret[x, y] = new PathNode();
                    ret[x, y].X = x;
                    ret[x, y].Y = y;
                    ret[x, y].IsWall = Tiles[x, y].Solid;
                }
            }

            return ret;
        }
    }
}
