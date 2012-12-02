using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Client.Effects;
using Client.Entities;
using Client.Level;
using SFML.Audio;
using SFML.Graphics;
using SFML.Window;
using SettlersEngine;
using Shared;

namespace Client.GameModes
{
    internal abstract class GameModeBase
    {
        public const int ENTITYFOWRADIUS = 5;

        protected const float ALERTFADESPEED = .05f;
        protected List<EffectBase> Effects;
        protected List<HUDAlert> alerts;
        protected Sound attackSound_Cliff;

        protected Sound deathSound_Cliff;
        protected Dictionary<ushort, EntityBase> entities;
        protected Sound gatherResourceSound_Cliff;
        protected TileMap map;
        protected SpatialAStar<PathNode, object> pathFinding;
        protected Dictionary<byte, Player> players;
        protected Sound unitCompleteSound_Worker;
        protected Sound useSound_Cliff;
        protected FogOfWar Fog;
        protected byte myId;
        protected bool idSet;

        public Dictionary<ushort, EntityBase> EntityBases
        {
            get { return entities; }
        }

        public GameModeBase()
        {
            myId = 0;
            idSet = false;

            map = new TileMap();

            alerts = new List<HUDAlert>();
            entities = new Dictionary<ushort, EntityBase>();
            players = new Dictionary<byte, Player>();

            Effects = new List<EffectBase>();

            pathFinding = null;

            Fog = null;

            deathSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/Death/0.wav"));
            unitCompleteSound_Worker = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/UnitCompleted/0.wav"));
            useSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/UseCommand/0.wav"));
            gatherResourceSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/GetResources/0.wav"));
            attackSound_Cliff = new Sound(ExternalResources.GSoundBuffer("Resources/Audio/OnAttack/0.wav"));
        }

        public void AddAlert(HUDAlert.AlertTypes type)
        {
            alerts.Add(new HUDAlert {Alpha = 255, Type = type});
        }


        public void AddEffect(EffectBase effect)
        {
            effect.MyGamemode = this;
            Effects.Add(effect);
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

        public virtual void KeyPress(KeyEventArgs keyEvent)
        {
        }

        public virtual void KeyRelease(KeyEventArgs keyEvent)
        {
        }

        public virtual void MouseClick(Mouse.Button button, int x, int y)
        {
        }

        public virtual void MouseMoved(int x, int y)
        {
        }

        public virtual void MouseRelease(Mouse.Button button, int x, int y)
        {
        }

        protected abstract void ParseCustom(MemoryStream memory);

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
                        ushort id = reader.ReadUInt16();
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
                        Fog = new FogOfWar(map.MapSize.X, map.MapSize.Y);
                        for (int x = 0; x < map.MapSize.X; x++)
                        {
                            for (int y = 0; y < map.MapSize.Y; y++)
                            {
                                Fog.Grid[x, y].Blocker = map.Tiles[x, y].Solid;
                            }
                        }
                    }
                    break;
                case Gamemode.Signature.Handshake:
                    ParseHandshake(stream);
                    break;
                case Gamemode.Signature.EntityAdd:
                    {
                        ushort id = reader.ReadUInt16();
                        byte entityType = reader.ReadByte();
                        EntityBase entity = EntityBase.EntityFactory(entityType);
                        entity.Type = (Entity.EntityType) entityType;
                        entity.WorldEntities = entities;
                        entity.WorldId = id;
                        entity.MyGameMode = this;
                        entity.LoadFromBytes(stream);
                        AddEntity(entity, id);
                        entity.SetTeam(reader.ReadByte());
                    }
                    break;
                case Gamemode.Signature.EntityLoad:
                    {
                        ushort count = reader.ReadUInt16();
                        for (int i = 0; i < count; i++)
                        {
                            ushort entId = reader.ReadUInt16();
                            byte entType = reader.ReadByte();
                            EntityBase entAdd = EntityBase.EntityFactory(entType);
                            entAdd.Type = (Entity.EntityType) entType;
                            entAdd.WorldEntities = entities;
                            entAdd.LoadFromBytes(stream);
                            entAdd.WorldId = entId;
                            entAdd.MyGameMode = this;
                            AddEntity(entAdd, entId);
                            entAdd.SetTeam(reader.ReadByte());
                        }
                    }
                    break;
                case Gamemode.Signature.PlayerData:
                    {
                        byte playerId = reader.ReadByte();
                        if (players.ContainsKey(playerId))
                        {
                            players[playerId].Load(stream);
                        }
                    }
                    break;
                case Gamemode.Signature.PlayersLoad:
                    {
                        byte count = reader.ReadByte();
                        for (int i = 0; i < count; i++)
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
                        ushort id = reader.ReadUInt16();
                        if (entities.ContainsKey(id))
                        {
                            entities[id].OnDeath();
                            entities.Remove(id);
                        }
                    }
                    break;
                case Gamemode.Signature.GroupMovement:
                    {
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        bool reset = reader.ReadBoolean();
                        bool attack = reader.ReadBoolean();
                        byte count = reader.ReadByte();

                        for (int i = 0; i < count; i++)
                        {
                            ushort id = reader.ReadUInt16();
                            if (!entities.ContainsKey(id)) continue;
                            if (reset)
                                entities[id].ClearRally();

                            Vector2f startPos = entities[id].Position;
                            if (!reset && entities[id].rallyPoints.Count > 0)
                            {
                                startPos = entities[id].rallyPoints[entities[id].rallyPoints.Count - 1];
                            }

                            PathFindReturn path = PathFindNodes(startPos.X, startPos.Y, x, y);

                            if (path.List == null) continue;

                            foreach (PathNode pathNode in path.List)
                            {
                                if (pathNode == path.List.First.Value) continue;
                                var pos =
                                    new Vector2f(pathNode.X*path.MapSize.X + (path.MapSize.X/2),
                                                 pathNode.Y*path.MapSize.Y + (path.MapSize.Y/2));
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
                case Gamemode.Signature.UpdatePosition:
                    {
                        ushort unitId = reader.ReadUInt16();
                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();

                        if (entities.ContainsKey(unitId))
                        {
                            entities[unitId].Position = new Vector2f(posX, posY);
                        }
                    }
                    break;
            }
        }

        protected abstract void ParseHandshake(MemoryStream memory);

        protected virtual void ParseMap(MemoryStream memory)
        {
            map.LoadFromBytes(memory);
            pathFinding = new SpatialAStar<PathNode, object>(map.GetPathNodeMap());
        }

        public virtual PathFindReturn PathFindNodes(float sx, float sy, float x, float y)
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
            LinkedList<PathNode> path =
                pathFinding.Search(
                    new Point((int) sx,
                              (int) sy),
                    new Point((int) x,
                              (int) y), null);


            return new PathFindReturn
                       {
                           List = path,
                           MapSize = map.TileSize,
                       };
        }

