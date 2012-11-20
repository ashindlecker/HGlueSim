using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class Gamemode
    {
        public enum Signature : byte
        {
            Custom,
            PlayerData,
            Entity,
            MapLoad,
            TiledMapLoad,
            EntityLoad,
            Handshake,
            EntityAdd,
            PlayersLoad,
            RemoveEntity,
            PlayerLeft,
            GameEnded,
            GroupMovement,
        }
    }
}
