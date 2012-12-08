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
        public string Name;
        public byte Wins;
        public byte Losses;
        public bool HasLoadedGame;


        public LobbyPlayer()
        {
            HasLoadedGame = false;
            Id = 0;
            IsHost = false;
            Team = 0;
            IsReady = false;
            Name = "NOTSET";
            Wins = 0;
            Losses = 0;
        }
        
        public byte[] ToBytes()
        {
            var memory = new MemoryStream();
            var writer = new BinaryWriter(memory);

            writer.Write(Id);
            writer.Write(Team);
            writer.Write(IsHost);
            writer.Write(IsReady);
            writer.Write(Wins);
            writer.Write(Losses);
            writer.Write(Name);
            
            return memory.ToArray();
        }

        public void Load(MemoryStream memory)
        {
            var reader = new BinaryReader(memory);

            Id = reader.ReadByte();
            Team = reader.ReadByte();
            IsHost = reader.ReadBoolean();
            IsReady = reader.ReadBoolean();
            Wins = reader.ReadByte();
            Losses = reader.ReadByte();
            Name = reader.ReadString();
        }
    }
}
