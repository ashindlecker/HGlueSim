using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using Shared;
using SFML.Graphics;

namespace Client.Level
{
    class TileMap : STileMap, ILoadable
    {
        public void LoadFromBytes(MemoryStream data)
        {
            var reader = new BinaryReader(data);

            MapSize.X = reader.ReadInt32();
            MapSize.Y = reader.ReadInt32();

            Tiles = new STileBase[MapSize.X,MapSize.Y];
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    Tiles[x, y] = new TileBase();
                    ((TileBase) Tiles[x, y]).LoadFromBytes(data);
                }
            }
        }

        public void Render(RenderTarget target)
        {
            var spriteSheet =
                TileSheet.GrabSprites(ExternalResources.GTexture("Resources/Sprites/Map/terrain_atlas.png"),
                                      new Vector2i(32, 32), new Vector2i(0, 0)); 
            Sprite sprite = new Sprite();
            sprite.Texture = ExternalResources.GTexture("Resources/Sprites/TestTile.png");

            for(int x = 0;  x < MapSize.X; x++)
            {
                for(int y = 0; y < MapSize.Y; y++)
                {
                    STileBase tile = Tiles[x, y];
                    switch(tile.Type)
                    {
                        case STileBase.TileType.Grass:
                            sprite = spriteSheet[173];
                            break;
                        case STileBase.TileType.Water:
                            sprite = spriteSheet[178];
                            break;
                        case STileBase.TileType.Stone:
                            sprite = spriteSheet[50];
                            break;
                        default:
                            break;
                    }
                    sprite.Position = new Vector2f(x*TileSize.X, y*TileSize.Y);
                    target.Draw(sprite);
                }
            }
        }
    }
}
