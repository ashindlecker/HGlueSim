using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SFML.Graphics;
using SFML.Window;
using Shared;

namespace Client.Entities
{
    internal class UnitBase : EntityBase
    {
        #region AnimationTypes enum

        public enum AnimationTypes : byte
        {
            Idle,
            Moving,
            EndAttacking,
            StartAttacking,
            SpellCast,
            IdleWithResources,
            MovingWithResources,
            GrabbingResources,
        }

        #endregion

        #region UnitState enum

        public enum UnitState : byte
        {
            Agro,
            Standard,
        }

        #endregion

        private readonly Stopwatch attackTimer;
        public ushort AttackDelay;
        public float Range;
        public float Speed;

        protected Dictionary<AnimationTypes, AnimatedSprite> Sprites;
        public UnitState State;

        private AnimationTypes _currentAnimation;

        private bool _moveXCompleted, _moveYCompleted;
        private bool allowMovement;

        private Vector2f drawPosition;

        protected UnitTypes UnitType;
        public bool RangedUnit;

        public float StandardAttackDamage { get; protected set; }
        public Entity.DamageElement StandardAttackElement { get; protected set; }
        protected EntityBase EntityToAttack { get; private set; }


        protected AnimationTypes CurrentAnimation
        {
            get { return _currentAnimation; }
            set
            {
                if (CurrentAnimation == AnimationTypes.StartAttacking || CurrentAnimation == AnimationTypes.EndAttacking ||
                    CurrentAnimation == AnimationTypes.SpellCast || CurrentAnimation == AnimationTypes.GrabbingResources)
                {
                    if (Sprites[CurrentAnimation].AnimationCompleted)
                    {
                        _currentAnimation = value;
                        Sprites[CurrentAnimation].Reset();
                    }
                }
                else
                {
                    _currentAnimation = value;
                    Sprites[CurrentAnimation].Reset();
                }
            }
        }

        protected string SpriteFolder { get; set; }

        public UnitBase()
        {
            UnitType = UnitTypes.Default;

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

            Sprites = new Dictionary<AnimationTypes, AnimatedSprite>();

            const byte AnimationTypeCount = 8;
            for (int i = 0; i < AnimationTypeCount; i++)
            {
                Sprites.Add((AnimationTypes)i, new AnimatedSprite(100));
            }

            CurrentAnimation = AnimationTypes.Idle;
            SpriteFolder = "";
        }

        public override void Move(float x, float y)
        {
            base.Move(x, y);
            _moveXCompleted = false;
            _moveYCompleted = false;
        }

        protected override void ParseCustom(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);
            var signature = (UnitSignature) reader.ReadByte();

            switch (signature)
            {
                case UnitSignature.RallyCompleted:
                    {
                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();

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
                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();

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
                        MyGameMode.PlayAttackSound(ExternalResources.AttackSounds.CliffGetFucked);
                    }
                    break;
                case UnitSignature.GrabbingResources:
                    {
                        CurrentAnimation = AnimationTypes.GrabbingResources;
                        MyGameMode.PlayGatherResourcesSound(ExternalResources.ResourceSounds.CliffMining);
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void ParseUpdate(MemoryStream memoryStream)
        {
            var reader = new BinaryReader(memoryStream);

            UnitType = (UnitTypes) reader.ReadByte();

            bool hasEntToUse = reader.ReadBoolean();
            if (hasEntToUse)
            {
                ushort id = reader.ReadUInt16();
                if (WorldEntities.ContainsKey(id))
                {
                    EntityToUse = WorldEntities[id];
                }
            }

            RangedUnit = reader.ReadBoolean();
            Health = reader.ReadSingle();
            MaxHealth = reader.ReadSingle();
            State = (UnitState) reader.ReadByte();
            Position = new Vector2f(reader.ReadSingle(), reader.ReadSingle());
            Speed = reader.ReadSingle();
            Energy = reader.ReadUInt16();
            Range = reader.ReadSingle();
            allowMovement = reader.ReadBoolean();

            byte rallyCount = reader.ReadByte();
            rallyPoints.Clear();
            for (int i = 0; i < rallyCount; i++)
            {
                rallyPoints.Add(new Vector2f(reader.ReadSingle(), reader.ReadSingle()));
            }

            drawPosition = Position;
        }

        public override void Render(RenderTarget target)
        {
            if (Sprites.ContainsKey(CurrentAnimation) && Sprites[CurrentAnimation].Sprites.Count > 0)
            {
                Sprite spr = Sprites[CurrentAnimation].CurrentSprite;

                spr.Position = drawPosition;
                spr.Origin = new Vector2f(spr.TextureRect.Width/2, spr.TextureRect.Height/2);
                target.Draw(spr);
            }
        }

        public override void SetTeam(byte team)
        {
            base.SetTeam(team);

            Sprite[] idleSprites =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" + "Idle/");
            if (idleSprites != null)
                Sprites[AnimationTypes.Idle].Sprites.AddRange(idleSprites);

            Sprite[] moveSprites =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "Moving/");
            if (moveSprites != null)
                Sprites[AnimationTypes.Moving].Sprites.AddRange(moveSprites);

            Sprite[] resourceMoveSprites =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "MovingWithResources/");
            if (resourceMoveSprites != null)
                Sprites[AnimationTypes.MovingWithResources].Sprites.AddRange(resourceMoveSprites);

            Sprite[] resourceIdleSprites =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "IdleWithResources/");
            if (resourceIdleSprites != null)
                Sprites[AnimationTypes.IdleWithResources].Sprites.AddRange(resourceIdleSprites);

