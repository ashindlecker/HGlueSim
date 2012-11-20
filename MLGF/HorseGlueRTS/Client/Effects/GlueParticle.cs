using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;

namespace Client.Effects
{
    class GlueParticle : EffectBase
    {
        private float fadeSpeed;

        private static Texture[] glueSprites;

        private Sprite mySprite;

        private float alpha;

        private Vector2f Position;
        

        public GlueParticle(Vector2f Pos)
        {
            if(glueSprites == null)
            {
                glueSprites = new Texture[3];
                glueSprites[0] = ExternalResources.GTexture("Resources/Sprites/GlueParticles/0.png");
                glueSprites[1] = ExternalResources.GTexture("Resources/Sprites/GlueParticles/1.png");
                glueSprites[2] = ExternalResources.GTexture("Resources/Sprites/GlueParticles/2.png");
            }

            fadeSpeed = (float)Program.MRandom.NextDouble();
            fadeSpeed += .01f;
            fadeSpeed /= 2;
            mySprite = new Sprite(glueSprites[Program.MRandom.Next(0, glueSprites.Length)]);
            mySprite.Scale = new Vector2f(2, 2);
            mySprite.Rotation = Program.MRandom.Next(0, 360);
            alpha = 255;

            Position = Pos;
            const int SPACING = 50;
            Position.X += Program.MRandom.Next(-SPACING, SPACING);
            Position.Y += Program.MRandom.Next(-SPACING, SPACING);
        }

        public override void Update(float ms)
        {
            alpha -= ms*fadeSpeed;
            if(alpha <= 0)
            {
                MyGamemode.RemoveEffect(this);
            }
        }

        public override void Render(SFML.Graphics.RenderTarget target)
        {
            mySprite.Color = new Color(255,255,255,(byte)alpha);
            mySprite.Position = Position;
            target.Draw(mySprite);
        }
    }
}
