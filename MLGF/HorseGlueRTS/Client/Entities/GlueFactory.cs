using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Effects;

namespace Client.Entities
{
    class GlueFactory : BuildingBase
    {

        public GlueFactory()
        {
            SetSprites(); ;
        }

        public override void SetTeam(byte team)
        {
            base.SetTeam(team);

            var standardSprites = ExternalResources.GetSprites("Resources/Sprites/GlueFactory/" + team.ToString() + "/");
            Sprites[AnimationTypes.Standard].Sprites.AddRange(standardSprites);

            var producingSprites = ExternalResources.GetSprites("Resources/Sprites/GlueFactory/" + team.ToString() + "/");
            Sprites[AnimationTypes.Producing].Sprites.AddRange(producingSprites);

            SetSprites();
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            //TODO: Blood/Glue effect
            for(var i = 0; i < 5; i++)
            {
                MyGameMode.AddEffect(new GlueParticle(Position));
            }
        }
    }
}
