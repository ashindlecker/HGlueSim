using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using SettlersEngine;

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

            for (int x = 0; x < Tiles.GetLength(0); x++)
            {
                for (int y = 0; y < Tiles.GetLength(0); y++)
                {
                    Tiles[x, y] = new TYPE();
                }
            }
        }

        public byte[,] GetPathMap()
        {
            var ret = new byte[Tiles.GetLength(0),Tiles.GetLength(1)];


            for (int y = 0; y < Tiles.GetLength(1); y++)
            {
                for (int x = 0; x < Tiles.GetLength(0);x++)
                {
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
