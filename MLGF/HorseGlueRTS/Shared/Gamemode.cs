namespace Shared
{
    public class Gamemode
    {
        #region Signature enum

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
            SetCamera,
            UpdatePosition,
        }

        #endregion
    }
}