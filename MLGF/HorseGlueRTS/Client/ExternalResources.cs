using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using System.IO;
using SFML.Audio;

namespace Client
{
    class ExternalResources
    {
        private static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        private static Dictionary<string, SoundBuffer> sounds = new Dictionary<string, SoundBuffer>();

        public static SoundBuffer GSoundBuffer(string file)
        {
            if (sounds.ContainsKey(file) == false)
            {
                sounds.Add(file, new SoundBuffer(file));
            }
            return sounds[file];
        }

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
            if (Directory.Exists(directory) == false) return null;
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

        public enum DeathSounds
        {
            CliffDeath = 0,
        }

        public enum ResourceSounds
        {
            CliffMining = 0,
        }

        public enum UseSounds
        {
            CliffUsing = 0,
        }

        public enum AttackSounds
        {
            CliffGetFucked = 0,
        }
    }
}
