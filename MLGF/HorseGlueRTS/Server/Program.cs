using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.GameModes;
using System.Diagnostics;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            GameServer server = new GameServer(5555);
            server.SetGame(new StandardMelee(server, 2));
            server.Start();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();

            while(true)
            {
                float dt = (float)(stopwatch.Elapsed.TotalSeconds * 1000);
                stopwatch.Restart();
                server.Update(dt);
                System.Threading.Thread.Sleep(5);
            }
        }
    }
}
