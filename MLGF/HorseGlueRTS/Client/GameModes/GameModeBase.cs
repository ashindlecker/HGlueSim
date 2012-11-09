using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Client.Entities;
using SettlersEngine;
using Shared;
using SFML.Graphics;
using SFML.Window;
using System.Threading;

namespace Client.GameModes
{
    internal abstract class GameModeBase
    {
        public class HUDAlert
        {
            public enum AlertTypes : byte
            {
                CreatedUnit,
                UnitUnderAttack,
                UnitCreated,
                BuildingCompleted
            }

            public AlertTypes Type;
            public float Alpha;
            
        }

        protected Dictionary<ushort, EntityBase> entities;
        protected Dictionary<byte, Player> players;

        protected const float ALERTFADESPEED = .05f;
        protected List<HUDAlert> alerts;
        

        public GameModeBase()
        {
            alerts = new List<HUDAlert>();
            entities = new Dictionary<ushort, EntityBase>();
            players = new Dictionary<byte, Player>();
        }


        public void ParseData(MemoryStream stream)
        {
            var reader = new BinaryReader(stream);
            var signature = (Gamemode.Signature) reader.ReadByte();

            switch (signature)
            {
                case Gamemode.Signature.Custom:
                    ParseCustom(stream);
                    break;
                case Gamemode.Signature.Entity:
                    {
                        var id = reader.ReadUInt16();
                        if (entities.ContainsKey(id))
                            entities[id].ParseData(stream);
                    }
                    break;
                case Gamemode.Signature.MapLoad:
                    ParseMap(stream);
                    break;
                case Gamemode.Signature.Handshake:
                    ParseHandshake(stream);
                    break;
                case Gamemode.Signature.EntityAdd:
                    {
                        var id = reader.ReadUInt16();
                        var entityType = reader.ReadByte();
                        EntityBase entity = EntityBase.EntityFactory(entityType);
                        entity.Type = (Entity.EntityType) entityType;
                        entity.WorldEntities = entities;
                        entity.LoadFromBytes(stream);
                        entity.WorldId = id;
                        AddEntity(entity, id);
                    }
                    break;
                case Gamemode.Signature.EntityLoad:
                    {
                        var count = reader.ReadUInt16();
                        for (var i = 0; i < count; i++)
                        {
                            var entId = reader.ReadUInt16();
                            var entType = reader.ReadByte();
                            var entAdd = EntityBase.EntityFactory(entType);
                            entAdd.Type = (Entity.EntityType)entType;
                            entAdd.WorldEntities = entities;
                            entAdd.LoadFromBytes(stream);
                            entAdd.WorldId = entId;
                            AddEntity(entAdd, entId);
                        }
                    }
                    break;
                case Gamemode.Signature.PlayerData:
                    {
                        var playerId = reader.ReadByte();
                        if (players.ContainsKey(playerId))
                        {
                            players[playerId].Load(stream);
                        }
                    }
                    break;
                case Gamemode.Signature.PlayersLoad:
                    {
                        var count = reader.ReadByte();
                        for (var i = 0; i < count; i++)
                        {
                            var playerAdd = new Player();
                            playerAdd.Load(stream);
                            if (players.ContainsKey(playerAdd.ClientId) == false)
                            {
                                players.Add(playerAdd.ClientId, playerAdd);
                            }
                        }
                    }
                    break;
                case Gamemode.Signature.RemoveEntity:
                    {
                        var id = reader.ReadUInt16();
                        if (entities.ContainsKey(id))
                        {
                            entities[id].OnDeath();
                            entities.Remove(id);
                        }
                    }
                    break;
                case Gamemode.Signature.GroupMovement:
                    {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var reset = reader.ReadBoolean();
                        var attack = reader.ReadBoolean();
                        var count = reader.ReadByte();

                        for (int i = 0; i < count; i++)
                        {
                            var id = reader.ReadUInt16();
                            if (entities.ContainsKey(id))
                            {
                                if (reset)
                                    entities[id].ClearRally();

                                var startPos = entities[id].Position;
                                if (!reset && entities[id].rallyPoints.Count > 0)
                                {
                                    startPos = entities[id].rallyPoints[entities[id].rallyPoints.Count - 1];
                                }

                                var path = PathFindNodes(startPos.X, startPos.Y, x, y);
                                if (path.List != null)
                                {
                                    foreach (var pathNode in path.List)
                                    {
                                        if (pathNode != path.List.First.Value)
                                        {
                                            var pos =
                                                new Vector2f(pathNode.X * path.MapSize.X + (path.MapSize.X / 2),
                                                             pathNode.Y * path.MapSize.Y + (path.MapSize.Y / 2));
                                            entities[id].Move(pos.X, pos.Y);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void AddEntity(EntityBase entity, ushort id)
        {
            if (entities.ContainsKey(id) == false)
            {
                entity.MyGameMode = this;
                entity.WorldId = id;
                entities.Add(id, entity);
            }
        }

        public void AddAlert(HUDAlert.AlertTypes type)
        {
            alerts.Add(new HUDAlert() { Alpha = 255, Type = type });
        }

        protected abstract void ParseCustom(MemoryStream memory);
        protected abstract void ParseMap(MemoryStream memory);
        protected abstract void ParseHandshake(MemoryStream memory);

        public abstract void Update(float ms);
        public abstract void Render(RenderTarget target);


        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }

        public abstract PathFindReturn PathFindNodes(float sx, float sy, float x, float y);


        protected void UpdateAlerts(float ms)
        {
            var readonlyAlerts = new List<HUDAlert>(alerts);

            foreach (var hudAlert in readonlyAlerts)
            {
                hudAlert.Alpha -= ALERTFADESPEED*ms;
                if(hudAlert.Alpha <= 0)
                {
                    alerts.Remove(hudAlert);
                }
            }
        }

        public virtual void MouseMoved(int x, int y)
        {
        }

        public virtual void MouseClick(Mouse.Button button, int x, int y)
        {
            
        }

        public virtual void MouseRelease(Mouse.Button button, int x, int y)
        {

        }

        public virtual void KeyPress(KeyEventArgs keyEvent)
        {
            
        }


        public virtual void KeyRelease(KeyEventArgs keyEvent)
        {

        }
    }
}
