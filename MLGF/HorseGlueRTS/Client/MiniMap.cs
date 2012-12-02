using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client
{
    class MiniMap
    {
        public  const uint SIZEX = 180;
        public const uint SIZEY = 180;

        public Level.TileMap TileMap;
        public FogOfWar Fog;

        public Vector2f CameraPosition;
        public Dictionary<ushort, Entities.EntityBase> Entities;
        public byte Team;

        private RenderTexture renderTexture;

        public Sprite MapSprite
        {
            get; private set;
        }

        public MiniMap(Level.TileMap tileMap, FogOfWar fog, Dictionary<ushort, Entities.EntityBase> entities )
        {
            Team = 0;
            Entities = entities;
            TileMap = tileMap;
            Fog = fog;

            CameraPosition = new Vector2f(0, 0);
            renderTexture = new RenderTexture(SIZEX, SIZEY);

            MapSprite = new Sprite(renderTexture.Texture);
        }

        public Vector2f ConvertCoordsToView(Vector2f clickPos)
        {
            float multX = ((float)TileMap.MapSize.X * TileMap.TileSize.X) / renderTexture.Size.X;
            float multY = ((float)TileMap.MapSize.Y * TileMap.TileSize.Y) / renderTexture.Size.Y;

            return new Vector2f(multX*clickPos.X, multY*clickPos.Y);
        }

        public Vector2f ConvertCoordsToGrid(Vector2f pos)
        {
            float squareSizeX = (float)renderTexture.Size.X / TileMap.MapSize.X;
            float squareSizeY = (float)renderTexture.Size.Y / TileMap.MapSize.Y;
            return new Vector2f(TileMap.ConvertCoords(pos).X * squareSizeX, TileMap.ConvertCoords(pos).Y * squareSizeY);
        }

        public void Render()
        {
            if(Fog == null || TileMap == null || Entities == null) return;
            renderTexture.Clear();

            //Draw tiles

            float squareSizeX = (float)renderTexture.Size.X / TileMap.MapSize.X;
            float squareSizeY = (float)renderTexture.Size.Y / TileMap.MapSize.Y;
            var square = new RectangleShape(new Vector2f(squareSizeX, squareSizeY));

            for(int x = 0; x < TileMap.MapSize.X; x++)
            {
                for (int y = 0; y < TileMap.MapSize.Y; y++)
                {
                    square.Position = new Vector2f(x*squareSizeX, y*squareSizeY);
                    switch(Fog.Grid[x,y].CurrentState)
                    {
                        case FOWTile.TileStates.NeverSeen:
                            square.FillColor = new Color(10, 10, 10);
                            break;
                        case FOWTile.TileStates.PreviouslySeen:
                            square.FillColor = new Color(100, 100, 100);
                            break;
                        case FOWTile.TileStates.CurrentlyViewed:
                            square.FillColor = new Color(200, 200, 200);
                            break;
                        default:
                            break;
                    }
                    renderTexture.Draw(square);
                }
            }

            //Draw Entities
            //square.Scale = new Vector2f(1.1f, 1.1f);
            foreach (var entityBase in Entities.Values)
            {
                var gridPos = ConvertCoordsToGrid(entityBase.Position);
                square.Position = gridPos;
                bool allowDraw = false;
                bool secondaryDraw = false;
                var mapGridPos = TileMap.ConvertCoords(entityBase.Position);
                if ((int)mapGridPos.X >= 0 && (int)mapGridPos.X < (int)Fog.Grid.GetLength(0) && (int)mapGridPos.Y >= 0 && (int)mapGridPos.Y < (int)Fog.Grid.GetLength(1))
                {
                    allowDraw = Fog.Grid[(int) mapGridPos.X, (int) mapGridPos.Y].CurrentState ==
                                FOWTile.TileStates.CurrentlyViewed;
                    if(entityBase is Entities.BuildingBase)
                    {
                        if(entityBase.HasBeenViewed && Fog.Grid[(int) mapGridPos.X, (int) mapGridPos.Y].CurrentState !=
                                FOWTile.TileStates.CurrentlyViewed)
                        {
                            allowDraw = true;
                            secondaryDraw = true;
                        }
                    }
                }

                if (entityBase is Entities.Resources)
                {
                    square.FillColor = new Color(100, 100, 200);
                    allowDraw = true;
                }
                else
                {
                    if (entityBase.Team != Team)
                    {
                        if (!secondaryDraw)
                        {
                            square.FillColor = new Color(200, 100, 100);
                        }
                        else
                        {
                            square.FillColor = new Color(100, 50, 50);
                        }
                    }
                    if (entityBase.Team == Team)
                    {
                        square.FillColor = new Color(100, 200, 100);
                    }
                }
                if(allowDraw)
                renderTexture.Draw(square);
            }

            //Draw camera view


            Vector2f viewPos = new Vector2f(CameraPosition.X - Program.window.Size.X / 2, CameraPosition.Y - Program.window.Size.Y / 2);
            viewPos = ConvertCoordsToGrid(viewPos);
            Vector2f viewPos2 = new Vector2f(CameraPosition.X + Program.window.Size.X / 2, CameraPosition.Y + Program.window.Size.Y / 2);
            viewPos2 = ConvertCoordsToGrid(viewPos2);

            Vector2f camPos = viewPos;
            Vector2f size = new Vector2f(viewPos2.X - camPos.X, viewPos2.Y- camPos.Y);

            var cameraRender = new RectangleShape(size);
            cameraRender.Position = camPos;
            cameraRender.OutlineThickness = 2;
            cameraRender.FillColor = new Color(255, 255, 255, 10);
            cameraRender.OutlineColor = new Color(255, 255, 255, 255);

            renderTexture.Draw(cameraRender);


            renderTexture.Display();
        }
    }
}
