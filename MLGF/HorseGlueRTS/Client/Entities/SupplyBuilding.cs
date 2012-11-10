using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Entities
{
    class SupplyBuilding : BuildingBase
    {
        public SupplyBuilding()
        {
        }


        public override void SetTeam(byte team)
        {
            base.SetTeam(team);
            Sprites[AnimationTypes.Standard].Sprites.AddRange(
                ExternalResources.GetSprites("Resources/Sprites/SupplyBuilding/" + team.ToString() + "/" + "Standard/"));
            SetSprites();
        }
    }
}
