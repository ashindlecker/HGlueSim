﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shared;
using SFML.Graphics;
using SFML.Window;


namespace Client.Entities
{
    abstract class EntityBase :ILoadable
    {
        protected List<Vector2f> rallyPoints;
        public Vector2f Position;
        public float Health;
        public float MaxHealth;

        public ushort Energy;
        public ushort MaxEnergy;
        public byte EnergyRegenRate;    //in milliseconds
        

        public EntityBase EntityToUse;  //Workers use minerals, gas geisers, etc

        public Dictionary<ushort, EntityBase> WorldEntities;

        public ushort WorldId;
        public Entity.EntityType Type;

        protected Vector2f BoundsSize;


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
        }

        public abstract void Update(float ts);
        public abstract void Render(RenderTarget target);

        public virtual void Use(EntityBase user)
        {
            //minerals may give the user minerals to hold, etc
        }

        public virtual FloatRect GetBounds()
        {
            return new FloatRect(Position.X - (BoundsSize.X/2), Position.Y - (BoundsSize.Y/2), BoundsSize.X,
                                 BoundsSize.Y);
        }

        public virtual void OnDeath()
        {
            
        }

        public void ParseData(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);
            var signature = (Entity.Signature) reader.ReadByte();

            switch(signature)
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
            }

        }

        private void ParseUse(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var userid = reader.ReadUInt16();

            if(WorldEntities.ContainsKey(userid))
            {
                Use(WorldEntities[userid]);
            }
        }

        private void ParseEntityToUseChange(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var isNotNull = reader.ReadBoolean();


            if (isNotNull)
            {
                var id = reader.ReadUInt16();
                if (WorldEntities.ContainsKey(id))
                {
                    EntityToUse = WorldEntities[id];
                }
            }
            else
            {
                EntityToUse = null;
            }
        }

        private void ParseDamage(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var damage = reader.ReadSingle();
            var element = (Entity.DamageElement)reader.ReadByte();
            Health = reader.ReadSingle();

            OnTakeDamage(damage, element);
        }

        public virtual void OnTakeDamage(float damage, Entity.DamageElement element)
        {
            Health -= damage;
        }

        private void ParseMove(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            Position.X = reader.ReadSingle();
            Position.Y = reader.ReadSingle();
            var count = reader.ReadByte();
            rallyPoints.Clear();
            for (var i = 0; i < count; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
            }

            onMove();
        }

        public virtual void onMove()
        {
            //perhaps a sound is played or HUD change
        }

        protected abstract void ParseCustom(MemoryStream memoryStream);
        protected abstract void ParseUpdate(MemoryStream memoryStream);


        public void LoadFromBytes(MemoryStream data)
        {
            ParseUpdate(data);
        }

        public static EntityBase EntityFactory(byte type)
        {
            switch ((Entity.EntityType)type)
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
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            return null;
        }
    }
}