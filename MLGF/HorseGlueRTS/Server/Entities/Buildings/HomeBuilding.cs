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

        public HomeBuilding(GameServer server, Player plr) : base(server, plr)
        {
            BuildTime = 2000;
            EntityType = Entity.EntityType.HomeBuilding;

            supportedBuilds.Add(new BuildProduceData()
                                    {
                                        AppleCost = 0,
                                        WoodCost = 0,
                                        GlueCost = 0,
                                        SupplyCost = 0,
                                        CreationTime = 1,
                                    });
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
                        producedEntity = new Units.StandardWorker(Server, MyPlayer)
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
                            MyPlayer.Wood += workerCast.resourceCount;
                            break;
                        case ResourceTypes.Glue:
                            MyPlayer.Glue += workerCast.resourceCount;
                            break;
                        case ResourceTypes.Apple:
                            MyPlayer.Apples += workerCast.resourceCount;
                            break;
                        default:
                            break;
                    }

                    MyGameMode.UpdatePlayer(MyPlayer);
                    workerCast.resourceCount = 0;
                }
            }
            return base.UseResponse(user);
        }
    }
}
