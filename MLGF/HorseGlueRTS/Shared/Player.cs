using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Shared
{
    public class Player
    {
        public ushort Glue;
        public ushort Wood;
        public ushort Apples;
        public byte Supply;
        public byte UsedSupply;
        public byte ClientId;
        public byte Team;

        public enum StatusTypes
        {
            InGame,
            Left,
        }

        public StatusTypes Status;

        public int FreeSupply
        {
            get { return Supply - UsedSupply; }
        }

        public Player()
        {
            ClientId = 0;
            Glue = 0;
            Wood = 0;
            Apples = 0;
            Supply = 0;
            UsedSupply = 0;
            Team = 0;
            Status = StatusTypes.InGame;
        }

        public void Load(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            Supply = reader.ReadByte();
            Wood = reader.ReadUInt16();
            Glue = reader.ReadUInt16();
            Apples = reader.ReadUInt16();
            ClientId = reader.ReadByte();
            Team = reader.ReadByte();
            UsedSupply = reader.ReadByte();
            Status = (StatusTypes)reader.ReadByte();
        }

        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Supply);
            writer.Write(Wood);
            writer.Write(Glue);
            writer.Write(Apples);
            writer.Write(ClientId);
            writer.Write(Team);
            writer.Write(UsedSupply);
            writer.Write((byte) Status);
            return memory.ToArray();
        }
    }
}
