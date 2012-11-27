using SFML.Graphics;

namespace Client.GameStates
{
    internal abstract class GameStateBase
    {
        public GameStateManager MyManager;
        public abstract void End();
        public abstract void Init(object loadData);
        public abstract void Render(RenderTarget target);
        public abstract void Update(float ts);
    }
}