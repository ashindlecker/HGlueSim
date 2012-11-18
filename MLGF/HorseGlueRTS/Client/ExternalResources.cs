using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using System.IO;

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

        public static Sprite[] GetSprites(string directory)
        {
            List<Sprite> ret = new List<Sprite>();
            string[] files = Directory.GetFiles(directory, "*.png");
           
            for(int i = 0; i < files.Count(); i++)
            {
                ret.Add(new Sprite(GTexture(files[i])));
            } 
            

            files = Directory.GetFiles(directory, "*.bmp");

            for (int i = 0; i < files.Count(); i++)
            {
                ret.Add(new Sprite(GTexture(files[i])));
            }

            return ret.ToArray();
        }
    }
}
