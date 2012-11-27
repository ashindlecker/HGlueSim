using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    class Projectile:EntityBase
    {
        public float Damage;
        public Entity.DamageElement Element;
        public EntityBase Target;
        public float Speed;

        
        public Projectile()
        {
            Damage = 0;
            Element = Entity.DamageElement.Normal;
            Target = null;
            Speed = 0;
            
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            Damage = reader.ReadSingle();
            Element = (Entity.DamageElement) reader.ReadByte();
            Speed = reader.ReadSingle();
            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());
            var targetId = reader.ReadUInt16();

            if(MyGameMode.EntityBases.ContainsKey(targetId))
            {
                Target = MyGameMode.EntityBases[targetId];
            }
        }

        public override void Render(RenderTarget target)
        {
        }

        public override void Update(float ms)
        {
            if(Target == null) return;

            Vector2f anglePos = Target.Position - Position;
            float angle = (float)Math.Atan2(anglePos.Y, anglePos.X);

            Position += new Vector2f((float)Math.Cos(angle) * Speed * ms, (float)Math.Sin(angle) * Speed * ms);

            if (Target.GetBounds().Intersects(GetBounds()))
            {
                OnHit(Target);
            }
        }

        public virtual void OnHit(EntityBase entity)
        {
            //play sound or something
        }
    }
}
