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


        public float Speed;
        public UnitState State;
        public float Range;
        public ushort AttackDelay;

        //movement is typically stopped when a unit is attacking, after the attack the unit continues moving
        private bool allowMovement;


        private Stopwatch attackTimer;
        protected EntityBase EntityToAttack { get; private set; }


        private float _moveAngle;
        private float _moveX, _moveY;
        private bool _callMoveCalculations;

        public UnitBase(GameServer _server) : base(_server)
        {
            EntityToAttack = null;

            EntityType = Entity.EntityType.Unit;
            Speed = .01f;
            State = UnitState.Agro;
            Range = 50;
            AttackDelay = 2000;
            attackTimer = new Stopwatch();
            attackTimer.Restart();

            allowMovement = false;

            _moveAngle = 0;
            _moveX = 0;
            _moveY = 0;
            _callMoveCalculations = true;
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
            _callMoveCalculations = true;
        }

        public override void Update(float ms)
        {
            if(State == UnitState.Agro)
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

            if(EntityToAttack == null || State == UnitState.Standard) setAllowMove(true);

            if (State != UnitState.Agro)
                attackTimer.Restart();

            //Rallypoint movement
            if (allowMovement && rallyPoints.Count > 0)
            {

                if (rallyPoints[0].RallyType == Entity.RallyPoint.RallyTypes.AttackMove)
                    State = UnitState.Agro;
                else
                    State = UnitState.Standard;

                Vector2f destination = new Vector2f(rallyPoints[0].X, rallyPoints[0].Y);


                if (_callMoveCalculations)
                {
                    _callMoveCalculations = false;

                    _moveAngle = (float) Math.Atan2(destination.Y - Position.Y, destination.X - Position.X);
                    _moveX = (float) Math.Cos(_moveAngle);
                    _moveY = (float) Math.Sin(_moveAngle);
                }

                Position.X += (_moveX*Speed)*ms;
                Position.Y += (_moveY*Speed)*ms;

                bool completedDestinationX = (_moveX > 0 && Position.X >= destination.X) ||
                                             (_moveX < 0 && Position.X <= destination.X) || (int)Position.X == (int)destination.X;

                bool completedDestinationY = (_moveY > 0 && Position.Y >= destination.Y) ||
                                             (_moveY < 0 && Position.Y <= destination.Y) || (int)Position.Y == (int)destination.Y;


                if (completedDestinationX && completedDestinationY)
                {
                    _callMoveCalculations = true;
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
            entity.TakeDamage(10, Entity.DamageElement.Normal, false);
            return new byte[0];
        }

        protected override byte[] MoveResponse(float x, float y, bool reset)
        {
            _callMoveCalculations = true;
            return base.MoveResponse(x, y, reset);
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
