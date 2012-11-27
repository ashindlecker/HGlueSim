using Client.GameModes;
using SFML.Graphics;

namespace Client.Effects
{
    internal abstract class EffectBase
    {
        public GameModeBase MyGamemode;
        public abstract void Render(RenderTarget target);
        public abstract void Update(float ms);
    }
}