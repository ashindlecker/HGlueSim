using System;
using System.Diagnostics;
using System.Threading;
using Server.GameModes;
using Shared;
namespace Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Settings.Init("Resources/Data/Buildings.xml", "Resources/Data/Units.xml");

            var server = new GameServer(5555);
            //server.SetGame(new StandardMelee(server, 1));
            server.Start();

            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            while (true)
            {
                var dt = (float) (stopwatch.Elapsed.TotalSeconds*1000);
                stopwatch.Restart();
                server.Update(dt);
                Thread.Sleep(100);
            }
        }
    }
}