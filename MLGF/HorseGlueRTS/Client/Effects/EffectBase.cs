using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace Client.Effects
{
    abstract class EffectBase
    {
        public GameModes.GameModeBase MyGamemode;
        public abstract void Update(float ms);
        public abstract void Render(RenderTarget target);
    }
}
