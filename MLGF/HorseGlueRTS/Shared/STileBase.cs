namespace Shared
{
    public class STileBase
    {
        #region TileType enum

        public enum TileType : byte
        {
            Grass,
            Water,
            Stone,
        }

        #endregion

        public bool DynamicSolid;

        public int GridX, GridY;
        public bool Solid;
        public TileType Type;


        //Changes typically from buildings

        public STileBase()
        {
            Type = TileType.Grass;
            GridX = 0;
            GridY = 0;
            Solid = false;
            DynamicSolid = false;
        }
    }
}