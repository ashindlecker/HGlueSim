using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using System.IO;

namespace Server.Entities.Buildings
{
    class SupplyBuilding : BuildingBase
    {
        public byte SupplyAdd;

        public SupplyBuilding(GameServer server, Player ply, byte sAdd) : base(server, ply)
        {
            SupplyAdd = sAdd;
            EntityType = Entity.EntityType.SupplyBuilding;
        }

        public override void onBuildComplete()
        {
            MyPlayer.Supply += SupplyAdd;
            MyGameMode.UpdatePlayer(MyPlayer);
        }
    }
}
