using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class STileBase
    {
        public enum TileType:byte
        {
            Grass,
            Water,
            Stone,
        }

        public TileType Type;
        public int GridX, GridY;
        public bool Solid;

        //Changes typically from buildings
        public bool DynamicSolid;

        public STileBase()
        {
            Type = TileType.Grass;
            GridX = 0;
            GridY = 0;
            Solid = false;
            DynamicSolid = false;
        }

    }
}
