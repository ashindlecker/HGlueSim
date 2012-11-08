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
            spells.Add((byte)WorkerSpellIds.BuildGlueFactory, new SpellData(0, BuildGlueFactory));
        }

        private byte[] BuildSupplyBuilding(float x, float y)
        {
            AddBuildingToBuild((byte)WorkerSpellIds.BuildSupplyBuilding, x, y);
            return new byte[0];
        }


        public byte[] BuildHomeBase(float x, float y)
        {
            AddBuildingToBuild((byte)WorkerSpellIds.BuildHomeBase, x, y);
            return new byte[0];
        }

        public byte[] BuildGlueFactory(float x, float y)
        {
            AddBuildingToBuild((byte)WorkerSpellIds.BuildGlueFactory, x, y);
            return new byte[0];
        }

        protected override EntityBase OnPlaceBuilding(byte type, float x, float y)
        {
            base.OnPlaceBuilding(type, x, y);
            switch(type)
            {
                case (byte)WorkerSpellIds.BuildSupplyBuilding://Supply building
                        return new SupplyBuilding(Server, MyPlayer, 12);
                    break;
                case (byte)WorkerSpellIds.BuildHomeBase: //Home base
                        return new HomeBuilding(Server, MyPlayer);
                    break;
                case (byte)WorkerSpellIds.BuildGlueFactory: //Glue Factory
                        return new GlueFactory(Server, MyPlayer);
                    break;
            }
            return null;
        }
    }
}
