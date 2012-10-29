using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using Server.Entities.Buildings;
using Shared;

namespace Server.Entities.Units
{
    class StandardWorker : Worker
    {
        public StandardWorker(GameServer server, Player mPlayer) : base(server, mPlayer)
        {
            spells.Add((byte)WorkerSpellIds.BuildHomeBase, new SpellData(0, BuildHomeBase));
            spells.Add((byte)WorkerSpellIds.BuildSupplyBuilding, new SpellData(0, BuildSupplyBuilding));
        }

        private byte[] BuildSupplyBuilding(float x, float y)
        {
            if (true || MyPlayer.Wood >= BuildingData.SupplyBuildingWoodCost)
            {
                buildingsToBuild.Add(new BuildingListData(new Vector2f(x, y), new SupplyBuilding(Server, MyPlayer, 12)));
                MyPlayer.Wood -= BuildingData.SupplyBuildingWoodCost;

                MyGameMode.UpdatePlayer(MyPlayer);
            }
            return new byte[0];
        }


        public byte[] BuildHomeBase(float x, float y)
        {
            if(true || MyPlayer.Apples >= BuildingData.HomeBaseAppleCost && MyPlayer.Wood >= BuildingData.HomeBaseWoodCost)
            {
                buildingsToBuild.Add(new BuildingListData(new Vector2f(x, y), new HomeBuilding(Server, MyPlayer)));
                MyPlayer.Apples -= BuildingData.HomeBaseAppleCost;
                MyPlayer.Wood -= BuildingData.HomeBaseWoodCost;

                MyGameMode.UpdatePlayer(MyPlayer);
            }
            return new byte[0];
        }
    }
}
