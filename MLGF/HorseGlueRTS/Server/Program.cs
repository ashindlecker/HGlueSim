using System.Diagnostics;
using System.Threading;
using Server.GameModes;

namespace Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var server = new GameServer(5555);
            server.SetGame(new StandardMelee(server, 1));
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