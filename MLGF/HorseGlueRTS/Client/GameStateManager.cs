using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace Client
{
    class GameStateManager
    {
        private GameStates.GameStateBase gameState;

        public GameStateManager()
        {
            gameState = null;
        }

        public void SwitchState(GameStates.GameStateBase state, object data)
        {
            if(gameState != null)
            {
                gameState.End();
            }
            gameState = state;
            gameState.MyManager = this;
            gameState.Init(data);
        }

        public void Update(float ts)
        {
            if(gameState != null)
            gameState.Update(ts);
        }

        public void Render(RenderTarget target)
        {
            if(gameState != null)
            gameState.Render(target);
        }
    }
}
