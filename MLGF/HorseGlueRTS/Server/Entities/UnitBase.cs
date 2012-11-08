using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using System.IO;
using SFML.Window;
using System.Diagnostics;
using SFML.Graphics;
namespace Server.Entities
{
    class UnitBase : EntityBase
    {
        public enum UnitState : byte
        {
            Agro,
            Standard,
        }

        public byte SupplyUsage
        {
            get; protected set;
        }

        public float StandardAttackDamage { get; protected set; }
        public Entity.DamageElement StandardAttackElement { get; protected set; }


        public float Speed;
        public UnitState State;
        public float Range;
        public ushort AttackDelay;

        //movement is typically stopped when a unit is attacking, after the attack the unit continues moving
        private bool allowMovement;


        private Stopwatch attackTimer;
        protected EntityBase EntityToAttack { get; private set; }

        private bool _moveXCompleted, _moveYCompleted;


        public UnitBase(GameServer _server, Player player) : base(_server, player)
        {
            EntityType = Entity.EntityType.Unit;

            EntityToAttack = null;

            Speed = .01f;
            Range = 50;
            AttackDelay = 2000;
            SupplyUsage = 1;

            StandardAttackDamage = 1;
            StandardAttackElement = Entity.DamageElement.Normal;

            State = UnitState.Agro;
            allowMovement = false;
            _moveXCompleted = false;
            _moveYCompleted = false;

            attackTimer = new Stopwatch();
            attackTimer.Restart();
        }

        protected FloatRect RangeBounds()
        {
            return new FloatRect(Position.X - (Range/2), Position.Y - (Range/2), Range, Range);
        }

        private void clearRally()
        {
            rallyPoints.Clear();
            SendData(new byte[1] { (byte)UnitSignature.ClearRally }, Entity.Signature.Custom);
        }

        protected void setAllowMove(bool value)
        {
            if (allowMovement != value)
            {
                allowMovement = value;
                SendData(new byte[2] { (byte)UnitSignature.ChangeMovementAllow, BitConverter.GetBytes(value)[0] }, Entity.Signature.Custom);
            }
        }

        public override void OnPlayerCustomMove()
        {
            base.OnPlayerCustomMove();
            _moveXCompleted = false;
            _moveYCompleted = false;
        }

        public override void Update(float ms)
        {
            if (State == UnitState.Agro)
            {
                var rangeBounds = RangeBounds();

                //If the unit has something to attack
                if (EntityToAttack != null)
                {
                    //If the unit is dead, there's nothing to attack
                    if (EntityToAttack.Health <= 0)
                    {
                        EntityToAttack = null;
                    }
                    else
                    {
                        //If the entity to attack is not in range, allow movement
                        if (!rangeBounds.Intersects(EntityToAttack.GetBounds()))
                        {
                            attackTimer.Restart();
                            EntityToAttack = null;
                            setAllowMove(true);
                        }
                        else //Otherwize, try to attack and stop ability to move
                        {
                            if (rallyPoints.Count > 0)
                            {
                                setAllowMove(false);
                            }
                            Attack(EntityToAttack);
                        }
                    }
                }
                else //Otherwise look for something to attack
                {
                    foreach (var entity in MyGameMode.WorldEntities.Values)
                    {
                        if (entity.Team == Team) continue;

                        if (!entity.Neutral && entity.Health > 0 && rangeBounds.Intersects(entity.GetBounds()))
                        {
                            EntityToAttack = entity;
                            break;
                        }
                    }
                }
            }

            if (EntityToAttack == null || State == UnitState.Standard) setAllowMove(true);

            if (State != UnitState.Agro)
                attackTimer.Restart();
            if(rallyPoints.Count == 0)
                State = UnitState.Agro;
            //Rallypoint movement
            if (allowMovement && rallyPoints.Count > 0)
            {
                if (rallyPoints[0].RallyType == Entity.RallyPoint.RallyTypes.AttackMove)
                    State = UnitState.Agro;
                else
                    State = UnitState.Standard;

                var destination = new Vector2f(rallyPoints[0].X, rallyPoints[0].Y);

                if ((int) Position.X < (int) destination.X)
                {
                    Position.X += Speed*ms;
                    if ((int)Position.X >= (int)destination.X) _moveXCompleted = true;
                }
                if ((int) Position.Y < (int) destination.Y)
                {
                    Position.Y += Speed*ms;
                    if ((int)Position.Y >= (int)destination.Y) _moveYCompleted = true;
                }
                if ((int) Position.X > destination.X)
                {
                    Position.X -= Speed*ms;
                    if ((int)Position.X <= (int)destination.X) _moveXCompleted = true;
                }
                if ((int) Position.Y > (int) destination.Y)
                {
                    Position.Y -= Speed*ms;
                    if ((int)Position.Y <= (int)destination.Y) _moveYCompleted = true;
                }

                if ((int)Position.X == (int)destination.X) _moveXCompleted = true;
                if ((int)Position.Y == (int)destination.Y) _moveYCompleted = true;

                if (_moveXCompleted && _moveYCompleted)
                {
                    _moveXCompleted = false;
                    _moveYCompleted = false;

                    if (rallyPoints.Count == 1)
                        Position = destination;

                    OnRallyPointCompleted(rallyPoints[0]);
                    rallyPoints.RemoveAt(0);
                    if (rallyPoints.Count == 0)
                    {
                        State = UnitState.Agro;

                        var memory = new MemoryStream();
                        var writer = new BinaryWriter(memory);

                        writer.Write((byte) UnitSignature.RallyCompleted);
                        writer.Write(Position.X);
                        writer.Write(Position.Y);

                        SendData(memory.ToArray(), Entity.Signature.Custom);

                        memory.Close();
                        writer.Close();
                    }
                }
            }
        }

        protected virtual void OnRallyPointCompleted(Entity.RallyPoint rally)
        {
            //Called when a rally is popped off
        }

        public void Attack(EntityBase entity)
        {
            if (entity.Team == Team) return;
            if (attackTimer.ElapsedMilliseconds < AttackDelay) return;

            attackTimer.Restart();

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)UnitSignature.Attack);
            writer.Write((ushort)entity.WorldId);
            writer.Write(onAttack(entity));

            SendData(memory.ToArray(), Entity.Signature.Custom);

            writer.Close();
            memory.Close();
        }

        protected virtual byte[] onAttack(EntityBase entity)
        {
            entity.TakeDamage(StandardAttackDamage, StandardAttackElement, false);
            return new byte[0];
        }

        public override void OnDeath()
        {
            base.OnDeath();
            MyPlayer.UsedSupply -= UnitData.WorkerSupplyCost;
            MyGameMode.UpdatePlayer(MyPlayer);
        }

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((bool)( EntityToUse != null));
            if(EntityToUse != null)
            {
                writer.Write(EntityToUse.WorldId);
            }

            writer.Write(Health);
            writer.Write((byte)State);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Speed);
            writer.Write(Energy);
            writer.Write(Range);
            writer.Write(allowMovement);
            writer.Write((byte)rallyPoints.Count);
            for (var i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write(rallyPoints[i].X);
                writer.Write(rallyPoints[i].Y);
            }
            return memory.ToArray();
        }

    }
}
