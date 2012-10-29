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

        public override void Update(float ms)
        {
            if(State == UnitState.Agro)
            {
                var rangeBounds = RangeBounds();
                if (EntityToAttack != null)
                {
                    if (EntityToAttack.Health <= 0)
                    {
                        EntityToAttack = null;
                    }
                    else
                    {
                        if (!rangeBounds.Intersects(EntityToAttack.GetBounds()))
                        {
                            EntityToAttack = null;
                            setAllowMove(true);
                        }
                        else
                        {
                            if (rallyPoints.Count > 0)
                            {
                                setAllowMove(false);
                            }
                            Attack(EntityToAttack);
                        }
                    }
                }
                else
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

            //Rallypoint movement
            if (allowMovement && rallyPoints.Count > 0)
            {
                Vector2f destination = rallyPoints[0];
                if ((int)Position.X < (int)destination.X)
                {
                    Position.X += Speed*ms;
                    if ((int)Position.X >= (int)destination.X) Position.X = (int)destination.X;
                }
                if ((int)Position.Y < (int)destination.Y)
                {
                    Position.Y += Speed*ms;
                    if ((int)Position.Y >= (int)destination.Y) Position.Y = (int)destination.Y;
                }
                if ((int)Position.X > destination.X)
                {
                    Position.X -= Speed*ms;
                    if ((int)Position.X <= (int)destination.X) Position.X = (int)destination.X;
                }
                if ((int)Position.Y > (int)destination.Y)
                {
                    Position.Y -= Speed*ms;
                    if ((int)Position.Y <= (int)destination.Y) Position.Y = (int)destination.Y;
                }

                if ((int) Position.X == (int) destination.X && (int) Position.Y == (int) destination.Y)
                {
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

        public void Attack(EntityBase entity)
        {
            if (entity.Team == Team) return;
            if (attackTimer.ElapsedMilliseconds < AttackDelay) return;

            rallyPoints.Clear();
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
