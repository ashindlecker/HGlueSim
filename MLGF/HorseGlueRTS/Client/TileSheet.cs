using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;

namespace Client
{
    class TileSheet
    {
        public static List<Sprite> GrabSprites(Texture texture, Vector2i tileSize, Vector2i spacing)
        {
            var ret = new List<Sprite>();

            for (var y = 0; y < texture.Size.Y; y += tileSize.Y + spacing.Y)
            {
                for (var x = 0; x < texture.Size.X; x += tileSize.X + spacing.X)
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
