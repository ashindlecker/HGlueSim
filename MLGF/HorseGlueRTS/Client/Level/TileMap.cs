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
        private List<Sprite> tiles = new List<Sprite>();

        public override void ApplyLevel(TiledMap level)
        {
            base.ApplyLevel(level);
            foreach (var tileSets in MyMap.TileSets)
            {
                var sprites = TileSheet.GrabSprites(ExternalResources.GTexture(tileSets.ImageSource), tileSets.TileSize, new Vector2i(0, 0));
                tiles.AddRange(sprites);
            }
        }

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
            /*
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
                            sprite = spriteSheet[160];
                            break;
                        case STileBase.TileType.Water:
                            sprite = spriteSheet[179];
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
             * */

            if(MyMap == null) return;
            foreach (var layers in MyMap.TileLayers)
            {
                for(var y = 0; y < layers.GIds.GetLength(1); y++)
                {
                    for(var x = 0; x < layers.GIds.GetLength(0); x++)
                    {
                        if (layers.GIds[x,y] == 0 || layers.GIds[x, y] - 1 >= tiles.Count) continue;
                        var sprite = tiles[(int) layers.GIds[x, y] - 1];
                        sprite.Position = new Vector2f(x*TileSize.X, y*TileSize.Y);
                        target.Draw(sprite);
                    }
                }
            }
        }

    }
}
