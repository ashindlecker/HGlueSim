using System.Collections.Generic;
using System.IO;
using SFML.Graphics;
using SFML.Window;
using Server.Entities.Buildings;
using Server.GameModes;
using SettlersEngine;
using Shared;

namespace Server.Entities
{
    internal abstract class EntityBase : ISavable
    {
        public Vector2f BoundsSize;

        public ushort Energy;
        public byte EnergyRegenRate; //in milliseconds
        public EntityBase EntityToUse; //Workers use minerals, gas geisers, etc
        public float Health;
        public ushort MaxEnergy;
        public float MaxHealth;
        public GameModeBase MyGameMode;
        public Player MyPlayer;
        public Vector2f Position;
        public GameServer Server;
        public List<Entity.RallyPoint> rallyPoints;
        public Dictionary<byte, SpellData> spells;
        public ushort UseCount;


        protected EntityBase(GameServer _server, Player player)
        {
            UseCount = 0;
            Server = _server;
            MyPlayer = player;

            Neutral = false;
            BoundsSize = new Vector2f(10, 10);
            RemoveOnNoHealth = true;

            EntityToUse = null;
            MyGameMode = null;
            rallyPoints = new List<Entity.RallyPoint>();
            Health = 0;
            MaxHealth = 0;
            Energy = 0;
            EnergyRegenRate = 0;
            MaxEnergy = 0;

            Position = new Vector2f();

            spells = new Dictionary<byte, SpellData>();
        }

        public ushort WorldId { get; set; }
        public Entity.EntityType EntityType { get; protected set; }
        public byte Team { get; set; }

        public bool RemoveOnNoHealth { get; protected set; }

        //Neutral units shouldn't be attacked by anyone by default (things like minerals)
        public bool Neutral { protected set; get; }

        #region ISavable Members

        public byte[] ToBytes()
        {
            return UpdateData();
        }

        #endregion

        public bool CastSpell(byte spell, float x, float y)
        {
            if (spells.ContainsKey(spell) == false) return false;
            if (Energy < spells[spell].EnergyCost) return false;

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(spell);
            if(!spells[spell].IsBuildSpell)
                writer.Write(spells[spell].Function(x, y));
            else
            {
                var worker = this as Worker;
                if (worker != null)
                {
                    worker.AddBuildingToBuild(spells[spell].BuildType, x, y);
                }
                else
                {
                    var building = this as BuildingBase;
                    if(building != null)
                    {
                        building.StartProduce(spells[spell].BuildType);
                    }
                }
            }

            SendData(memory.ToArray(), Entity.Signature.Spell);
            return true;
        }

        public static EntityBase EntityFactory(Entity.EntityType type, GameServer server, Player ply)
        {
            switch (type)
            {
                case Entity.EntityType.Unit:
                    break;
                case Entity.EntityType.Building:
                    break;
                case Entity.EntityType.Worker:
                    break;
                case Entity.EntityType.Resources:
                    break;
                case Entity.EntityType.HomeBuilding:
                    return new HomeBuilding(server, ply);
                    break;
                case Entity.EntityType.SupplyBuilding:
                    return new SupplyBuilding(server, ply, 12);
                default:
                    break;
            }

            return null;
        }

        public virtual FloatRect GetBounds()
        {
            return new FloatRect(Position.X - (BoundsSize.X/2), Position.Y - (BoundsSize.Y/2), BoundsSize.X,
                                 BoundsSize.Y);
        }

