using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SFML.Window;
using System.IO;

namespace Shared
{
    public class TiledMap
    {
        public Vector2i TileSize;
        public Vector2i MapSize;

        public class TileSet
        {
            public uint FirstId;
            public Vector2i TileSize;
            public string ImageSource;

            public TileSet(uint iu, Vector2i tilesize, string image)
            {
                FirstId = iu;
                TileSize = tilesize;
                ImageSource = image;
            }
        }

        public List<TileSet> TileSets;

        public class TileLayer
        {
            public TileLayer()
            {
                GIds = null;
                SolidLayer = false;
            }
            public uint[,] GIds;
            public bool SolidLayer;
        }

        public List<TileLayer> TileLayers;

        public List<Vector2f> WoodResources;
        public List<Vector2f> AppleResources;
        public List<Vector2f> SpawnPoints; 
        
        public TiledMap()
        {
            WoodResources = new List<Vector2f>();
            AppleResources = new List<Vector2f>();
            SpawnPoints = new List<Vector2f>();

            TileSets = new List<TileSet>();
            TileLayers = new List<TileLayer>();
            MapSize = new Vector2i(0, 0);
        }

        public void Load(string file)
        {
            var reader = new XmlTextReader(file);

            var tileSet = new TileSet(0, new Vector2i(0,0), "na");

            var tileLayer = new TileLayer();
            uint currentTileIdX = 0;
            uint currentTileIdY = 0;

            string objectGroup = "";

            while(reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.None:
                        break;
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "map":
                                MapSize = new Vector2i(Convert.ToInt32(reader.GetAttribute("width")),
                                                       Convert.ToInt32(reader.GetAttribute("height")));

                                break;
                            case "tileset":
                                tileSet.FirstId = Convert.ToUInt32(reader.GetAttribute("firstgid"));
                                tileSet.TileSize = new Vector2i(Convert.ToInt32(reader.GetAttribute("tilewidth")),
                                                                Convert.ToInt32(reader.GetAttribute("tileheight")));
                                break;
                            case "image":
                                tileSet.ImageSource = reader.GetAttribute("source");
                                break;
                            case "layer": 
                                tileLayer = new TileLayer();
                                tileLayer.GIds =
                                    new uint[Convert.ToUInt32(reader.GetAttribute("width")),
                                        Convert.ToUInt32(reader.GetAttribute("height"))];

                                tileLayer.SolidLayer = (reader.GetAttribute("name") == "Solids");

                                currentTileIdX = 0;
                                currentTileIdY = 0;

                                break;
                            case "tile":
                                if(tileLayer.GIds != null)
                                {
                                    tileLayer.GIds[currentTileIdX, currentTileIdY] =
                                        Convert.ToUInt32(reader.GetAttribute("gid"));

                                    currentTileIdX++;
                                    if(currentTileIdX == tileLayer.GIds.GetLength(0))
                                    {
                                        currentTileIdX = 0;
                                        currentTileIdY++;
                                    }
                                }
                                break;
                            case "objectgroup":
                                objectGroup = reader.GetAttribute("name");
                                break;
                            case "object":
                                {
                                    var pos = new Vector2f(Convert.ToSingle(reader.GetAttribute("x")), Convert.ToSingle(reader.GetAttribute("y")));
                                    switch (objectGroup.ToLower())
                                    {
                                        case "wood":
                                            WoodResources.Add(pos);
                                            break;
                                        case "apples":
                                            AppleResources.Add(pos);
                                            break;
                                        case "spawn":
                                        case "spawns":
                                            SpawnPoints.Add(pos);
                                            break;
                                    }
                                }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            case "tileset":
                                TileSets.Add(tileSet);
                                break;
                            case "layer":
                                if(tileLayer.GIds != null)
                                    TileLayers.Add(tileLayer);
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            reader.Close();
        }

        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(TileSize.X);
            writer.Write(TileSize.Y);
            writer.Write(MapSize.X);
            writer.Write(MapSize.Y);

            writer.Write(TileLayers.Count);

            for(var i = 0; i < TileLayers.Count; i++)
            {
                writer.Write(TileLayers[i].SolidLayer);

                writer.Write(TileLayers[i].GIds.GetLength(0));
                writer.Write(TileLayers[i].GIds.GetLength(1));

                for (var y = 0; y < TileLayers[i].GIds.GetLength(1); y++)
                {
                    for (var x = 0; x < TileLayers[i].GIds.GetLength(0); x++)
                    {
                        writer.Write(TileLayers[i].GIds[x, y]);
                    }
                }
            }


            writer.Write(TileSets.Count);
            
            for(var i = 0; i < TileSets.Count;i++)
            {
                writer.Write(TileSets[i].FirstId);
                writer.Write(TileSets[i].TileSize.X);
                writer.Write(TileSets[i].TileSize.Y);
                writer.Write(TileSets[i].ImageSource);
            }

            return memory.ToArray();
        }

        public void Load(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            TileSize.X = reader.ReadInt32();
            TileSize.Y = reader.ReadInt32();
            MapSize.X = reader.ReadInt32();
            MapSize.Y = reader.ReadInt32();

            var layerCount = reader.ReadInt32();

            for (var i = 0; i < layerCount; i++)
            {
                var newLayer = new TileLayer();
                newLayer.SolidLayer = reader.ReadBoolean();
                newLayer.GIds = new uint[reader.ReadInt32(),reader.ReadInt32()];

                for (var y = 0; y < newLayer.GIds.GetLength(1); y++)
                {
                    for (var x = 0; x < newLayer.GIds.GetLength(0); x++)
                    {
                        newLayer.GIds[x, y] = reader.ReadUInt32();
                    }
                }
                TileLayers.Add(newLayer);
            }

            var tileSetCount = reader.ReadInt32();

            for (var i = 0; i < tileSetCount; i++)
            {
                var newTileSet = new TileSet(reader.ReadUInt32(), new Vector2i(reader.ReadInt32(), reader.ReadInt32()), reader.ReadString());
                TileSets.Add(newTileSet);
            }
        }
    }
}
