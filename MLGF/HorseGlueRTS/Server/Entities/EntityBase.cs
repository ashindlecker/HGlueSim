using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Entities.Buildings;
using Shared;
using System.IO;
using SFML.Window;
using SFML.Graphics;

namespace Server.Entities
{
    abstract class EntityBase : ISavable
    {
        public GameServer Server;

        public GameModes.GameModeBase MyGameMode;
 
        public ushort WorldId { get; set; }

        public Entity.EntityType EntityType { get; protected set; }

        public byte Team { get; set; }

        public float Health;
        public float MaxHealth;

        public Vector2f Position;
        protected List<Entity.RallyPoint> rallyPoints;

        public EntityBase EntityToUse;  //Workers use minerals, gas geisers, etc
        public Vector2f BoundsSize;

        public ushort Energy;
        public ushort MaxEnergy;
        public byte EnergyRegenRate;    //in milliseconds

        public bool RemoveOnNoHealth
        {
            get; protected set;
        }

        //Neutral units shouldn't be attacked by anyone by default (things like minerals)
        public bool Neutral { protected set; get; }


        protected delegate byte[] SpellResponseDelegate(float x, float y);
        protected class SpellData
        {
            public SpellData(float engy, SpellResponseDelegate dDelegate)
            {
                EnergyCost = engy;
                Function = dDelegate;
            }
            public SpellResponseDelegate Function;
            public float EnergyCost;
        }
        protected Dictionary<byte, SpellData> spells;  
        

        protected EntityBase(GameServer _server)
        {
            Neutral = false;
            BoundsSize = new Vector2f(10, 10);
            RemoveOnNoHealth = true;

            EntityToUse = null;
            MyGameMode = null;
            Server = _server;
            rallyPoints = new List<Entity.RallyPoint>();
            Health = 0;
            MaxHealth = 0;
            Energy = 0;
            EnergyRegenRate = 0;
            MaxEnergy = 0;
            Position = new Vector2f();
            spells = new Dictionary<byte, SpellData>();
        }

        public abstract void Update(float ms);

        public virtual FloatRect GetBounds()
        {
            return new FloatRect(Position.X - (BoundsSize.X / 2), Position.Y - (BoundsSize.Y / 2), BoundsSize.X,
                                 BoundsSize.Y);
        }

        public void SetEntityToUse(EntityBase toUse)
        {
            var data = SetEntityToUseResponse(toUse);
            SendData(data, Entity.Signature.EntityToUseChange);
        }

        public virtual void OnPlayerCustomMove()
        {
            
        }

        protected virtual byte[] SetEntityToUseResponse(EntityBase toUse)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
            writer.Write((bool)(toUse != null));
            EntityToUse = toUse;

            if (toUse != null)
            {
                writer.Write(EntityToUse.WorldId);
            }
            return memory.ToArray();
        }

        public virtual void OnDeath()
        {
            
        }

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

        public void TakeDamage(float damage, Entity.DamageElement element, bool send = true)
        {
            var data = TakeDamageResponse(damage, element);
            if (send)
                SendData(data, Entity.Signature.Damage);
        }

        protected virtual byte[] TakeDamageResponse(float damage, Entity.DamageElement element)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(damage);
            writer.Write((byte)element);
            writer.Write(Health);

            Health -= damage;
            return memory.ToArray();
        }

        public void Move(float x, float y, Entity.RallyPoint.RallyTypes type = Entity.RallyPoint.RallyTypes.StandardMove, bool reset = false,  bool send = true, byte buildData = 0)
        {
            if(reset)
                rallyPoints.Clear();
            rallyPoints.Add(new Entity.RallyPoint()
                                {
                                    X = x,
                                    Y = y,
                                    RallyType = type,
                                    BuildType = buildData,
                                });


            var data = MoveResponse(x, y, reset);
            if (send)
                SendData(data, Entity.Signature.Move);
        }

        protected virtual byte[] MoveResponse(float x, float y, bool reset)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Position.X);
            writer.Write(Position.Y);

            writer.Write((byte)rallyPoints.Count);

            for (int i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write(rallyPoints[i].X);
                writer.Write(rallyPoints[i].Y);
            }

            return memory.ToArray();
        }

        public abstract byte[] UpdateData();


        public bool CastSpell(byte spell, float x, float y)
        {
            if (spells.ContainsKey(spell) == false) return false;
            if (Energy < spells[spell].EnergyCost) return false;

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
           
            writer.Write(spell);
            writer.Write(spells[spell].Function(x, y));

            SendData(memory.ToArray(), Entity.Signature.Spell);
            return true;
        }


        protected void SendData(byte[] data, Entity.Signature signature)
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)Gamemode.Signature.Entity);
            writer.Write(WorldId);
            writer.Write((byte) signature);
            writer.Write(data);

            Server.SendGameData(memory.ToArray());

            memory.Close();
            writer.Close();
        }

        public byte[] ToBytes()
        {
            return UpdateData();
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
    }
}
