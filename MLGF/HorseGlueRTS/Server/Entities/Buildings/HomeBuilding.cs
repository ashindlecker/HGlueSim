using Server.Entities.Units;
using Shared;

namespace Server.Entities
{
    internal class HomeBuilding : BuildingBase
    {
        public HomeBuilding(GameServer server, Player plr) : base(server, plr)
        {
            BuildTime = 2000;
            EntityType = Entity.EntityType.HomeBuilding;

        }


        protected override byte[] UseResponse(EntityBase user)
        {
            if (user.Team == Team && user.EntityType == Entity.EntityType.Worker)
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