        public void Move(float x, float y, Entity.RallyPoint.RallyTypes type = Entity.RallyPoint.RallyTypes.StandardMove,
                         bool reset = false, bool send = true, byte buildData = 0, bool noclipLast = false)
        {
            if (reset)
                rallyPoints.Clear();

            Vector2f searchStartPos = Position;
            if (rallyPoints.Count > 0)
            {
                searchStartPos = new Vector2f(rallyPoints[rallyPoints.Count - 1].X, rallyPoints[rallyPoints.Count - 1].Y);
            }

            GameModeBase.PathFindReturn nodes = MyGameMode.PathFindNodes(searchStartPos.X, searchStartPos.Y, x, y,
                                                                         noclipLast);
            if (nodes.List != null)
            {
                foreach (PathNode node in nodes.List)
                {
                    if (node != nodes.List.First.Value)
                    {
                        Entity.RallyPoint.RallyTypes rallyType = type;
                        if (rallyType == Entity.RallyPoint.RallyTypes.Build && node != nodes.List.Last.Value)
                            rallyType = Entity.RallyPoint.RallyTypes.StandardMove;

                        rallyPoints.Add(new Entity.RallyPoint
                                            {
                                                X = node.X*nodes.MapSize.X + (nodes.MapSize.X/2),
                                                Y = node.Y*nodes.MapSize.Y + (nodes.MapSize.Y/2),
                                                RallyType = rallyType,
                                                BuildType = buildData,
                                            });
                    }
                }
            }
            byte[] data = MoveResponse(x, y, reset);
            if (send)
                SendData(data, Entity.Signature.Move);
        }

        protected virtual byte[] MoveResponse(float x, float y, bool reset)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((ushort) Position.X);
            writer.Write((ushort) Position.Y);

            writer.Write((byte) rallyPoints.Count);

            for (int i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write((ushort) rallyPoints[i].X);
                writer.Write((ushort) rallyPoints[i].Y);
            }

            return memory.ToArray();
        }

        public virtual void OnDeath()
        {
            //Called when Entity has 0 or less HP
        }

        public virtual void OnPlayerCustomMove()
        {
            //Called when player sends a move input
        }

        protected void SendData(byte[] data, Entity.Signature signature)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) Gamemode.Signature.Entity);
            writer.Write(WorldId);
            writer.Write((byte) signature);
            writer.Write(data);

            Server.SendGameData(memory.ToArray());

            memory.Close();
            writer.Close();
        }

        public void SetEntityToUse(EntityBase toUse)
        {
            byte[] data = SetEntityToUseResponse(toUse);
            SendData(data, Entity.Signature.EntityToUseChange);
        }

        protected virtual byte[] SetEntityToUseResponse(EntityBase toUse)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
            writer.Write((toUse != null));
            EntityToUse = toUse;

            if (toUse != null)
            {
                writer.Write(EntityToUse.WorldId);
            }
            return memory.ToArray();
        }


        public void TakeDamage(float damage, Entity.DamageElement element, bool send = true)
        {
            byte[] data = TakeDamageResponse(damage, element);
            if (send)
                SendData(data, Entity.Signature.Damage);
        }

        protected virtual byte[] TakeDamageResponse(float damage, Entity.DamageElement element)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(damage);
            writer.Write((byte) element);
            writer.Write(Health);

            Health -= damage;
            return memory.ToArray();
        }

        public abstract void Update(float ms);


        public abstract byte[] UpdateData();

        public void Use(EntityBase user)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(user.WorldId);
            writer.Write(UseResponse(user));

            SendData(memory.ToArray(), Entity.Signature.Use);
        }

        protected virtual byte[] UseResponse(EntityBase user)
        {
            return new byte[0];
        }

        #region Nested type: SpellData

        public class SpellData
        {
            public bool IsBuildSpell;
            public byte BuildType;

            public ushort AppleCost;
            public float EnergyCost;
            public SpellResponseDelegate Function;
            public ushort GlueCost;
            public ushort WoodCost;
            public ushort CastTime;
            public byte SupplyCost;

            public SpellData(float engy, SpellResponseDelegate dDelegate)
            {
                IsBuildSpell = false;
                BuildType = 0;
                EnergyCost = engy;
                Function = dDelegate;
                WoodCost = 0;
                AppleCost = 0;
                GlueCost = 0;
                CastTime = 0;
                SupplyCost = 0;
            }
        }

        #endregion

        #region Nested type: SpellResponseDelegate

        public delegate byte[] SpellResponseDelegate(float x, float y);

        #endregion
    }
}