using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Client.Effects;
using Client.Entities;
using Client.Level;
using SettlersEngine;
using Shared;
using SFML.Graphics;
using SFML.Window;
using System.Threading;
using SFML.Audio;

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

        protected Level.TileMap map;
        protected SpatialAStar<PathNode, object> pathFinding; 

        protected Dictionary<ushort, EntityBase> entities;
        protected Dictionary<byte, Player> players;

        protected List<Effects.EffectBase> Effects; 

        protected const float ALERTFADESPEED = .05f;
        protected List<HUDAlert> alerts;

        protected Sound deathSound_Cliff;
        protected Sound unitCompleteSound_Worker;
        protected Sound useSound_Cliff;
        protected Sound gatherResourceSound_Cliff;
        protected Sound attackSound_Cliff;
        
        public GameModeBase()
        {
            map = new TileMap();

            alerts = new List<HUDAlert>();
            entities = new Dictionary<ushort, EntityBase>();
            players = new Dictionary<byte, Player>();

            Effects = new List<EffectBase>();

            pathFinding = null;

            deathSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/Death/0.wav"));
            unitCompleteSound_Worker = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/UnitCompleted/0.wav"));
            useSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/UseCommand/0.wav"));
            gatherResourceSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/GetResources/0.wav"));
            attackSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/OnAttack/0.wav"));
        }


        public void AddEffect(Effects.EffectBase effect)
        {
            effect.MyGamemode = this;
            Effects.Add(effect);
        }

        public void RemoveEffect(Effects.EffectBase effect)
        {
            Effects.Remove(effect);
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
                    case Gamemode.Signature.TiledMapLoad:
                    {
                        var tiledMap = new TiledMap();
                        tiledMap.Load(stream);
                        map.ApplyLevel(tiledMap);
                        pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());
                    }
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
                        entity.WorldId = id;
                        entity.LoadFromBytes(stream);
                        AddEntity(entity, id);
                        entity.SetTeam(reader.ReadByte());
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
                            entAdd.SetTeam(reader.ReadByte());
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

                        for (var i = 0; i < count; i++)
                        {
                            var id = reader.ReadUInt16();
                            if (!entities.ContainsKey(id)) continue;
                            if (reset)
                                entities[id].ClearRally();

                            var startPos = entities[id].Position;
                            if (!reset && entities[id].rallyPoints.Count > 0)
                            {
                                startPos = entities[id].rallyPoints[entities[id].rallyPoints.Count - 1];
                            }

                            var path = PathFindNodes(startPos.X, startPos.Y, x, y);

                            if (path.List == null) continue;

                            foreach (var pathNode in path.List)
                            {
                                if (pathNode == path.List.First.Value) continue;
                                var pos =
                                    new Vector2f(pathNode.X * path.MapSize.X + (path.MapSize.X / 2),
                                                 pathNode.Y * path.MapSize.Y + (path.MapSize.Y / 2));
                                entities[id].Move(pos.X, pos.Y);
                            }
                        }
                    }
                    break;
                    case Gamemode.Signature.SetCamera:
                    {
                        SetCamera(reader.ReadByte(), new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
                    }
                    break;
            }
        }

        public abstract void SetCamera(byte id, Vector2f pos);

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
        protected virtual void ParseMap(MemoryStream memory)
        {
            map.LoadFromBytes(memory);
            pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());
        }
        protected abstract void ParseHandshake(MemoryStream memory);

        public abstract void Update(float ms);
        public abstract void Render(RenderTarget target);

        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }

        public virtual PathFindReturn PathFindNodes(float sx, float sy, float x, float y)
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
            var path =
                pathFinding.Search(
                    new System.Drawing.Point((int)sx,
                              (int)sy),
                    new System.Drawing.Point((int)x,
                              (int)y), null);


            return new PathFindReturn()
            {
                List = path,
                MapSize = map.TileSize,
            };
        }

        protected virtual void PlaySound(Sound sound)
        {
            //TODO: Set sound properties based on player's camera 
            sound.Volume = Settings.SOUNDVOLUME;
            if(sound.Status != SoundStatus.Playing)
                sound.Play();
        }

        public virtual void PlayUnitFinishedSound(Entity.EntityType type)
        {
            switch (type)
            {
                default:
                case Entity.EntityType.Worker:
                    PlaySound(unitCompleteSound_Worker);
                    break;
            }
        }

        public virtual void PlayDeathSound(ExternalResources.DeathSounds sounds)
        {
            switch (sounds)
            {
                default:
                case ExternalResources.DeathSounds.CliffDeath:
                    PlaySound(deathSound_Cliff);
                    break;
            }
        }

        public virtual void PlayUseSound(ExternalResources.UseSounds sounds)
        {
            switch (sounds)
            {
                default:
                case ExternalResources.UseSounds.CliffUsing:
                    PlaySound(useSound_Cliff);
                    break;
            }
        }

        public virtual void PlayGatherResourcesSound(ExternalResources.ResourceSounds sounds)
        {
            switch (sounds)
            {
                default:
                case ExternalResources.ResourceSounds.CliffMining:
                    PlaySound(gatherResourceSound_Cliff);
                    break;
            }
        }

        public virtual void PlayAttackSound(ExternalResources.AttackSounds sounds)
        {
            switch(sounds)
            {
                default:
                    case ExternalResources.AttackSounds.CliffGetFucked:
                    PlaySound(attackSound_Cliff);
                    break;
            }
        }

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
                float sine = (float)Math.Sin(spaceAngle);

                foreach (var checkEntity in readOnlyList.Values)
                {
                    if (!spacedUnits.Contains(checkEntity.WorldId) && checkEntity != entityBase && checkEntity is UnitBase)
                    {
                        var checkRect = new FloatRect(checkEntity.Position.X - (Shared.Globals.SPACE_BOUNDS / 2),
                                                      checkEntity.Position.Y - (Shared.Globals.SPACE_BOUNDS / 2), Shared.Globals.SPACE_BOUNDS,
                                                      Shared.Globals.SPACE_BOUNDS);
                        if (entRect.Intersects(checkRect))
                        {
                            entityBase.Position += new Vector2f((Shared.Globals.SPACING_SPEED * cosine) * ms, (Shared.Globals.SPACING_SPEED * sine) * ms);
                            spacedUnits.Add(checkEntity.WorldId);
                        }
                    }
                }
                spaceAngle += Shared.Globals.SPACE_ANGLE_INCREASE;
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
