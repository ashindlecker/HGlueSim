using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace Client
{
    class ExternalResources
    {
        private static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

        public static Texture GTexture(string file)
        {
            if(textures.ContainsKey(file) == false)
            {
                textures.Add(file, new Texture(file));
            }
            return textures[file];
        }
    }
}
