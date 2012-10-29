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
        private Player player;
        public byte SupplyAdd;

        public SupplyBuilding(GameServer server, Player ply, byte sAdd) : base(server)
        {
            SupplyAdd = sAdd;
            player = ply;
        }

        public override void onBuildComplete()
        {
            player.Supply += SupplyAdd;
            MyGameMode.UpdatePlayer(player);
        }

        public override byte[] UpdateData()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);
            writer.Write(base.UpdateData());

            writer.Write(SupplyAdd);

            return memory.ToArray();
        }
    }
}
