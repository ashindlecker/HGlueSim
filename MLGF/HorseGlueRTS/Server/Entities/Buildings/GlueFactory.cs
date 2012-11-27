using Shared;

namespace Server.Entities.Buildings
{
    internal class GlueFactory : BuildingBase
    {
        public GlueFactory(GameServer server, Player player) : base(server, player)
        {
            EntityType = Entity.EntityType.GlueFactory;
        }

        protected override byte[] UseResponse(EntityBase user)
        {
            if (user.EntityType == Entity.EntityType.Worker)
            {
                float userHealth = user.Health;
                user.TakeDamage(userHealth, Entity.DamageElement.Normal, false);

                MyPlayer.Glue += (ushort) userHealth;
                MyGameMode.UpdatePlayer(MyPlayer);
            }
            return base.UseResponse(user);
        }
    }
}