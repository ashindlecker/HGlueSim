using Shared;

namespace Server.Entities.Buildings
{
    internal class SupplyBuilding : BuildingBase
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