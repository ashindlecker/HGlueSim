using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Level;
using SettlersEngine;
using Shared;
using System.IO;
using Lidgren.Network;
using Server.Entities;
using SFML.Window;
using SFML.Graphics;

namespace Server.GameModes
{
    internal abstract class GameModeBase
    {
        public GameServer Server;
        protected Dictionary<ushort, EntityBase> entities;
        protected List<Player> players;
        public SettlersEngine.SpatialAStar<PathNode, object> pathFinding;


        public TileMap map;
        public TiledMap TiledMap; //Map Created from tiled level editor


        public Dictionary<ushort, EntityBase> WorldEntities
        {
            get { return entities; }
        }

        private ushort entityWorldIdToGive;

        protected GameModeBase(GameServer server)
        {
            map = new TileMap();
            TiledMap = new TiledMap();

            players = new List<Player>();
            entityWorldIdToGive = 0;
            Server = server;
            entities = new Dictionary<ushort, EntityBase>();
        }

        public virtual void Update(float ms)
        {
            var readOnlyList = new Dictionary<ushort, EntityBase>(entities);

            foreach (var entityBase in readOnlyList.Values)
            {
                entityBase.Update(ms);
                if (entityBase.Health >= entityBase.MaxHealth)
                    entityBase.Health = entityBase.MaxHealth;
                if (entityBase.Energy >= entityBase.MaxEnergy)
                    entityBase.Energy = entityBase.MaxEnergy;

                if(entityBase.RemoveOnNoHealth && entityBase.Health <= 0)
                {
                    entityBase.OnDeath();
                    Remove(entityBase);
                }
            }
            SpaceUnits(ms);
        }

        protected void SpaceUnits(float ms)
        {
            var readOnlyList = new Dictionary<ushort, EntityBase>(entities);

            var spacedUnits = new List<ushort>();

            
            float spaceAngle = 0;

            foreach (var entityBase in readOnlyList.Values)
            {
                //TODO: Add Unit Type Enum so we don't have to use an "is" check
                //Workers have to be able to go through units because it disrupts mining
                if (spacedUnits.Contains(entityBase.WorldId) || entityBase.rallyPoints.Count > 0 || !(entityBase is UnitBase)) continue;

                var entRect = new FloatRect(entityBase.Position.X - (Shared.Globals.SPACE_BOUNDS / 2),
                                            entityBase.Position.Y - (Shared.Globals.SPACE_BOUNDS / 2), Shared.Globals.SPACE_BOUNDS, Shared.Globals.SPACE_BOUNDS);

                float cosine = (float)Math.Cos(spaceAngle);
                float sine = (float) Math.Sin(spaceAngle);

                foreach (var checkEntity in readOnlyList.Values)
                {
                    if (!spacedUnits.Contains(checkEntity.WorldId) && checkEntity != entityBase && checkEntity is UnitBase)
                    {
                        var checkRect = new FloatRect(checkEntity.Position.X - (Shared.Globals.SPACE_BOUNDS / 2),
                                                      checkEntity.Position.Y - (Shared.Globals.SPACE_BOUNDS / 2), Shared.Globals.SPACE_BOUNDS,
                                                      Shared.Globals.SPACE_BOUNDS);
                        if(entRect.Intersects(checkRect))
                        {
                            entityBase.Position += new Vector2f((Shared.Globals.SPACING_SPEED * cosine) * ms, (Shared.Globals.SPACING_SPEED * sine) * ms);
                            spacedUnits.Add(checkEntity.WorldId);
                        }
                    }
                }
                spaceAngle += Shared.Globals.SPACE_ANGLE_INCREASE;
            }
        }

        public void Remove(EntityBase entity)
        {
            entities.Remove(entity.WorldId);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(entity.WorldId);
            SendData(memory.ToArray(), Gamemode.Signature.RemoveEntity);
        }

        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }

