using Server.Entities.Buildings;
using Shared;

namespace Server.Entities.Units
{
    internal class StandardWorker : Worker
    {
        public StandardWorker(GameServer server, Player mPlayer) : base(server, mPlayer)
        {
            spells.Add((byte) WorkerSpellIds.BuildHomeBase, new SpellData(0, BuildHomeBase));
            spells.Add((byte) WorkerSpellIds.BuildSupplyBuilding, new SpellData(0, BuildSupplyBuilding));
            spells.Add((byte) WorkerSpellIds.BuildGlueFactory, new SpellData(0, BuildGlueFactory));

            Health = 50;
            MaxHealth = 50;
        }

        public byte[] BuildGlueFactory(float x, float y)
        {
            AddBuildingToBuild((byte) WorkerSpellIds.BuildGlueFactory, x, y);
            return new byte[0];
        }

        public byte[] BuildHomeBase(float x, float y)
        {
            AddBuildingToBuild((byte) WorkerSpellIds.BuildHomeBase, x, y);
            return new byte[0];
        }

        private byte[] BuildSupplyBuilding(float x, float y)
        {
            AddBuildingToBuild((byte) WorkerSpellIds.BuildSupplyBuilding, x, y);
            return new byte[0];
        }

    }
}