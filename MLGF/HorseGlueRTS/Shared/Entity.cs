using System.IO;

namespace Shared
{
    public class Entity
    {
        #region DamageElement enum

        public enum DamageElement : byte
        {
            Normal,
            Fire,
            Water,
            Electric,
            Acid,
        }

        #endregion

        #region EntityType enum

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
            Projectile,
        }

        #endregion

        #region Signature enum

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

        #endregion

        #region Nested type: RallyPoint

        public class RallyPoint
        {
            #region RallyTypes enum

            public enum RallyTypes
            {
                StandardMove,
                AttackMove,
                Build,
            }

            #endregion

            //Only used if rally type is build
            public string BuildType;
            public RallyTypes RallyType;
            public float X, Y;

            public RallyPoint()
            {
                X = 0;
                Y = 0;
                RallyType = RallyTypes.StandardMove;
                BuildType = "";
            }

            public void Load(MemoryStream memory)
            {
                var reader = new BinaryReader(memory);
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
                RallyType = (RallyTypes) reader.ReadByte();

                if (RallyType == RallyTypes.Build)
                {
                    BuildType = reader.ReadString();
                }
            }

            public byte[] ToBytes()
            {
                var memory = new MemoryStream();
                var writer = new BinaryWriter(memory);

                writer.Write(X);
                writer.Write(Y);
                writer.Write((byte) RallyType);
                if (RallyType == RallyTypes.Build)
                {
                    writer.Write(BuildType);
                }


                return memory.ToArray();
            }
        }

        #endregion
    }
}