using System;
using System.Diagnostics;
using Client.GameModes;
using Client.GameStates;
using SFML.Graphics;
using SFML.Window;

namespace Client
{
    internal class Program
    {
        private static GameClient client = null;

        public static RenderWindow window = new RenderWindow(new VideoMode(1200, 720), "Game", Styles.Default,
                                                             new ContextSettings(32, 32, 10, 0, 100));

        public static Random MRandom;

        public static GameStateManager manager = new GameStateManager();

        private static void Main(string[] args)
        {
            Settings.Init();
            client = new GameClient();

            MRandom = new Random();

            window.Closed += WindowOnClosed;
            window.MouseMoved += WindowOnMouseMoved;
            window.MouseButtonPressed += WindowOnMouseButtonPressed;
            window.KeyPressed += WindowOnKeyPressed;
            window.KeyReleased += WindowOnKeyReleased;
            window.MouseButtonReleased += WindowOnMouseButtonReleased;
            window.SetFramerateLimit(75);
            client.GameMode = new StandardMelee(client.InputHandler);


            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            manager.SwitchState(new MainMenuState(), null);


            while (window.IsOpen())
            {
                window.DispatchEvents();
                window.Clear(new Color(100, 100, 200));

                var dt = (float) (stopwatch.Elapsed.TotalSeconds*1000);
                stopwatch.Restart();

                manager.Update(dt);
                manager.Render(window);
                window.Display();
            }
        }

        private static void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            ((Window) sender).Close();
        }

        //TODO: eventually the client will be in it's own gamestate where it'll process events there

        private static void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            manager.SendKeyPress(keyEventArgs);   
            //client.GameMode.KeyPress(keyEventArgs);
        }

        private static void WindowOnKeyReleased(object sender, KeyEventArgs keyEventArgs)
        {
            manager.SendKeyRelease(keyEventArgs);
            //client.GameMode.KeyRelease(keyEventArgs);
        }

        private static void WindowOnMouseButtonPressed(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            manager.SendMouseClick(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
            //client.GameMode.MouseClick(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnMouseButtonReleased(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            manager.SendMouseRelease(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
            //client.GameMode.MouseRelease(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnMouseMoved(object sender, MouseMoveEventArgs mouseMoveEventArgs)
        {
            manager.SendMouseMoved(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
            //client.GameMode.MouseMoved(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
        }
    }
}