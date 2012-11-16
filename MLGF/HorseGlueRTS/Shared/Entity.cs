using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Shared
{
    public class Entity
    {
        public enum EntityType : byte
        {
            Unit,
            Building,
            Worker,
            Resources,
            HomeBuilding,
            SupplyBuilding,
            GlueFactory,
            ResourceUnloadSpot,
        }

        public enum DamageElement : byte
        {
            Normal,
            Fire,
            Water,
            Electric,
            Acid,
        }

        public enum Signature : byte
        {
            Damage,
            Move,
            Custom,
            Update,
            Spell,
            Use,
            EntityToUseChange,
        }

        public class RallyPoint
        {
            public float X, Y;
            public enum RallyTypes
            {
                StandardMove,
                AttackMove,
                Build,
            }

            //Only used if rally type is build
            public byte BuildType;
            public RallyTypes RallyType;

            public RallyPoint()
            {
                X = 0;
                Y = 0;
                RallyType = RallyTypes.StandardMove;
                BuildType = 0;
            }

            public void Load(MemoryStream memory)
            {
                var reader = new BinaryReader(memory);
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
                RallyType = (RallyTypes) reader.ReadByte();

                if(RallyType == RallyTypes.Build)
                {
                    BuildType = reader.ReadByte();
                }
            }

            public byte[] ToBytes()
            {

                var memory = new MemoryStream();
                var writer = new BinaryWriter(memory);

                writer.Write(X);
                writer.Write(Y);
                writer.Write((byte) RallyType);
                if(RallyType == RallyTypes.Build)
                {
                    writer.Write(BuildType);
                }


                return memory.ToArray();
            }
        }
    }
}
