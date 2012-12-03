using Client.GameStates;
using SFML.Graphics;
using SFML.Window;

namespace Client
{
    internal class GameStateManager
    {
        private GameStateBase gameState;

        public GameStateManager()
        {
            gameState = null;
        }

        public void Render(RenderTarget target)
        {
            if (gameState != null)
                gameState.Render(target);
        }

        public void SwitchState(GameStateBase state, object data)
        {
            if (gameState != null)
            {
                gameState.End();
            }
            gameState = state;
            gameState.MyManager = this;
            gameState.Init(data);
        }

        public void Update(float ts)
        {
            if (gameState != null)
                gameState.Update(ts);
        }


        public void SendKeyPress(KeyEventArgs keyEvent)
        {
            if (gameState != null)
                gameState.KeyPress(keyEvent);
        }

        public void SendKeyRelease(KeyEventArgs keyEvent)
        {
            if (gameState != null)
                gameState.KeyRelease(keyEvent);
        }

        public void SendMouseClick(Mouse.Button button, int x, int y)
        {
            if (gameState != null)
                gameState.MouseClick(button, x, y);
        }

        public void SendMouseMoved(int x, int y)
        {
            if (gameState != null)
                gameState.MouseMoved(x,y);
        }

        public void SendMouseRelease(Mouse.Button button, int x, int y)
        {
            if (gameState != null)
                gameState.MouseRelease(button, x, y);
        }
    }
}