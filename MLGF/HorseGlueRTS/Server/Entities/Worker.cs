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

        protected class BuildingListData
        {
            public BuildingListData(Vector2f pos, EntityBase entity)
            {
                Position = pos;
                EntityToAdd = entity;
            }
            public Vector2f Position;
            public EntityBase EntityToAdd;
        }

        protected List<BuildingListData> buildingsToBuild; 


        public bool IsHoldingResources
        {
            get { return (resourceCount > 0); }
        }

        protected Player MyPlayer;

        public Worker(GameServer server, Player mPlayer) : base(server)
        {
            MyPlayer = mPlayer;

            GatherResourceTime = 1000;
            passedGatherTime = new Stopwatch();

            EntityType = Entity.EntityType.Worker;
            Speed = .2f;
            Range = 60;
            Health = 50;
            MaxHealth = 50;

            buildingsToBuild = new List<BuildingListData>();
            
            updatedMovePositionTimer = new Stopwatch();
        }

        public override void OnPlayerCustomMove()
        {
            EntityToUse = null;
            buildingsToBuild.Clear();
        }


        public override void OnDeath()
        {
            base.OnDeath();
            MyPlayer.Supply -= UnitData.WorkerSupplyCost;
            MyGameMode.UpdatePlayer(MyPlayer);
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
            updatedMovePositionTimer.Restart();
            rallyPoints.Clear();
            moveToUsedEntity(toUse);
            return base.SetEntityToUseResponse(toUse);
        }

        protected void moveToUsedEntity(EntityBase toUse)
        {
            if (toUse != null)
                Move(toUse.Position.X, toUse.Position.Y);
        }

        public override void Update(float ms)
        {
            base.Update(ms);

            foreach (var building in buildingsToBuild)
            {
                if(RangeBounds().Contains(building.Position.X, building.Position.Y))
                {
                    var entity = building.EntityToAdd;
                    entity.Position = building.Position;
                    MyGameMode.AddEntity(entity);
                    buildingsToBuild.Remove(building);
                    break;
                }
            }

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
        }

        protected TYPE GetClosest<TYPE>(Entity.EntityType eType) where TYPE:EntityBase
        {
            float shortest = 0;
            TYPE ret = null;

            foreach (var entity in MyGameMode.WorldEntities.Values)
            {
                if (entity.EntityType != eType)
                    continue;
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
