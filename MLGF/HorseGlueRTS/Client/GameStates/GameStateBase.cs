using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace Client.GameStates
{
    abstract class GameStateBase
    {
        public GameStateManager MyManager;
        public abstract void Update(float ts);
        public abstract void Render(RenderTarget target);

        //Some game states could start after a match. (Returning back to main screen after a match), and would like to know the game results (loss, win, spending, etc).
        public abstract void Init(object loadData);
        public abstract void End();
    }
}
