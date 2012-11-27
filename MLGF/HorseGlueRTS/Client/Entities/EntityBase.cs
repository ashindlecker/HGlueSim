using System;
using System.Collections.Generic;
using System.IO;
using Client.GameModes;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    internal abstract class EntityBase : ILoadable
    {
        protected Vector2f BoundsSize;
        public ushort Energy;
        public byte EnergyRegenRate; //in milliseconds

        public EntityBase EntityToUse; //Workers use minerals, gas geisers, etc
        public float Health;
        public ushort MaxEnergy;
        public float MaxHealth;

        public GameModeBase MyGameMode;
        public Vector2f Position;

        public Entity.EntityType Type;
        public Dictionary<ushort, EntityBase> WorldEntities;
        public ushort WorldId;
        public List<Vector2f> rallyPoints;


        protected EntityBase()
        {
            Type = Entity.EntityType.Unit;
            BoundsSize = new Vector2f(20, 20);
            WorldId = 0;
            EntityToUse = null;
            WorldEntities = null;
            rallyPoints = new List<Vector2f>();
            Health = 0;
            MaxHealth = 0;
            Position = new Vector2f();
            Energy = 0;
            MaxHealth = 0;
            EnergyRegenRate = 0;
            MyGameMode = null;
        }

        #region ILoadable Members

        public void LoadFromBytes(MemoryStream data)
        {
            ParseUpdate(data);
        }

        #endregion

        public void ClearRally()
        {
            rallyPoints.Clear();
        }

        public static EntityBase EntityFactory(byte type)
        {
            Console.WriteLine(type);
            switch ((Entity.EntityType) type)
            {
                case Entity.EntityType.Unit:
                    return new UnitBase();
                    break;
                case Entity.EntityType.Building:
                    return new BuildingBase();
                    break;
                case Entity.EntityType.Worker:
                    return new Worker();
                    break;
                case Entity.EntityType.Resources:
                    return new Resources();
                    break;
                case Entity.EntityType.HomeBuilding:
                    return new HomeBuilding();
                    break;
                case Entity.EntityType.SupplyBuilding:
                    return new SupplyBuilding();
                    break;
                case Entity.EntityType.GlueFactory:
                    return new GlueFactory();
                    break;
                    case Entity.EntityType.Projectile:
                    return new Projectile();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type " + type);
            }
            return null;
        }


        public virtual FloatRect GetBounds()
        {
            return new FloatRect(Position.X - (BoundsSize.X/2), Position.Y - (BoundsSize.Y/2), BoundsSize.X,
                                 BoundsSize.Y);
        }

        public virtual void Move(float x, float y)
        {
            rallyPoints.Add(new Vector2f(x, y));
        }

        public virtual void OnDeath()
        {
            MyGameMode.PlayDeathSound(ExternalResources.DeathSounds.CliffDeath);
        }

        public virtual void OnMove()
        {
            //perhaps a sound is played or HUD change
        }

        protected virtual void OnSpellCast(MemoryStream memory, byte type)
        {
        }

        public virtual void OnTakeDamage(float damage, Entity.DamageElement element)
        {
            Health -= damage;
        }

        public virtual void OnUseChange(EntityBase entity)
        {
            //Play a sound or something
        }

        protected abstract void ParseCustom(MemoryStream memoryStream);

        private void ParseDamage(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            float damage = reader.ReadSingle();
            var element = (Entity.DamageElement) reader.ReadByte();
            Health = reader.ReadSingle();

            OnTakeDamage(damage, element);
        }

        public void ParseData(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);
            var signature = (Entity.Signature) reader.ReadByte();

            switch (signature)
            {
                case Entity.Signature.Damage:
                    ParseDamage(memory);
                    break;
                case Entity.Signature.Move:
                    ParseMove(memory);
                    break;
                case Entity.Signature.Custom:
                    ParseCustom(memory);
                    break;
                case Entity.Signature.Update:
                    ParseUpdate(memory);
                    break;
                case Entity.Signature.Use:
                    ParseUse(memory);
                    break;
                case Entity.Signature.EntityToUseChange:
                    ParseEntityToUseChange(memory);
                    break;
                case Entity.Signature.Spell:
                    ParseSpellCast(memory);
                    break;
            }
        }

        private void ParseEntityToUseChange(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            bool isNotNull = reader.ReadBoolean();


            if (isNotNull)
            {
                ushort id = reader.ReadUInt16();
                if (WorldEntities.ContainsKey(id))
                {
                    EntityToUse = WorldEntities[id];
                    OnUseChange(EntityToUse);
                }
            }
            else
            {
                EntityToUse = null;
            }
        }


        private void ParseMove(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            Position.X = reader.ReadUInt16();
            Position.Y = reader.ReadUInt16();
            byte count = reader.ReadByte();
            rallyPoints.Clear();
            for (int i = 0; i < count; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadUInt16(), reader.ReadUInt16()));
            }

            OnMove();
        }

        protected virtual void ParseSpellCast(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            byte spell = reader.ReadByte();

            OnSpellCast(memoryStream, spell);
        }

        protected abstract void ParseUpdate(MemoryStream memoryStream);

        private void ParseUse(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            ushort userid = reader.ReadUInt16();

            if (WorldEntities.ContainsKey(userid))
            {
                Use(WorldEntities[userid]);
            }
        }

        public abstract void Render(RenderTarget target);

        public virtual void SetTeam(byte team)
        {
        }

        public abstract void Update(float ts);

        public virtual void Use(EntityBase user)
        {
            //minerals may give the user minerals to hold, etc
        }
    }
}