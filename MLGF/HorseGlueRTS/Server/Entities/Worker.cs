using System;
using System.Diagnostics;
using System.IO;
using SFML.Window;
using Server.Entities.Buildings;
using Shared;
using SFML.Graphics;

namespace Server.Entities
{
    internal class Worker : UnitBase
    {
        private const float moveUpdateDelay = 3000; //3 seconds
        private readonly Stopwatch passedGatherTime;

        //The worker should constantly move towards it's target, but not flood the client
        private readonly Stopwatch updatedMovePositionTimer;
        public ushort GatherResourceTime; //How long it takes to gather a resource in milliseconds
        public ResourceTypes heldResource;

        private ResourceTypes lastResourceHeld;
        public byte resourceCount;


        public Worker(GameServer server, Player mPlayer) : base(server, mPlayer)
        {
            lastResourceHeld = ResourceTypes.Tree;

            GatherResourceTime = 1000;
            passedGatherTime = new Stopwatch();
            passedGatherTime.Reset();
            passedGatherTime.Stop();

            EntityType = Entity.EntityType.Worker;
            Speed = .2f;
            Range = 60;
            Health = 50;
            MaxHealth = 50;

            updatedMovePositionTimer = new Stopwatch();
            updatedMovePositionTimer.Start();
        }

        public bool IsHoldingResources
        {
            get { return (resourceCount > 0); }
        }

        public void AddBuildingToBuild(string type, float x, float y)
        {
            Move(x, y, Entity.RallyPoint.RallyTypes.Build, false, true, type, false);
        }

        protected TYPE GetClosest<TYPE>(Entity.EntityType eType, ResourceTypes rType = ResourceTypes.Apple)
            where TYPE : EntityBase
        {
            float shortest = 0;
            TYPE ret = null;

            foreach (EntityBase entity in MyGameMode.WorldEntities.Values)
            {
                if (entity.EntityType != eType)
                    continue;
                if (entity.Neutral == false)
                {
                    if (entity.Team != Team)
                        continue;
                }
                if (entity is Resources && (((Resources) entity).ResourceType != rType || entity.UseCount > 3)) continue;

                float distance = Math.Abs(Position.X - entity.Position.X) + Math.Abs(Position.Y - entity.Position.Y);
                if (ret == null || distance < shortest)
                {
                    shortest = distance;
                    ret = (TYPE) entity;
                }
            }
            return ret;
        }

        public void GiveResource(ResourceTypes type, byte amount)
        {
            if (!IsHoldingResources)
            {
                heldResource = type;
                resourceCount = amount;
            }
        }


        protected  EntityBase OnPlaceBuilding(byte type, float x, float y)
        {
            switch (type)
            {
                case (byte) WorkerSpellIds.BuildSupplyBuilding: //Supply building
                    return new SupplyBuilding(Server, MyPlayer, 12);
                    break;
                case (byte) WorkerSpellIds.BuildHomeBase: //Home base
                    return new HomeBuilding(Server, MyPlayer);
                    break;
                case (byte) WorkerSpellIds.BuildGlueFactory: //Glue Factory
                    return new GlueFactory(Server, MyPlayer);
                    break;
            }
            return null;
        }

        protected EntityBase OnPlaceBuilding(string type, float x, float y)
        {
            return BuildingBase.CreateBuilding(type, Server, MyPlayer);
        }

        public override void OnPlayerCustomMove()
        {
            base.OnPlayerCustomMove();
            EntityToUse = null;
        }

        protected override void OnRallyPointCompleted(Entity.RallyPoint rally)
        {
            base.OnRallyPointCompleted(rally);
            if (rally.RallyType == Entity.RallyPoint.RallyTypes.Build)
            {
                if (spells.ContainsKey(rally.BuildType))
                {
                    if (spells[rally.BuildType].AppleCost <= MyPlayer.Apples &&
                        spells[rally.BuildType].GlueCost <= MyPlayer.Glue &&
                        spells[rally.BuildType].WoodCost <= MyPlayer.Wood)
                    {
                        MyPlayer.Apples -= spells[rally.BuildType].AppleCost;
                        MyPlayer.Glue -= spells[rally.BuildType].GlueCost;
                        MyPlayer.Wood -= spells[rally.BuildType].WoodCost;
                    }
                    else
                    {
                        return;
                    }
                }
                EntityBase ent = null;
                if(rally.BuildType.Length > 0)
                {
                    ent = OnPlaceBuilding(rally.BuildType, rally.X, rally.Y);
                }
                else
                {
                    ent = OnPlaceBuilding(rally.BuildType, rally.X, rally.Y);
                }
                ent.Position = new Vector2f(rally.X, rally.Y);
                ent.Team = Team;
                MyGameMode.AddEntity(ent);
                MyGameMode.UpdatePlayer(MyPlayer);
            }
        }

        protected override byte[] SetEntityToUseResponse(EntityBase toUse)
        {
            rallyPoints.Clear();
            State = UnitState.Standard;
            moveToUsedEntity(toUse);
            return base.SetEntityToUseResponse(toUse);
        }

        private void StartGatheringResources()
        {
            passedGatherTime.Restart();
            SendData(new byte[1] {(byte) UnitSignature.GrabbingResources}, Entity.Signature.Custom);
        }

        private void StopGatheringResources()
        {
            passedGatherTime.Reset();
            passedGatherTime.Stop();
        }

        public override void Update(float ms)
        {
            base.Update(ms);

            if (EntityToUse != null)
            {
                State = UnitState.Standard;
                const float USEBOUNDS = 50;
                var useBounds = new FloatRect(Position.X - (USEBOUNDS / 2), Position.Y - (USEBOUNDS / 2), USEBOUNDS, USEBOUNDS);
                if (useBounds.Contains(EntityToUse.Position.X, EntityToUse.Position.Y))
                {
                    if (EntityToUse.EntityType == Entity.EntityType.Resources)
                    {
                        if (passedGatherTime.IsRunning == false && IsHoldingResources == false)
                        {
                            StartGatheringResources();
                        }

                        if (IsHoldingResources == false && passedGatherTime.ElapsedMilliseconds >= GatherResourceTime)
                        {
                            //Grab Resources
                            EntityToUse.Use(this);
                            StopGatheringResources();
                        }
                        else if (IsHoldingResources)
                        {
                            lastResourceHeld = heldResource;
                            //Go to closest base if availible
                            var homeEntity = GetClosest<EntityBase>(Entity.EntityType.HomeBuilding);
                            if (homeEntity != null)
                                SetEntityToUse(homeEntity);
                            else
                            {
                                homeEntity = GetClosest<EntityBase>(Entity.EntityType.ResourceUnloadSpot);
                                if (homeEntity != null)
                                    SetEntityToUse(homeEntity);
                            }
                        }
                    }
                    else if (EntityToUse.EntityType == Entity.EntityType.HomeBuilding ||
                             EntityToUse.EntityType == Entity.EntityType.ResourceUnloadSpot)
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

                            var resourceEntity = GetClosest<EntityBase>(Entity.EntityType.Resources,
                                                                        lastResourceHeld);
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
                        StopGatheringResources();
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
         
        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(base.UpdateData());
            writer.Write((byte) heldResource);
            writer.Write(resourceCount);

            return memory.ToArray();
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
    }
}