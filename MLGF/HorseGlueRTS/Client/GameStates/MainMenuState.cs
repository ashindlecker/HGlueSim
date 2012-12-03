using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;

namespace Client.GameStates
{
    internal class MainMenuState : GameStateBase
    {
        public string PlayerName;

        public enum OptionTypes
        {
            FindGame = 0,
            ViewProfile = 1,
            QuitGame = 2,
        }

        private List<Sprite[]> options;


        private int selectedOption;
        private const int MAXOPTIONS = 3;

        

        public MainMenuState()
        {
            PlayerName = "NO NAME";
            selectedOption = 0;

            options = new List<Sprite[]>();
            for(var i = 0; i < MAXOPTIONS; i++)
            {
                options.Add(new Sprite[2]);
            }

            //TODO: THIS IS YOUR DEPARTMENT BEN ROFL

            options[(int)OptionTypes.FindGame] = new Sprite[2]
                           {
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/FindGameButton.png")),
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/FindGameButton2.png"))
                           };
            options[(int)OptionTypes.ViewProfile] = new Sprite[2]
                           {
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/ViewProfileButton.png")),
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/ViewProfileButton2.png"))
                           };
            options[(int)OptionTypes.QuitGame] = new Sprite[2]
                           {
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/QuitGameButton.png")),
                               new Sprite(ExternalResources.GTexture("Resources/Sprites/MainMenu/QuitGameButton2.png"))
                           };

            const float BUTTONSPACING = 100f;

            for(var i =0 ; i < options.Count; i++)
            {
                for(var s = 0; s < 2; s++)
                {
                    options[i][s].Position = new Vector2f(Program.window.Size.X/1.5f,
                                                          ((float)Program.window.Size.Y/5) + i*BUTTONSPACING);
                }
            }
        }

        public override void End()
        {
        }

        public override void Init(object loadData)
        {
            selectedOption = 0;
        }

        public override void Render(RenderTarget target)
        {
            var renderName = new Text(PlayerName);
            renderName.Position = target.ConvertCoords(new Vector2i(10, 10));
            renderName.Scale = new Vector2f(.6f, .6f);
            target.Draw(renderName);

            var gameName = new Text("MY LITTLE GLUE FACTORY");
            gameName.Scale = new Vector2f(1, 1);
            gameName.Origin = new Vector2f(gameName.GetGlobalBounds().Width/2, gameName.GetGlobalBounds().Height/2);
            gameName.Position = target.ConvertCoords(new Vector2i((int) target.Size.X/2, 10));
            target.Draw(gameName);

            for(var i = 0; i < options.Count; i++)
            {
                if(selectedOption == i)
                {
                    target.Draw(options[i][1]);
                }
                else
                {
                    target.Draw(options[i][0]);
                }
            }
        }

        public override void Update(float ts)
        {
        }

        public override void KeyPress(KeyEventArgs keyEvent)
        {
            if(keyEvent.Code == Keyboard.Key.Down)
            {
                selectedOption++;
                if(selectedOption >= MAXOPTIONS)
                {
                    selectedOption = 0;
                }
            }
            if (keyEvent.Code == Keyboard.Key.Up)
            {
                selectedOption--;
                if (selectedOption < 0)
                {
                    selectedOption = MAXOPTIONS - 1;
                }
            }

            if(keyEvent.Code == Keyboard.Key.Return)
            {
                selectoption();
            }
        }

        public override void KeyRelease(KeyEventArgs keyEvent)
        {
        }

        public override void MouseClick(Mouse.Button button, int x, int y)
        {
            if(button != Mouse.Button.Left) return;

            for (var i = 0; i < options.Count; i++)
            {
                var bounds = options[i][0].GetGlobalBounds();
                bounds.Left = options[i][0].Position.X;
                bounds.Top = options[i][0].Position.Y;

                if (bounds.Contains(x, y))
                {
                    selectedOption = i;
                    selectoption();
                }
            }
        }

        public override void MouseMoved(int x, int y)
        {
            for(var i = 0; i < options.Count; i++)
            {
                var bounds = options[i][0].GetGlobalBounds();
                bounds.Left = options[i][0].Position.X;
                bounds.Top = options[i][0].Position.Y;

                if(bounds.Contains(x,y))
                {
                    selectedOption = i;
                }
            }
        }

        public override void MouseRelease(Mouse.Button button, int x, int y)
        {
        }


        private void selectoption()
        {
            //TODO: do functions based on id

            switch ((OptionTypes)selectedOption)
            {
                case OptionTypes.FindGame:
                    {
                        
                    }
                    break;
                case OptionTypes.ViewProfile:

                    break;
                case OptionTypes.QuitGame:
                    Program.window.Close();
                    break;
                default:
                    break;
            }
        }
    }
}