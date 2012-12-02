using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class FOWTile
    {
        public bool Blocker;
        public enum TileStates
        {
            NeverSeen,
            PreviouslySeen,
            CurrentlyViewed,
        }
        public TileStates CurrentState;

        public FOWTile()
        {
            Blocker = false;
            CurrentState = TileStates.NeverSeen;
        }
    }

    public class FogOfWar
    {
        public FOWTile[,] Grid;

        public FogOfWar(int sizeX, int sizeY)
        {
            Grid = new FOWTile[sizeX,sizeY];

            for(int x = 0; x < Grid.GetLength(0); x++)
            {
                for (int y = 0; y < Grid.GetLength(1); y++)
                {
                    Grid[x, y] = new FOWTile();
                }
            }
        }

        public void SetupForFrame()
        {
            foreach (var fowTile in Grid)
            {
                if(fowTile.CurrentState == FOWTile.TileStates.CurrentlyViewed)
                {
                    fowTile.CurrentState = FOWTile.TileStates.PreviouslySeen;
                }
            }
        }

        public void ApplyView(uint x, uint y, uint radius, float accuracy = 1f)
        {
            for (float angle = 0; angle <= 360; angle += accuracy)
            {
                float cos = (float) Math.Cos(angle*(3.14f/180f));
                float sin = (float) Math.Sin(angle*(3.14f/180f));

                for (int r = 0; r <= radius; r++)
                {
                    var placeX = x + (cos*r);
                    var placeY = y + (sin*r);

                    if (placeX >= 0 && placeX < Grid.GetLength(0) && placeY >= 0 && placeY < Grid.GetLength(1))
                    {
                        Grid[(int) placeX, (int) placeY].CurrentState = FOWTile.TileStates.CurrentlyViewed;
                        if (Grid[(int) placeX, (int) placeY].Blocker)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