        public PathFindReturn PathFindNodes(float sx, float sy, float x, float y, bool noclipLast = false)
        {
            sx /= map.TileSize.X;
            x /= map.TileSize.X;
            sy /= map.TileSize.Y;
            y /= map.TileSize.Y;

            if (sx < 0 || sy < 0 || x < 0 || y < 0 || sx >= map.Tiles.GetLength(0) || x >= map.Tiles.GetLength(0) || sy >= map.Tiles.GetLength(1) || y >= map.Tiles.GetLength(1))
            {
                return new PathFindReturn()
                {
                    List = null,
                    MapSize = map.TileSize,
                };
            }
            bool resetBack = false;
            if(noclipLast)
            {
                if(pathFinding.SearchSpace[(int)x,(int)y].IsWall)
                {
                    pathFinding.SearchSpace[(int) x, (int) y].IsWall = false;
                    resetBack = true;
                }
            }
            var path =
                pathFinding.Search(
                    new Point((int)sx,
                              (int)sy),
                    new Point((int)x,
                              (int)y), null);

            if(resetBack)
            {
                pathFinding.SearchSpace[(int) x, (int) y].IsWall = true;
            }

            return new PathFindReturn()
            {
                List = path,
                MapSize = map.TileSize,
            };
        }

        public abstract void OnStatusChange(NetConnection connection, NetConnectionStatus status);
        
        public abstract void ParseInput(MemoryStream memory, NetConnection client);
       
        public abstract byte[] HandShake();

        public abstract void UpdatePlayer(Player player);

        public virtual void AddEntity(EntityBase entity, ushort id)
        {
            if (entities.ContainsKey(id)) return;

            entity.MyGameMode = this;
            entity.WorldId = id;
            entities.Add(id, entity);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(id);
            writer.Write((byte) entity.EntityType);
            writer.Write(entity.UpdateData());
            writer.Write(entity.Team);
            SendData(memory.ToArray(), Gamemode.Signature.EntityAdd);
        }

        public void AddEntity(EntityBase ent)
        {
            AddEntity(ent, entityWorldIdToGive);
            entityWorldIdToGive++;
        }

        public void SetCamera(Player player, Vector2f pos)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
            writer.Write(player.ClientId);
            writer.Write(pos.X);
            writer.Write(pos.Y);
            SendData(memory.ToArray(), Gamemode.Signature.SetCamera);
        }

        protected void SendMap()
        {
            SendData(TiledMap.ToBytes(), Gamemode.Signature.TiledMapLoad);
            //SendData(map.ToBytes(), Gamemode.Signature.MapLoad);
        }

        protected void SetMap(string file)
        {
            TiledMap.Load(file);
            map.ApplyLevel(TiledMap);
            pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());

            foreach (var apples in TiledMap.AppleResources)
            {
                var resourceAdd = new Resources(Server, null);
                resourceAdd.ResourceType = ResourceTypes.Apple;
                resourceAdd.Position = apples;
                AddEntity(resourceAdd);
            }

            foreach (var wood in TiledMap.WoodResources)
            {
                var resourceAdd = new Resources(Server, null);
                resourceAdd.ResourceType = ResourceTypes.Tree;
                resourceAdd.Position = wood;
                AddEntity(resourceAdd);
            }
        }

        public void SendAllEntities()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((ushort) entities.Count);
            foreach (var entity in entities.Values)
            {
                writer.Write((ushort) entity.WorldId);
                writer.Write((byte) entity.EntityType);
                writer.Write(entity.ToBytes());
                writer.Write(entity.Team);
            }

            SendData(memory.ToArray(), Gamemode.Signature.EntityLoad);

            writer.Close();
            memory.Close();
        }
        
        public void SendAllPlayers()
        {

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) players.Count);

            for(int i = 0; i < players.Count; i++)
            {
                writer.Write(players[i].ToBytes());
            }

            SendData(memory.ToArray(), Gamemode.Signature.PlayersLoad);

            writer.Close();
            memory.Close();
        }

        protected void SendData(byte[] data, Gamemode.Signature signature, bool direct = false)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) signature);
            writer.Write(data);

            Server.SendGameData(memory.ToArray(), direct);

            memory.Close();
            writer.Close();
        }
    }
}
