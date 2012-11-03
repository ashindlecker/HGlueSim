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
            AddBuildingToBuild(0, x, y);
            return new byte[0];
        }


        public byte[] BuildHomeBase(float x, float y)
        {
            AddBuildingToBuild(1, x, y);
            return new byte[0];
        }

        protected override void OnPlaceBuilding(byte type, float x, float y)
        {
            base.OnPlaceBuilding(type, x, y);
            switch(type)
            {
                case 0://Supply building

                    if (true || MyPlayer.Wood >= BuildingData.SupplyBuildingWoodCost)
                    {
                        MyPlayer.Wood -= BuildingData.SupplyBuildingWoodCost;
                        var add = new SupplyBuilding(Server, MyPlayer, 12);
                        add.Position = new Vector2f(x, y);
                        add.Team = Team;
                        MyGameMode.AddEntity(add);
                    }
                    break;
                case 1: //Home base
                    if (true || MyPlayer.Apples >= BuildingData.HomeBaseAppleCost && MyPlayer.Wood >= BuildingData.HomeBaseWoodCost)
                    {
                        MyPlayer.Apples -= BuildingData.HomeBaseAppleCost;
                        MyPlayer.Wood -= BuildingData.HomeBaseWoodCost;
                        var add = new HomeBuilding(Server, MyPlayer);
                        add.Position = new Vector2f(x, y);
                        add.Team = Team;
                        MyGameMode.AddEntity(add);
                    }
                    break;
            }

            MyGameMode.UpdatePlayer(MyPlayer);
        }
    }
}
