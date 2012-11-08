using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server.Entities.Buildings
{
    class GlueFactory:BuildingBase
    {
        private Player myPlayer;

        public GlueFactory(GameServer server, Player player) : base(server, player)
        {
            EntityType = Entity.EntityType.GlueFactory;
        }

        protected override byte[] UseResponse(EntityBase user)
        {
            if (user.EntityType == Entity.EntityType.Worker)
            {
                var userHealth = user.Health;
                user.TakeDamage(userHealth, Entity.DamageElement.Normal, false);

                myPlayer.Glue += (ushort)userHealth;
                MyGameMode.UpdatePlayer(myPlayer);
            }
            return base.UseResponse(user);
        }
    }
}
