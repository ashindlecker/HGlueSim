using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    class SelectSeverState : GameStateBase
    {
        public class ServerInformation
        {
            public enum ServerState
            {
                InProgress,
                LookingForPlayers,

            }

            public string ServerName;
            public string ServerDescription;

            public string IPAddress;
            public ushort Port;

            public int MaxPlayers;
            public int PlayerCount;
        }

        public override void End()
        {
        }

        public override void Init(object loadData)
        {
        }

        public override void Render(RenderTarget target)
        {
        }

        public override void Update(float ts)
        {
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
        }

        public override void KeyRelease(KeyEventArgs keyEvent)
        {
        }

        public override void MouseClick(Mouse.Button button, int x, int y)
        {
        }

        public override void MouseMoved(int x, int y)
        {
        }

        public override void MouseRelease(Mouse.Button button, int x, int y)
        {
        }
    }
}
