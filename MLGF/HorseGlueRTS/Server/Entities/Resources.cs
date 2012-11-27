using System.IO;
using Shared;

namespace Server.Entities
{
    internal class Resources : EntityBase
    {
        public ushort RemainingResources;
        public ResourceTypes ResourceType;
        protected byte resourcesPerTrip;


        public Resources(GameServer _server, Player player) : base(_server, player)
        {
            Team = 100;
            Neutral = true;
            EntityType = Entity.EntityType.Resources;
            RemainingResources = 65535;
            resourcesPerTrip = 10;
            ResourceType = ResourceTypes.Glue;
            RemoveOnNoHealth = false;
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

        protected override byte[] UseResponse(EntityBase user)
        {
            if (user.EntityType == Entity.EntityType.Worker)
            {
                byte toGive = resourcesPerTrip;
                if (RemainingResources < resourcesPerTrip)
                {
                    toGive = (byte) RemainingResources;
                }
                RemainingResources -= toGive;

                var workerCast = (Worker) user;
                workerCast.GiveResource(ResourceType, toGive);
            }

            return base.UseResponse(user);
        }
    }
}