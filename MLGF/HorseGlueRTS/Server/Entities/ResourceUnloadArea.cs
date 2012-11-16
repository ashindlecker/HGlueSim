using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server.Entities
{
    class ResourceUnloadArea : EntityBase
    {
        public ResourceUnloadArea(GameServer _server, Player player) : base(_server, player)
        {
            RemoveOnNoHealth = false;
            EntityType = Entity.EntityType.ResourceUnloadSpot;
        }

        public override void Update(float ms)
        {
        }

        public override byte[] UpdateData()
        {
            return new byte[0];
        }

        protected override byte[] UseResponse(EntityBase user)
        {
            if (user.Team == Team && user.EntityType == Entity.EntityType.Worker)
            {
                var workerCast = (Worker)user;

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
