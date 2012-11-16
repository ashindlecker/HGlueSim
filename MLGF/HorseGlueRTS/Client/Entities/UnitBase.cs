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

        public enum AnimationTypes: byte
        {
            Idle,
            Moving,
            EndAttacking,
            StartAttacking,
            SpellCast,
            IdleWithResources,
            MovingWithResources,
        }

        public float StandardAttackDamage { get; protected set; }
        public Entity.DamageElement StandardAttackElement { get; protected set; }

        public float Speed;
        public UnitState State;
        public float Range;
        public ushort AttackDelay;

        private Stopwatch attackTimer;

        protected EntityBase EntityToAttack { get; private set; }

        private bool allowMovement;


        protected Dictionary<AnimationTypes, AnimatedSprite> Sprites;

        private AnimationTypes _currentAnimation;

        protected AnimationTypes CurrentAnimation
        {
            get { return _currentAnimation; }
            set
            {
                if (CurrentAnimation == AnimationTypes.StartAttacking || CurrentAnimation == AnimationTypes.EndAttacking)
                {
                    if(Sprites[CurrentAnimation].AnimationCompleted)
                        _currentAnimation = value;
                }
                else
                {
                    _currentAnimation = value;
                }
            }
        }

        private bool _moveXCompleted, _moveYCompleted;

        private Vector2f drawPosition;


        public UnitBase()
        {
            drawPosition = new Vector2f();
            _moveXCompleted = false;
            _moveYCompleted = false;

            EntityToAttack = null;
            allowMovement = false;

            Speed = 0;
            State = UnitState.Agro;
            Range = 1000;
            AttackDelay = 2000;
            attackTimer = new Stopwatch();
            attackTimer.Restart();

            StandardAttackDamage = 1;
            StandardAttackElement = Entity.DamageElement.Normal;

            CurrentAnimation = AnimationTypes.Idle;
            Sprites = new Dictionary<AnimationTypes, AnimatedSprite>();

            const byte AnimationTypeCount = 7;
            for (int i = 0; i < AnimationTypeCount; i++)
            {
                Sprites.Add((AnimationTypes) i, new AnimatedSprite(100));
            }

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
                        ushort entityWorldId = reader.ReadUInt16();
                        if (WorldEntities.ContainsKey(entityWorldId))
                            onAttack(WorldEntities[entityWorldId]);

                        CurrentAnimation = AnimationTypes.EndAttacking;
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
                case UnitSignature.StartAttack:
                    {
                        CurrentAnimation = AnimationTypes.StartAttacking;
                    }
                    break;
                default:
                    break;
            }
        }


        protected virtual void onAttack(EntityBase ent)
        {
            ent.OnTakeDamage(StandardAttackDamage, StandardAttackElement);
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
            MaxHealth = reader.ReadSingle();
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

            drawPosition = Position;
        }

        protected virtual void onSetIdleAnimation()
        {
            CurrentAnimation = AnimationTypes.Idle;
        }

        protected virtual void onSetMovingAnimation()
        {
            CurrentAnimation = AnimationTypes.Moving;
        }

        public override void Move(float x, float y)
        {
            base.Move(x, y);
            _moveXCompleted = false;
            _moveYCompleted = false;
        }

        public override void Update(float ms)
        {
            if (Sprites.ContainsKey(CurrentAnimation))
            {
                Sprites[CurrentAnimation].Update(ms);
            }

            if(CurrentAnimation != AnimationTypes.SpellCast && (rallyPoints.Count == 0 || allowMovement == false)) onSetIdleAnimation();


            #region CLIENT PREDICTION INTERPOLATION
            Vector2f destination = Position;

            if ((int)drawPosition.X < (int)destination.X)
            {
                drawPosition.X += Speed * ms;
                if ((int)drawPosition.X >= (int)destination.X) drawPosition.X = destination.X;
            }
            if ((int)drawPosition.Y < (int)destination.Y)
            {
                drawPosition.Y += Speed * ms;
                if ((int)drawPosition.Y >= (int)destination.Y) drawPosition.Y = destination.Y;
            }
            if ((int)drawPosition.X > destination.X)
            {
                drawPosition.X -= Speed * ms;
                if ((int)drawPosition.X <= (int)destination.X) drawPosition.X = destination.X;
            }
            if ((int)drawPosition.Y > (int)destination.Y)
            {
                drawPosition.Y -= Speed * ms;
                if ((int)drawPosition.Y <= (int)destination.Y) drawPosition.Y = destination.Y;
            }
            #endregion

            if (!allowMovement || rallyPoints.Count == 0) return;
            if(CurrentAnimation != AnimationTypes.SpellCast)
                onSetMovingAnimation();

            destination = rallyPoints[0];

            if ((int)Position.X < (int)destination.X)
            {
                Position.X += Speed * ms;
                if ((int)Position.X >= (int)destination.X) _moveXCompleted = true;
            }
            if ((int)Position.Y < (int)destination.Y)
            {
                Position.Y += Speed * ms;
                if ((int)Position.Y >= (int)destination.Y) _moveYCompleted = true;
            }
            if ((int)Position.X > destination.X)
            {
                Position.X -= Speed * ms;
                if ((int)Position.X <= (int)destination.X) _moveXCompleted = true;
            }
            if ((int)Position.Y > (int)destination.Y)
            {
                Position.Y -= Speed * ms;
                if ((int)Position.Y <= (int)destination.Y) _moveYCompleted = true;
            }

            if ((int)Position.X == (int)destination.X) _moveXCompleted = true;
            if ((int)Position.Y == (int)destination.Y) _moveYCompleted = true;


            if (_moveXCompleted && _moveYCompleted)
            {
                _moveXCompleted = false;
                _moveYCompleted = false;

                if(rallyPoints.Count == 1)
                    Position = destination;
                rallyPoints.RemoveAt(0);
            }

        }

        public override void Render(RenderTarget target)
        {
            if(Sprites.ContainsKey(CurrentAnimation) && Sprites[CurrentAnimation].Sprites.Count > 0)
            {
                Sprite spr = Sprites[CurrentAnimation].CurrentSprite;

                spr.Position = drawPosition;
                spr.Origin = new Vector2f(spr.TextureRect.Width/2, spr.TextureRect.Height/2);
                target.Draw(spr);
            }
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
