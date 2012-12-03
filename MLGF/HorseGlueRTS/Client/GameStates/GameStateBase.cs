using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    internal abstract class GameStateBase
    {
        public GameStateManager MyManager;
        public abstract void End();
        public abstract void Init(object loadData);
        public abstract void Render(RenderTarget target);
        public abstract void Update(float ts);

        public abstract void KeyPress(KeyEventArgs keyEvent);

        public abstract void KeyRelease(KeyEventArgs keyEvent);

        public abstract void MouseClick(Mouse.Button button, int x, int y);

        public abstract void MouseMoved(int x, int y);
        public abstract void MouseRelease(Mouse.Button button, int x, int y);
    }
}