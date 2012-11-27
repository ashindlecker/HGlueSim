using Client.GameStates;
using SFML.Graphics;

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
    }
}