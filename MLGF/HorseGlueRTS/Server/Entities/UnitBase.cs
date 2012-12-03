using System;
using System.Diagnostics;
using System.IO;
using SFML.Graphics;
using SFML.Window;
using Shared;
using System.Xml;
using System.Xml.Linq;


namespace Server.Entities
{
    internal class UnitBase : EntityBase
    {
        #region UnitState enum

        public enum UnitState : byte
        {
            Agro,
            Standard,
        }

        #endregion

        private readonly Stopwatch attackTimer;
        private readonly Stopwatch rechargeTimer;

        private const float moveUpdateDelay = 3000; //3 seconds
        private readonly Stopwatch updatedMovePositionTimer;

        public ushort AttackDelay;
        public ushort AttackRechargeTime;
        public float Range;

        public float Speed;
        public UnitState State;

        private bool _moveXCompleted, _moveYCompleted;
        private bool allowMovement;

        protected bool RangedUnit;
        protected UnitTypes UnitType;

        public UnitBase(GameServer _server, Player player) : base(_server, player)
        {
            EntityType = Entity.EntityType.Unit;
            UnitType = UnitTypes.Default;

            EntityToAttack = null;

            Speed = .01f;
            Range = 50;
            AttackDelay = 100;
            AttackRechargeTime = 1000;
            SupplyUsage = 1;
            RangedUnit = false;

            StandardAttackDamage = 1;
            StandardAttackElement = Entity.DamageElement.Normal;

            State = UnitState.Agro;
            allowMovement = false;
            _moveXCompleted = false;
            _moveYCompleted = false;

            attackTimer = new Stopwatch();
            attackTimer.Reset();
            attackTimer.Stop();

            rechargeTimer = new Stopwatch();
            rechargeTimer.Restart();


            updatedMovePositionTimer = new Stopwatch();
            updatedMovePositionTimer.Start();
        }

        public byte SupplyUsage { get; protected set; }

        public float StandardAttackDamage { get; protected set; }
        public Entity.DamageElement StandardAttackElement { get; protected set; }
        protected EntityBase EntityToAttack { get; private set; }

        public void Attack(EntityBase entity)
        {
            if (entity.Team == Team) return;
            if (attackTimer.ElapsedMilliseconds < AttackDelay) return;

            attackTimer.Reset();
            attackTimer.Stop();

            rechargeTimer.Restart();

            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte) UnitSignature.Attack);
            writer.Write(entity.WorldId);
            writer.Write(onAttack(entity));

            SendData(memory.ToArray(), Entity.Signature.Custom);