        public virtual void PlayAttackSound(ExternalResources.AttackSounds sounds)
        {
            switch (sounds)
            {
                default:
                case ExternalResources.AttackSounds.CliffGetFucked:
                    PlaySound(attackSound_Cliff);
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

        protected virtual void PlaySound(Sound sound)
        {
            //TODO: Set sound properties based on player's camera 
            sound.Volume = Settings.SOUNDVOLUME;
            if (sound.Status != SoundStatus.Playing)
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

        public void RemoveEffect(EffectBase effect)
        {
            Effects.Remove(effect);
        }

        public abstract void Render(RenderTarget target);
        public abstract void SetCamera(byte id, Vector2f pos);

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

        public abstract void Update(float ms);

        protected void ApplyFog()
        {
            if(Fog == null) return;

            Fog.SetupForFrame();
            if (idSet && players.ContainsKey(myId))
            {
                var myPlayer = players[myId];
                foreach (var entityBase in entities.Values)
                {
                    if (entityBase.Team == myPlayer.Team)
                    {
                        var coords = map.ConvertCoords(entityBase.Position);
                        Fog.ApplyView((uint) coords.X, (uint) coords.Y, ENTITYFOWRADIUS, 5);
                    }
                }
            }
        }

        protected void UpdateAlerts(float ms)
        {
            var readonlyAlerts = new List<HUDAlert>(alerts);

            foreach (HUDAlert hudAlert in readonlyAlerts)
            {
                hudAlert.Alpha -= ALERTFADESPEED*ms;
                if (hudAlert.Alpha <= 0)
                {
                    alerts.Remove(hudAlert);
                }
            }
        }

        #region Nested type: HUDAlert

        public class HUDAlert
        {
            #region AlertTypes enum

            public enum AlertTypes : byte
            {
                CreatedUnit,
                UnitUnderAttack,
                UnitCreated,
                BuildingCompleted
            }

            #endregion

            public float Alpha;
            public AlertTypes Type;
        }

        #endregion

        #region Nested type: PathFindReturn

        public class PathFindReturn
        {
            public LinkedList<PathNode> List;
            public Vector2i MapSize;
        }

        #endregion
    }
}