using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;
using Shared;
using System.Diagnostics;

namespace Client.Entities
{
    class UnitBase : EntityBase
    {

        public enum UnitState : byte
        {
            Agro,
            Standard,
        }

        public float Speed;
        public UnitState State;
        public float Range;
        public ushort AttackDelay;

        private Stopwatch attackTimer;

        protected EntityBase EntityToAttack { get; private set; }

        private bool allowMovement;

        public UnitBase()
        {
            EntityToAttack = null;
            allowMovement = false;

            Speed = 0;
            State = UnitState.Agro;
            Range = 1000;
            AttackDelay = 2000;
            attackTimer = new Stopwatch();
            attackTimer.Restart();
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var signature = (UnitSignature) reader.ReadByte();

            switch (signature)
            {
                case UnitSignature.RallyCompleted:
                    {
                        var posX = reader.ReadSingle();
                        var posY = reader.ReadSingle();

                        Position = new Vector2f(posX, posY);
                        rallyPoints.Clear();
                    }
                    break;
                case UnitSignature.Attack:
                    {
                        rallyPoints.Clear();
                        ushort entityWorldId = reader.ReadUInt16();
                        if(WorldEntities.ContainsKey(entityWorldId))
                            onAttack(WorldEntities[entityWorldId]);
                    }
                    break;
                case UnitSignature.PopFirstRally:
                    {
                        var posX = reader.ReadSingle();
                        var posY = reader.ReadSingle();

                        Position = new Vector2f(posX, posY);
                        if (rallyPoints.Count > 0)
                            rallyPoints.RemoveAt(0);
                    }
                    break;
                case UnitSignature.ClearRally:
                    rallyPoints.Clear();
                    break;
                case UnitSignature.ChangeMovementAllow:
                    allowMovement = reader.ReadBoolean();
                    break;
                default:
                    break;
            }
        }

        protected virtual void onAttack(EntityBase ent)
        {
            ent.OnTakeDamage(10, Entity.DamageElement.Normal);
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            var hasEntToUse = reader.ReadBoolean();
            if(hasEntToUse)
            {
                var id = reader.ReadUInt16();
                if(WorldEntities.ContainsKey(id))
                {
                    EntityToUse = WorldEntities[id];
                }
            }

            Health = reader.ReadSingle();
            State = (UnitState) reader.ReadByte();
            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());
            Speed = reader.ReadSingle();
            Energy = reader.ReadUInt16();
            Range = reader.ReadSingle();
            allowMovement = reader.ReadBoolean();

            var rallyCount = reader.ReadByte();
            rallyPoints.Clear();
            for(var i = 0; i < rallyCount; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
            }
        }

        public override void Update(float ms)
        {
            if (!allowMovement || rallyPoints.Count == 0) return;

            Vector2f destination = rallyPoints[0];

            if ((int) Position.X < (int) destination.X)
            {
                Position.X += Speed*ms;
                if ((int) Position.X >= (int) destination.X) Position.X = (int) destination.X;
            }
            if ((int) Position.Y < (int) destination.Y)
            {
                Position.Y += Speed*ms;
                if ((int) Position.Y >= (int) destination.Y) Position.Y = (int) destination.Y;
            }
            if ((int) Position.X > destination.X)
            {
                Position.X -= Speed*ms;
                if ((int) Position.X <= (int) destination.X) Position.X = (int) destination.X;
            }
            if ((int) Position.Y > (int) destination.Y)
            {
                Position.Y -= Speed*ms;
                if ((int) Position.Y <= (int) destination.Y) Position.Y = (int) destination.Y;
            }

            if ((int) Position.X == (int) destination.X && (int) Position.Y == (int) destination.Y)
            {
                rallyPoints.RemoveAt(0);
            }
        }

        public override void Render(RenderTarget target)
        {
            //debug drawing
            Sprite sprite = new Sprite(ExternalResources.GTexture("Resources/Sprites/TestTile.png"));
            sprite.Origin = new Vector2f(sprite.TextureRect.Width/2, sprite.TextureRect.Height/2);
            sprite.Position = Position;
            sprite.Color = new Color(255, 200, 200);
            target.Draw(sprite);

            debugDrawRange(target);
        }

        protected void debugDrawRange(RenderTarget target)
        {
            CircleShape circle = new CircleShape(Range);
            circle.Origin = new Vector2f(circle.Radius, circle.Radius);
            circle.Position = Position;
            circle.FillColor = new Color(255, 0, 0, 100);

            target.Draw(circle);
        }
    }
}
