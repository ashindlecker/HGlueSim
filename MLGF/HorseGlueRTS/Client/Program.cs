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
        private static readonly GameClient client = new GameClient();

        public static RenderWindow window = new RenderWindow(new VideoMode(1200, 720), "Game", Styles.Default,
                                                             new ContextSettings(32, 32, 10, 0, 100));

        public static Random MRandom;


        private static void Main(string[] args)
        {
            Settings.Init();

            MRandom = new Random();

            window.Closed += WindowOnClosed;
            window.MouseMoved += WindowOnMouseMoved;
            window.MouseButtonPressed += WindowOnMouseButtonPressed;
            window.KeyPressed += WindowOnKeyPressed;
            window.KeyReleased += WindowOnKeyReleased;
            window.MouseButtonReleased += WindowOnMouseButtonReleased;
            window.SetFramerateLimit(75);
            client.GameMode = new StandardMelee(client.InputHandler);

            Console.WriteLine("Server IP (ip only no port): ");
            client.Connect(Console.ReadLine(), 5555);
            //client.Connect("localhost", 5555);

            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            var manager = new GameStateManager();
            manager.SwitchState(new MainMenuState(), null);


            while (window.IsOpen())
            {
                window.DispatchEvents();
                window.Clear(new Color(100, 100, 200));

                var dt = (float) (stopwatch.Elapsed.TotalSeconds*1000);
                stopwatch.Restart();

                client.Update(dt);
                client.GameMode.Render(window);

                //manager.Update(dt);
                //manager.Render(window);
                window.Display();
            }
        }

        private static void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            ((Window) sender).Close();
        }

        private static void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            client.GameMode.KeyPress(keyEventArgs);
        }

        private static void WindowOnKeyReleased(object sender, KeyEventArgs keyEventArgs)
        {
            client.GameMode.KeyRelease(keyEventArgs);
        }

        private static void WindowOnMouseButtonPressed(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            client.GameMode.MouseClick(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnMouseButtonReleased(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            client.GameMode.MouseRelease(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnMouseMoved(object sender, MouseMoveEventArgs mouseMoveEventArgs)
        {
            client.GameMode.MouseMoved(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
        }
    }
}