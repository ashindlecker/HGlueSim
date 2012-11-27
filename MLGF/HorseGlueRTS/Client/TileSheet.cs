using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;

namespace Client
{
    internal class TileSheet
    {
        public static List<Sprite> GrabSprites(Texture texture, Vector2i tileSize, Vector2i spacing)
        {
            var ret = new List<Sprite>();

            for (int y = 0; y < texture.Size.Y; y += tileSize.Y + spacing.Y)
            {
                for (int x = 0; x < texture.Size.X; x += tileSize.X + spacing.X)
                {
                    var spriteAdd = new Sprite(texture);
                    spriteAdd.TextureRect = new IntRect(x, y, tileSize.X, tileSize.Y);
                    ret.Add(spriteAdd);
                }
            }

            return ret;
        }
    }
}