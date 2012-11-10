using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Entities
{
    class HomeBuilding : BuildingBase
    {
        public HomeBuilding()
        {
            BuildTime = 2000;

            SetSprites();
        }

        public override void SetTeam(byte team)
        {
            base.SetTeam(team);

            var standardSprites = ExternalResources.GetSprites("Resources/Sprites/HomeBase/" + team.ToString() + "/" + "Standard/");
            Sprites[AnimationTypes.Standard].Sprites.AddRange(standardSprites);

            var producingSprites = ExternalResources.GetSprites("Resources/Sprites/HomeBase/" + team.ToString() + "/" + "Producing/");
            Sprites[AnimationTypes.Producing].Sprites.AddRange(producingSprites);

            SetSprites();
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            if(user is Worker)
            {
                var workerCast = (Worker) user;
                workerCast.ResourceCount = 0;
            }
        }


    }
}
