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
