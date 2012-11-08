using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Entities
{
    class GlueFactory : BuildingBase
    {

        public GlueFactory()
        {
            SetSprites(); ;
        }

        public override void Use(EntityBase user)
        {
            base.Use(user);
            //TODO: Blood/Glue effect
        }
    }
}
