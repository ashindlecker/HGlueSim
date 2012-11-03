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
            Sprites[AnimationTypes.Standard].Sprites.AddRange(
                ExternalResources.GetSprites("Resources/Sprites/SupplyBuilding/Standard/"));
            SetSprites();
        }
    }
}
