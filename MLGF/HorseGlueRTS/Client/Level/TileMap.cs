using System;
using System.Collections.Generic;
using System.IO;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Level
{
    internal class TileMap : STileMap, ILoadable
    {
        private readonly List<Sprite> tiles = new List<Sprite>();

        #region ILoadable Members

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

        #endregion

        public override void ApplyLevel(TiledMap level)
        {
            base.ApplyLevel(level);
            foreach (TiledMap.TileSet tileSets in MyMap.TileSets)
            {
                List<Sprite> sprites = TileSheet.GrabSprites(ExternalResources.GTexture(tileSets.ImageSource),
                                                             tileSets.TileSize, new Vector2i(0, 0));
                tiles.AddRange(sprites);
            }
        }

        public void Render(RenderTarget target, FloatRect screenBounds, FogOfWar fog = null)
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

            if (MyMap == null) return;

            int startX = (int)(screenBounds.Left/TileSize.X);
            int startY = (int)(screenBounds.Top/TileSize.Y);
            int endX = (int)((screenBounds.Left + screenBounds.Width) / TileSize.X);
            int endY = (int)((screenBounds.Top + screenBounds.Height) / TileSize.Y);
            endX++;
            endY++;

            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            endX = Math.Min(endX, Tiles.GetLength(0));
            endY = Math.Min(endY, Tiles.GetLength(1));

            foreach (TiledMap.TileLayer layers in MyMap.TileLayers)
            {
                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        if (layers.GIds[x, y] == 0 || layers.GIds[x, y] - 1 >= tiles.Count) continue;

                        Sprite sprite = tiles[(int) layers.GIds[x, y] - 1];
                        sprite.Position = new Vector2f(x*TileSize.X, y*TileSize.Y);
                        if(fog != null)
                        {
                            switch (fog.Grid[x,y].CurrentState)
                            {
                                case FOWTile.TileStates.NeverSeen:
                                    sprite.Color = new Color(50, 50, 50);
                                    break;
                                case FOWTile.TileStates.PreviouslySeen:
                                    sprite.Color = new Color(150, 150, 150);
                                    break;
                                case FOWTile.TileStates.CurrentlyViewed:
                                    sprite.Color = new Color(255, 255, 255);
                                    break;
                                default:
                                    break;
                            }
                        }
                        target.Draw(sprite);
                    }
                }
            }
        }
    }
}