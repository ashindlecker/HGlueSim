using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using System.IO;
using System.Diagnostics;
using SFML.Window;

namespace Server.Entities
{
    class Worker : UnitBase
    {
        public ResourceTypes heldResource;
        public byte resourceCount;

        public ushort GatherResourceTime; //How long it takes to gather a resource in milliseconds

        private Stopwatch passedGatherTime;

        //The worker should constantly move towards it's target, but not flood the client
        private Stopwatch updatedMovePositionTimer;
        private const float moveUpdateDelay = 3000; //3 seconds

        public bool IsHoldingResources
        {
            get { return (resourceCount > 0); }
        }

        public Worker(GameServer server, Player mPlayer) : base(server, mPlayer)
        {
            GatherResourceTime = 1000;
            passedGatherTime = new Stopwatch();

            EntityType = Entity.EntityType.Worker;
            Speed = .2f;
            Range = 60;
            Health = 50;
            MaxHealth = 50;

            updatedMovePositionTimer = new Stopwatch();
        }

        public override void OnPlayerCustomMove()
        {
            base.OnPlayerCustomMove();
            EntityToUse = null;
        }

        public void GiveResource(ResourceTypes type, byte amount)
        {
            if (!IsHoldingResources)
            {
                heldResource = type;
                resourceCount = amount;
            }
        }

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(base.UpdateData());
            writer.Write((byte)heldResource);
            writer.Write(resourceCount);

            return memory.ToArray();
        }

        protected override byte[] SetEntityToUseResponse(EntityBase toUse)
        {
            rallyPoints.Clear();
            State = UnitState.Standard;
            moveToUsedEntity(toUse);
            return base.SetEntityToUseResponse(toUse);
        }

        private void moveToUsedEntity(EntityBase toUse)
        {
            if (toUse != null)
                Move(toUse.Position.X, toUse.Position.Y);
        }

        protected void AddBuildingToBuild(byte type, float x, float y)
        {
            Move(x, y, Entity.RallyPoint.RallyTypes.Build, false, true, type);
        }

        public override void Update(float ms)
        {
            base.Update(ms);

            if(EntityToUse != null)
            {
                if (RangeBounds().Contains(EntityToUse.Position.X, EntityToUse.Position.Y))
                {
                    updatedMovePositionTimer.Restart();
                    if(EntityToUse.EntityType == Entity.EntityType.Resources)
                    {
                        if (IsHoldingResources == false && passedGatherTime.ElapsedMilliseconds >= GatherResourceTime)
                        {
                            //Grab Resources
                            EntityToUse.Use(this);
                            passedGatherTime.Restart();
                        }
                        else if(IsHoldingResources == true)
                        {
                            //Go to closest base if availible
                            EntityBase homeEntity = GetClosest<EntityBase>(Entity.EntityType.HomeBuilding);
                            if (homeEntity != null)
                                SetEntityToUse(homeEntity);
                        }
                    }
                    else if(EntityToUse.EntityType == Entity.EntityType.HomeBuilding)
                    {
                        if (EntityToUse.Team != Team)
                        {
                            SetEntityToUse(null);
                        }
                        else
                        {
                            //Give resources to base
                            EntityToUse.Use(this);

                            //Go back to closest resource field

                            EntityBase resourceEntity = GetClosest<EntityBase>(Entity.EntityType.Resources);
                            if (resourceEntity != null)
                                SetEntityToUse(resourceEntity);
                        }
                    }
                    else
                    {
                        EntityToUse.Use(this);
                    }
                }
                else
                {
                    if (EntityToUse.EntityType == Entity.EntityType.Resources)
                    {
                        passedGatherTime.Restart();
                    }
                }

                if (updatedMovePositionTimer.ElapsedMilliseconds >= moveUpdateDelay)
                {
                    updatedMovePositionTimer.Restart();
                    moveToUsedEntity(EntityToUse);
                }
            }
            else
            {
                updatedMovePositionTimer.Restart();
            }
        }

        protected override void OnRallyPointCompleted(Entity.RallyPoint rally)
        {
            base.OnRallyPointCompleted(rally);
            if(rally.RallyType == Entity.RallyPoint.RallyTypes.Build)
            {
                if(spells.ContainsKey(rally.BuildType))
                {
                    if(spells[rally.BuildType].AppleCost <= MyPlayer.Apples && spells[rally.BuildType].GlueCost <= MyPlayer.Glue && spells[rally.BuildType].WoodCost <= MyPlayer.Wood)
                    {
                        MyPlayer.Apples -= spells[rally.BuildType].AppleCost;
                        MyPlayer.Glue -= spells[rally.BuildType].GlueCost;
                        MyPlayer.Wood -= spells[rally.BuildType].WoodCost;
                    }
                }
                var ent = OnPlaceBuilding(rally.BuildType, rally.X, rally.Y);
                ent.Position = new Vector2f(rally.X, rally.Y);
                ent.Team = Team;
                MyGameMode.AddEntity(ent);
                MyGameMode.UpdatePlayer(MyPlayer);
            }
        }

        protected virtual EntityBase OnPlaceBuilding(byte type, float x, float y)
        {
            return null;
        }

        protected TYPE GetClosest<TYPE>(Entity.EntityType eType) where TYPE:EntityBase
        {
            float shortest = 0;
            TYPE ret = null;

            foreach (var entity in MyGameMode.WorldEntities.Values)
            {
                if (entity.EntityType != eType)
                    continue;
                if(entity.Neutral == false)
                {
                    if(entity.Team != Team)
                    continue;
                }

                float distance = Math.Abs(Position.X - entity.Position.X) + Math.Abs(Position.Y - entity.Position.Y);
                if (ret == null || distance < shortest)
                {
                    shortest = distance;
                    ret = (TYPE)entity;
                }
            }
            return ret;
        }
    }
}
