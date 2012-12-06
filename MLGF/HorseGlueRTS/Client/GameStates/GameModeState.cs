using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    class GameModeState : GameStateBase
    {
        private GameClient myclient;

        public GameModeState(GameModes.GameModeBase game, GameClient client)
        {
            myclient = client;
            myclient.GameMode = game;
        }

        public override void End()
        {
        }

        public override void Init(object loadData)
        {
        }

        public override void Render(RenderTarget target)
        {
            myclient.GameMode.Render(target);
        }

        public override void Update(float ts)
        {
            myclient.Update(ts);
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
            myclient.GameMode.KeyPress(keyEvent);
        }

        public override void KeyRelease(KeyEventArgs keyEvent)
        {
            myclient.GameMode.KeyRelease(keyEvent);
        }

        public override void MouseClick(Mouse.Button button, int x, int y)
        {
            myclient.GameMode.MouseClick(button, x, y);
        }

        public override void MouseMoved(int x, int y)
        {
            myclient.GameMode.MouseMoved(x, y);
        }

        public override void MouseRelease(Mouse.Button button, int x, int y)
        {
            myclient.GameMode.MouseRelease(button, x, y);
        }
    }
}
