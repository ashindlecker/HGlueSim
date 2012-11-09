using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.GameModes;
using SFML.Graphics;
using SFML.Window;
using System.Diagnostics;

namespace Client
{
    class Program
    {
        static GameClient client = new GameClient();
        public static RenderWindow window = new RenderWindow(new VideoMode(1200, 720), "Game");

        static void Main(string[] args)
        {

            window.Closed += WindowOnClosed;
            window.MouseMoved += WindowOnMouseMoved;
            window.MouseButtonPressed += WindowOnMouseButtonPressed;
            window.KeyPressed += WindowOnKeyPressed;
            window.KeyReleased += WindowOnKeyReleased;
            window.MouseButtonReleased += WindowOnMouseButtonReleased;
            window.SetFramerateLimit(60);

            client.GameMode = new StandardMelee(client.InputHandler);

            Console.WriteLine("Server IP (ip only no port): ");

            client.Connect(Console.ReadLine(), 5555);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            while(window.IsOpen())
            {

                window.DispatchEvents();
                window.Clear(new Color(100, 100, 200));

                var dt = (float)(stopwatch.Elapsed.TotalSeconds * 1000);
                stopwatch.Restart();
                client.Update(dt);
                client.GameMode.Render(window);
                window.Display();
                
            }
        }

        private static void WindowOnMouseButtonReleased(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            client.GameMode.MouseRelease(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnKeyReleased(object sender, KeyEventArgs keyEventArgs)
        {
            client.GameMode.KeyRelease(keyEventArgs);
        }

        private static void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            client.GameMode.KeyPress(keyEventArgs);
        }

        private static void WindowOnMouseButtonPressed(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            client.GameMode.MouseClick(mouseButtonEventArgs.Button, mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
        }

        private static void WindowOnMouseMoved(object sender, MouseMoveEventArgs mouseMoveEventArgs)
        {
            client.GameMode.MouseMoved(mouseMoveEventArgs.X, mouseMoveEventArgs.Y);
        }


        private static void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            ((Window)sender).Close();
        }
    }
}
