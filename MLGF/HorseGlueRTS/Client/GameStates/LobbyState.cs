using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.GameModes;
using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    class LobbyState : GameStateBase
    {
        private GameClient client;

        private Text lobbyNameText;
        private Text lobbyDescriptionText;


        public LobbyState(string ip, int port)
        {
            client = new GameClient();
            client.Connect(ip, port);

            lobbyNameText = new Text();
            lobbyDescriptionText = new Text();
        }

        public override void End()
        {
        }

        public override void Init(object loadData)
        {
        }

        public override void Render(RenderTarget target)
        {
            lobbyNameText.DisplayedString = client.MyLobby.Name;
            lobbyNameText.Origin = new Vector2f(lobbyNameText.GetGlobalBounds().Width/2, 0);
            lobbyNameText.Position = new Vector2f(target.Size.X/2, 10);


            lobbyDescriptionText.DisplayedString = client.MyLobby.Description;
            lobbyDescriptionText.Origin = new Vector2f(lobbyDescriptionText.GetGlobalBounds().Width / 2, 0);
            lobbyDescriptionText.Position = lobbyNameText.Position + new Vector2f(0, 50);
            lobbyDescriptionText.Scale = new Vector2f(.5f, .5f);
            target.Draw(lobbyDescriptionText);
            target.Draw(lobbyNameText);

            const float RECTHEIGHT = 50;
            const float RECTSPACING = 10;
            float STARTDRAWING = target.Size.Y/5;
            var rectangle = new RectangleShape(new Vector2f(500, RECTHEIGHT));
            rectangle.Position = new Vector2f(target.Size.X/5, STARTDRAWING);

            foreach (var player in client.MyLobby.Players.Values)
            {
                if(player.IsReady == false)
                    rectangle.FillColor = new Color(255, 200, 100);
                else
                {
                    rectangle.FillColor = new Color(100, 255, 100);
                }
                target.Draw(rectangle);

                var playerNameDraw = new Text(player.Name);
                if (player.IsHost)
                    playerNameDraw.DisplayedString += "[host]";
                                                      
                playerNameDraw.Position = rectangle.Position + new Vector2f(5, 5);
                target.Draw(playerNameDraw);

                rectangle.Position += new Vector2f(0, RECTHEIGHT + RECTSPACING);

            }

            for(int i = 0; i < client.MyLobby.MaxSlots - client.MyLobby.Players.Count; i++)
            {
                rectangle.FillColor = new Color(100, 100, 100);
                target.Draw(rectangle);
                rectangle.Position += new Vector2f(0, RECTHEIGHT + RECTSPACING);
            }
        }

        public override void Update(float ts)
        {
            client.Update(ts);
            if(client.MyLobby.FLAG_IsSwitchedToGame)
            {
                MyManager.SwitchState(new GameModeState(new StandardMelee(client.InputHandler),client), null);
                client.MyLobby.StartGameSwitchHandShake();
            }
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
            if(keyEvent.Code == Keyboard.Key.Return)
            {
                client.MyLobby.SetReady(true);
            }
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