            writer.Close();
            memory.Close();
        }

        public override void OnDeath()
        {
            base.OnDeath();
            if (MyPlayer != null)
            {
                MyPlayer.UsedSupply -= SupplyUsage;
                MyGameMode.UpdatePlayer(MyPlayer);
            }
        }

        public override void OnPlayerCustomMove()
        {
            base.OnPlayerCustomMove();
            _moveXCompleted = false;
            _moveYCompleted = false;
        }

        protected virtual void OnRallyPointCompleted(Entity.RallyPoint rally)
        {
            //Called when a rally is popped off
        }

        protected FloatRect RangeBounds()
        {
            return new FloatRect(Position.X - (Range/2), Position.Y - (Range/2), Range, Range);
        }

        private FloatRect SearchBounds()
        {
            const float SEARCHSIZE = 200;
            if(Range < SEARCHSIZE)
            return new FloatRect(Position.X - (SEARCHSIZE/2), Position.Y - (SEARCHSIZE/2), SEARCHSIZE, SEARCHSIZE);
            return RangeBounds();
        }

        protected override byte[] SetEntityToUseResponse(EntityBase toUse)
        {
            rallyPoints.Clear();
            State = UnitState.Standard;
            moveToUsedEntity(toUse);
            return base.SetEntityToUseResponse(toUse);
        }

        public virtual void StartAttack()
        {
            attackTimer.Restart();
            SendData(new byte[1] {(byte) UnitSignature.StartAttack}, Entity.Signature.Custom);
        }

        public virtual void StopAttack()
        {
            attackTimer.Reset();
            attackTimer.Stop();
        }

        public override void Update(float ms)
        {
            if(EntityToUse != null)
            {
                if (updatedMovePositionTimer.ElapsedMilliseconds >= moveUpdateDelay)
                {
                    updatedMovePositionTimer.Restart();
                    moveToUsedEntity(EntityToUse);
                }
            }
            else
            {
                updatedMovePositionTimer.Reset();
            }
            if (State == UnitState.Agro)
            {
                FloatRect rangeBounds = RangeBounds();
                FloatRect searchBounds = SearchBounds();

                //If the unit has something to attack
                if (EntityToAttack != null)
                {
                    //If the unit is dead, there's nothing to attack
                    if (EntityToAttack.Health <= 0)
                    {
                        EntityToAttack = null;
                        StopAttack();
                    }
                    else
                    {
                        if ( attackTimer.IsRunning == false)
                        {
                            //If the entity to attack is not in range, allow movement
                            if (!rangeBounds.Intersects(EntityToAttack.GetBounds()))
                            {
                                StopAttack();
                                setAllowMove(true);

                                if (!searchBounds.Intersects(EntityToAttack.GetBounds()))
                                {
                                    EntityToAttack = null;
                                }
                                else
                                {
                                    Move(EntityToAttack.Position.X, EntityToAttack.Position.Y,
                                         Entity.RallyPoint.RallyTypes.AttackMove, true, true, "", true);
                                }
                            }
                            else //Otherwize, try to attack and stop ability to move
                            {
                                if (rechargeTimer.ElapsedMilliseconds >= AttackRechargeTime )
                                {
                                    if (rallyPoints.Count > 0)
                                    {
                                        setAllowMove(false);
                                    }
                                    //start the attack
                                    StartAttack();
                                }
                            }
                        }
                        else
                        {
                            if (attackTimer.ElapsedMilliseconds >= AttackDelay)
                            {
                                Attack(EntityToAttack);
                            }
                        }
                    }
                }
                else //Otherwise look for something to attack
                {
                    StopAttack();
                    foreach (EntityBase entity in MyGameMode.WorldEntities.Values)
                    {
                        if (entity.Team == Team) continue;

                        if (!entity.Neutral && entity.Health > 0 && searchBounds.Intersects(entity.GetBounds()))
                        {
                            EntityToAttack = entity;
                            break;
                        }
                    }
                }
            }

            if (EntityToAttack == null || State == UnitState.Standard)
            {
                StopAttack();
                setAllowMove(true);
            }

            if (rallyPoints.Count == 0)
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

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write((byte)UnitType);
            writer.Write((EntityToUse != null));
            if (EntityToUse != null)
            {
                writer.Write(EntityToUse.WorldId);
            }

            writer.Write(RangedUnit);
            writer.Write(Health);
            writer.Write(MaxHealth);
            writer.Write((byte) State);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Speed);
            writer.Write(Energy);
            writer.Write(Range);
            writer.Write(allowMovement);
            writer.Write((byte) rallyPoints.Count);
            for (int i = 0; i < rallyPoints.Count; i++)
            {
                writer.Write(rallyPoints[i].X);
                writer.Write(rallyPoints[i].Y);
            }

            writer.Write(StandardAttackDamage);
            writer.Write(HotkeyString);

            return memory.ToArray();
        }

        private void clearRally()
        {
            rallyPoints.Clear();
            SendData(new byte[1] {(byte) UnitSignature.ClearRally}, Entity.Signature.Custom);
        }

        protected virtual byte[] onAttack(EntityBase entity)
        {
            if (!RangedUnit) //Happy asshole?
            {
                entity.TakeDamage(StandardAttackDamage, StandardAttackElement, false);
            }
            else
            {
                var rangedBullet = new Projectiles.ProjectileBase(Server, MyPlayer, Position, entity, StandardAttackDamage, StandardAttackElement);
                MyGameMode.AddEntity(rangedBullet);
            }
            return new byte[0];
        }

        protected void setAllowMove(bool value)
        {
            if (allowMovement != value)
            {
                allowMovement = value;
                SendData(new byte[2] {(byte) UnitSignature.ChangeMovementAllow, BitConverter.GetBytes(value)[0]},
                         Entity.Signature.Custom);
            }
        }

        private void moveToUsedEntity(EntityBase toUse)
        {
            if (toUse != null)
            {
                Move(toUse.Position.X, toUse.Position.Y,
                     noclipLast:
                         (toUse.EntityType == Entity.EntityType.HomeBuilding ||
                          toUse.EntityType == Entity.EntityType.GlueFactory));
            }
        }

        public static UnitBase LoadUnitFromXML(string unit, GameServer server, Player player)
        {

            var unitSetting = Settings.GetUnit(unit);
            if(unitSetting == null) return new UnitBase(server, player);

            UnitBase retUnit = null;
            switch (unitSetting.Type)
            {
                default:
                case "default":
                    retUnit = new UnitBase(server, player);
                    break;
                case "worker":
                    retUnit = new Worker(server, player);
                    break;
            }

            retUnit.RangedUnit = unitSetting.RangedUnit;
            retUnit.Range = unitSetting.Range;
            retUnit.Speed = unitSetting.Speed;
            retUnit.AttackDelay = unitSetting.AttackDelay;
            retUnit.AttackRechargeTime = unitSetting.AttackRechargeTime;
            retUnit.StandardAttackDamage = unitSetting.StandardAttackDamage;

            foreach (var spellXmlData in unitSetting.Spells)
            {
                var spellData = new SpellData(spellXmlData.EnergyCost, null);
                spellData.SpellType = SpellTypes.Normal;
                if(spellXmlData.IsBuildSpell)
                    spellData.SpellType = SpellTypes.BuildingPlacement;

                spellData.WoodCost = spellXmlData.WoodCost;
                spellData.EnergyCost = spellXmlData.EnergyCost;
                spellData.AppleCost = spellXmlData.AppleCost;
                spellData.GlueCost = spellXmlData.GlueCost;
                retUnit.spells.Add(spellXmlData.BuildString, spellData);
            }

            retUnit.Health = unitSetting.Health;
            retUnit.MaxHealth = unitSetting.MaxHealth;
            retUnit.SupplyUsage = unitSetting.SupplyCost;
            retUnit.HotkeyString = unitSetting.Name;
            return retUnit;
        }

        public static UnitBase CreateUnit(UnitTypes unit, GameServer server, Player player)
        {
            UnitBase retUnit = null;

            switch (unit)
            {
                default:
                case UnitTypes.Default:
                    retUnit = LoadUnitFromXML("default", server, player);
                    break;
                case UnitTypes.Worker:
                    retUnit = LoadUnitFromXML("worker", server, player);
                    break;
                case UnitTypes.Unicorn:
                    retUnit = LoadUnitFromXML("unicorn", server, player);
                    break;
            }

            return retUnit;
        }
        public static UnitBase CreateUnit(string unit, GameServer server, Player player)
        {
            return LoadUnitFromXML(unit, server, player);
        }
    }
}