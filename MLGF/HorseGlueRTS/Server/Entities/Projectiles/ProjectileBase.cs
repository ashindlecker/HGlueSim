using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SFML.Window;
using System.IO;

namespace Server.Entities.Projectiles
{
    class ProjectileBase:EntityBase
    {
        public Vector2f Start;
        public float Speed { get; protected set; }
        public EntityBase Target;
        public float Damage;
        public Entity.DamageElement Element;

        public ProjectileBase(GameServer _server, Player player, Vector2f startPosition, EntityBase target, float dmg, Entity.DamageElement element, float speed = 1) : base(_server, player)
        {
            EntityType = Entity.EntityType.Projectile;
            Start = startPosition;
            Target = target;
            Position = Start;
            BoundsSize = new Vector2f(5, 5);

            Damage = dmg;
            Element = element;
            Speed = speed;
            RemoveOnNoHealth = false;
        }

        public override void Update(float ms)
        {
            Vector2f anglePos = Target.Position - Position;
            float angle = (float) Math.Atan2(anglePos.Y, anglePos.X);

            Position += new Vector2f((float) Math.Cos(angle)*Speed * ms, (float) Math.Sin(angle)*Speed * ms);

            if(Target.GetBounds().Intersects(GetBounds()))
            {
                OnHit(Target);
                RemoveOnNoHealth = true;
                Health = 0;
            }
        }

        protected virtual void OnHit(EntityBase entity)
        {
            entity.TakeDamage(Damage, Element);
        }

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Damage);
            writer.Write((byte)Element);
            writer.Write(Speed);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Target.WorldId);

            return memory.ToArray();
        }
    }
}
