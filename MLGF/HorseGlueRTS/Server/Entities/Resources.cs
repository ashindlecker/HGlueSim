using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using System.IO;

namespace Server.Entities
{
    class Resources : EntityBase
    {

        public ushort RemainingResources;
        protected byte resourcesPerTrip;
        public ResourceTypes ResourceType;


        public Resources(GameServer _server) : base(_server)
        {
            EntityType = Entity.EntityType.Resources;
            RemainingResources = 65535;
            resourcesPerTrip = 10;
            ResourceType = ResourceTypes.Glue;
            RemoveOnNoHealth = false;
        }

        protected override byte[] UseResponse(EntityBase user)
        {
            if(user.EntityType == Entity.EntityType.Worker)
            {
                var toGive = resourcesPerTrip;
                if(RemainingResources < resourcesPerTrip)
                {
                    toGive = (byte)RemainingResources;
                }
                RemainingResources -= toGive;

                var workerCast = (Worker) user;
                workerCast.GiveResource(ResourceType, toGive);
            }

            return base.UseResponse(user);
        }


        public override void Update(float ms)
        {

        }

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write((byte) ResourceType);
            writer.Write(RemainingResources);
            writer.Write(resourcesPerTrip);

            return memory.ToArray();
        }

    }
}
