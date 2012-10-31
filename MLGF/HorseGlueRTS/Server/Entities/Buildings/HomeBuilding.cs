using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server.Entities
{
    class HomeBuilding : BuildingBase
    {

        private Player player;

        public HomeBuilding(GameServer server, Player plr) : base(server)
        {
            BuildTime = 2000;
            EntityType = Entity.EntityType.HomeBuilding;
            player = plr;

            supportedBuilds.Add((byte) UnitBuildIds.Worker);
        }

        protected override bool allowProduction(byte type)
        {
            bool ret = false;
            switch ((UnitBuildIds)type)
            {
                default:
                case UnitBuildIds.Worker: //Worker
                    if (player.Apples >= UnitData.WorkerAppleCost && player.FreeSupply >= UnitData.WorkerSupplyCost)
                    {
                        ret = true;
                        player.Apples -= UnitData.WorkerAppleCost;
                        player.UsedSupply += UnitData.WorkerSupplyCost;
                    }
                    break;
            }
            if(ret)
            {
                MyGameMode.UpdatePlayer(player);
            }
            return ret;
        }

        protected override uint creationTime(byte type)
        {
            switch ((UnitBuildIds)type)
            {
                default:
                case UnitBuildIds.Worker: //Worker
                    return 5000;
                    break;
            }
        }

        protected override BuildingBase.BuildCompleteData onComplete(byte unit)
        {
            switch ((UnitBuildIds)unit)
            {
                default:
                case UnitBuildIds.Worker: //Worker
                    return new BuildCompleteData()
                    {
                        messageData = new byte[0],
                        producedEntity = new Units.StandardWorker(Server, player)
                    };
                    break;
            }
        }

        protected override byte[] UseResponse(EntityBase user)
        {
            if(user.Team == Team && user.EntityType == Entity.EntityType.Worker)
            {
                var workerCast = (Worker) user;

                if (workerCast.IsHoldingResources)
                {
                    switch (workerCast.heldResource)
                    {
                        case ResourceTypes.Tree:
                            player.Wood += workerCast.resourceCount;
                            break;
                        case ResourceTypes.Glue:
                            player.Glue += workerCast.resourceCount;
                            break;
                        case ResourceTypes.Apple:
                            player.Apples += workerCast.resourceCount;
                            break;
                        default:
                            break;
                    }

                    MyGameMode.UpdatePlayer(player);
                    workerCast.resourceCount = 0;
                }
            }
            return base.UseResponse(user);
        }
    }
}