            Sprite[] beginAttack =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "BeginAttack/");
            if (beginAttack != null)
                Sprites[AnimationTypes.StartAttacking].Sprites.AddRange(beginAttack);
            Sprites[AnimationTypes.StartAttacking].Loop = false;

            Sprite[] afterAttack =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "AfterAttack/");
            if (afterAttack != null)
                Sprites[AnimationTypes.EndAttacking].Sprites.AddRange(afterAttack);
            Sprites[AnimationTypes.EndAttacking].Loop = false;

            Sprite[] grabbingResources =
                ExternalResources.GetSprites("Resources/Sprites/" + SpriteFolder + "/" + team.ToString() + "/" +
                                             "GrabbingResources/");
            if (grabbingResources != null)
                Sprites[AnimationTypes.GrabbingResources].Sprites.AddRange(grabbingResources);
            Sprites[AnimationTypes.GrabbingResources].Loop = false;
        }

        public override void Update(float ms)
        {
            if (Sprites.ContainsKey(CurrentAnimation))
            {
                Sprites[CurrentAnimation].Update(ms);
            }

            if ((rallyPoints.Count == 0 || allowMovement == false)) onSetIdleAnimation();

            #region CLIENT PREDICTION INTERPOLATION

            Vector2f destination = Position;

            if ((int) drawPosition.X < (int) destination.X)
            {
                drawPosition.X += Speed*ms;
                if ((int) drawPosition.X >= (int) destination.X) drawPosition.X = destination.X;
            }
            if ((int) drawPosition.Y < (int) destination.Y)
            {
                drawPosition.Y += Speed*ms;
                if ((int) drawPosition.Y >= (int) destination.Y) drawPosition.Y = destination.Y;
            }
            if ((int) drawPosition.X > destination.X)
            {
                drawPosition.X -= Speed*ms;
                if ((int) drawPosition.X <= (int) destination.X) drawPosition.X = destination.X;
            }
            if ((int) drawPosition.Y > (int) destination.Y)
            {
                drawPosition.Y -= Speed*ms;
                if ((int) drawPosition.Y <= (int) destination.Y) drawPosition.Y = destination.Y;
            }

            #endregion

            if (!allowMovement || rallyPoints.Count == 0) return;
            if (CurrentAnimation != AnimationTypes.SpellCast)
                onSetMovingAnimation();

            destination = rallyPoints[0];

            if ((int) Position.X < (int) destination.X)
            {
                Position.X += Speed*ms;
                if ((int) Position.X >= (int) destination.X) _moveXCompleted = true;
            }
            if ((int) Position.Y < (int) destination.Y)
            {
                Position.Y += Speed*ms;
                if ((int) Position.Y >= (int) destination.Y) _moveYCompleted = true;
            }
            if ((int) Position.X > destination.X)
            {
                Position.X -= Speed*ms;
                if ((int) Position.X <= (int) destination.X) _moveXCompleted = true;
            }
            if ((int) Position.Y > (int) destination.Y)
            {
                Position.Y -= Speed*ms;
                if ((int) Position.Y <= (int) destination.Y) _moveYCompleted = true;
            }

            if ((int) Position.X == (int) destination.X) _moveXCompleted = true;
            if ((int) Position.Y == (int) destination.Y) _moveYCompleted = true;


            if (_moveXCompleted && _moveYCompleted)
            {
                _moveXCompleted = false;
                _moveYCompleted = false;

                if (rallyPoints.Count == 1)
                    Position = destination;
                rallyPoints.RemoveAt(0);
            }
        }

        protected void debugDrawRange(RenderTarget target)
        {
            var circle = new CircleShape(Range);
            circle.Origin = new Vector2f(circle.Radius, circle.Radius);
            circle.Position = Position;
            circle.FillColor = new Color(255, 0, 0, 100);

            target.Draw(circle);
        }

        protected virtual void onAttack(EntityBase ent)
        {
            if (!RangedUnit) //Happy asshole?
            {
                ent.OnTakeDamage(StandardAttackDamage, StandardAttackElement);
            }
            //Ranged units send projectiles, this is handled in the game mode, and is not needed to do prediction here
        }

        protected virtual void onSetIdleAnimation()
        {
            CurrentAnimation = AnimationTypes.Idle;
        }

        protected virtual void onSetMovingAnimation()
        {
            CurrentAnimation = AnimationTypes.Moving;
        }
    }
}