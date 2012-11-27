using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFML.Audio;
using SFML.Graphics;

namespace Client
{
    internal class ExternalResources
    {
        #region AttackSounds enum

        public enum AttackSounds
        {
            CliffGetFucked = 0,
        }

        #endregion

        #region DeathSounds enum

        public enum DeathSounds
        {
            CliffDeath = 0,
        }

        #endregion

        #region ResourceSounds enum

        public enum ResourceSounds
        {
            CliffMining = 0,
        }

        #endregion

        #region UseSounds enum

        public enum UseSounds
        {
            CliffUsing = 0,
        }

        #endregion

        private static readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        private static readonly Dictionary<string, SoundBuffer> sounds = new Dictionary<string, SoundBuffer>();

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
            if (textures.ContainsKey(file) == false)
            {
                textures.Add(file, new Texture(file));
            }
            return textures[file];
        }

        public static Sprite[] GetSprites(string directory)
        {
            var ret = new List<Sprite>();
            if (Directory.Exists(directory) == false) return null;
            string[] files = Directory.GetFiles(directory, "*.png");

            for (int i = 0; i < files.Count(); i++)
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