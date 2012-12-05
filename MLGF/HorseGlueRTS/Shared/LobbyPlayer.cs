using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Shared
{
    public class LobbyPlayer
    {
        public byte Team;
        public bool IsHost;
        public byte Id;
        public bool IsReady;

        public LobbyPlayer()
        {
            Id = 0;
            IsHost = false;
            Team = 0;
            IsReady = false;
        }
        
        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Id);
            writer.Write(Team);
            writer.Write(IsHost);
            writer.Write(IsReady);

            return memory.ToArray();
        }

        public void Load(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            Id = reader.ReadByte();
            Team = reader.ReadByte();
            IsHost = reader.ReadBoolean();
            IsReady = reader.ReadBoolean();
        }
    }
}
