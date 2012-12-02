using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Lidgren.Network;
using SFML.Graphics;
using SFML.Window;
using Server.Entities;
using Server.Level;
using SettlersEngine;
using Shared;

namespace Server.GameModes
{
    internal abstract class GameModeBase
    {
        private readonly Stopwatch entityPositionUpdateTimer;
        public GameServer Server;
        public TiledMap TiledMap; //Map Created from tiled level editor
        protected Dictionary<ushort, EntityBase> entities;

        private ushort entityToUpdate;
        private ushort entityWorldIdToGive;
        public TileMap map;
        public SpatialAStar<PathNode, object> pathFinding;
        protected List<Player> players;


        protected GameModeBase(GameServer server)
        {
            map = new TileMap();
            TiledMap = new TiledMap();

            players = new List<Player>();
            entityWorldIdToGive = 0;
            Server = server;
            entities = new Dictionary<ushort, EntityBase>();

            entityToUpdate = 0;
            entityPositionUpdateTimer = new Stopwatch();
            entityPositionUpdateTimer.Restart();
        }

        public Dictionary<ushort, EntityBase> WorldEntities
        {
            get { return entities; }
        }

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

        public abstract byte[] HandShake();
        public abstract void OnStatusChange(NetConnection connection, NetConnectionStatus status);

        public abstract void ParseInput(MemoryStream memory, NetConnection client);

        public PathFindReturn PathFindNodes(float sx, float sy, float x, float y, bool noclipLast = false)
        {
            sx /= map.TileSize.X;
            x /= map.TileSize.X;
            sy /= map.TileSize.Y;
            y /= map.TileSize.Y;

            if (sx < 0 || sy < 0 || x < 0 || y < 0 || sx >= map.Tiles.GetLength(0) || x >= map.Tiles.GetLength(0) ||
                sy >= map.Tiles.GetLength(1) || y >= map.Tiles.GetLength(1))
            {
                return new PathFindReturn
                           {
                               List = null,
                               MapSize = map.TileSize,
                           };
            }
            bool resetBack = false;
            if (noclipLast)
            {
                if (pathFinding.SearchSpace[(int) x, (int) y].IsWall)
                {
                    pathFinding.SearchSpace[(int) x, (int) y].IsWall = false;
                    resetBack = true;
                }
            }
            LinkedList<PathNode> path =
                pathFinding.Search(
                    new Point((int) sx,
                              (int) sy),
                    new Point((int) x,
                              (int) y), null);

            if (resetBack)
            {
                pathFinding.SearchSpace[(int) x, (int) y].IsWall = true;
            }

            return new PathFindReturn
                       {
                           List = path,
                           MapSize = map.TileSize,
                       };
        }

        public void Remove(EntityBase entity)
        {
            entities.Remove(entity.WorldId);

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(entity.WorldId);
            SendData(memory.ToArray(), Gamemode.Signature.RemoveEntity);
        }

        public void SendAllEntities()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((ushort) entities.Count);
            foreach (EntityBase entity in entities.Values)
            {
                writer.Write(entity.WorldId);
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

            for (int i = 0; i < players.Count; i++)
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

        protected void SendEntityPosition(EntityBase entity)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(entity.WorldId);
            writer.Write(entity.Position.X);
            writer.Write(entity.Position.Y);

            SendData(memory.ToArray(), Gamemode.Signature.UpdatePosition);
        }

        protected void SendMap()
        {
            SendData(TiledMap.ToBytes(), Gamemode.Signature.TiledMapLoad);
            //SendData(map.ToBytes(), Gamemode.Signature.MapLoad);
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

        protected void SetMap(string file)
        {
            TiledMap.Load(file);
            map.ApplyLevel(TiledMap);
            pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());

            foreach (Vector2f apples in TiledMap.AppleResources)
            {
                var resourceAdd = new Resources(Server, null);
                resourceAdd.ResourceType = ResourceTypes.Apple;
                resourceAdd.Position = apples;
                AddEntity(resourceAdd);
            }

            foreach (Vector2f wood in TiledMap.WoodResources)
            {
                var resourceAdd = new Resources(Server, null);
                resourceAdd.ResourceType = ResourceTypes.Tree;
                resourceAdd.Position = wood;
                AddEntity(resourceAdd);
            }
        }

        protected void SpaceUnits(float ms)
        {
            var readOnlyList = new Dictionary<ushort, EntityBase>(entities);

            var spacedUnits = new List<ushort>();


            float spaceAngle = 0;

            foreach (EntityBase entityBase in readOnlyList.Values)
            {
                //TODO: Add Unit Type Enum so we don't have to use an "is" check
                //Workers have to be able to go through units because it disrupts mining
                if (spacedUnits.Contains(entityBase.WorldId) || entityBase.rallyPoints.Count > 0 ||
                    !(entityBase is UnitBase)) continue;

                var entRect = new FloatRect(entityBase.Position.X - (Globals.SPACE_BOUNDS/2),
                                            entityBase.Position.Y - (Globals.SPACE_BOUNDS/2), Globals.SPACE_BOUNDS,
                                            Globals.SPACE_BOUNDS);

                var cosine = (float) Math.Cos(spaceAngle);
                var sine = (float) Math.Sin(spaceAngle);

                foreach (EntityBase checkEntity in readOnlyList.Values)
                {
                    if (!spacedUnits.Contains(checkEntity.WorldId) && checkEntity != entityBase &&
                        checkEntity is UnitBase)
                    {
                        var checkRect = new FloatRect(checkEntity.Position.X - (Globals.SPACE_BOUNDS/2),
                                                      checkEntity.Position.Y - (Globals.SPACE_BOUNDS/2),
                                                      Globals.SPACE_BOUNDS,
                                                      Globals.SPACE_BOUNDS);
                        if (entRect.Intersects(checkRect))
                        {
                            entityBase.Position += new Vector2f((Globals.SPACING_SPEED*cosine)*ms,
                                                                (Globals.SPACING_SPEED*sine)*ms);
                            spacedUnits.Add(checkEntity.WorldId);
                        }
                    }
                }
                spaceAngle += Globals.SPACE_ANGLE_INCREASE;
            }
        }

        public virtual void Update(float ms)
        {
            var readOnlyList = new Dictionary<ushort, EntityBase>(entities);
            
            foreach (EntityBase entityBase in readOnlyList.Values)
            {
                entityBase.UseCount = 0;
            }

            foreach (EntityBase entityBase in readOnlyList.Values)
            {
                if (entityBase.EntityToUse != null)
                {
                    entityBase.EntityToUse.UseCount++;
                }
            }

            foreach (EntityBase entityBase in readOnlyList.Values)
            {
                entityBase.Update(ms);
                if (entityBase.Health >= entityBase.MaxHealth)
                    entityBase.Health = entityBase.MaxHealth;
                if (entityBase.Energy >= entityBase.MaxEnergy)
                    entityBase.Energy = entityBase.MaxEnergy;

                if (entityBase.RemoveOnNoHealth && entityBase.Health <= 0)
                {
                    entityBase.OnDeath();
                    Remove(entityBase);
                }
            }
            SpaceUnits(ms);

            if (false && entityPositionUpdateTimer.ElapsedMilliseconds >= 500)
            {
                if (entities.ContainsKey(entityToUpdate))
                    SendEntityPosition(entities[entityToUpdate]);
                entityToUpdate++;
                if (entityToUpdate >= entities.Count)
                    entityToUpdate = 0;
            }
        }

        public abstract void UpdatePlayer(Player player);

        #region Nested type: PathFindReturn

        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }

        #endregion
    }
}