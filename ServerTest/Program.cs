using RainbowMage.ActServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 100;

            var miniparse = new RainbowMage.ActServer.Extensions.MiniParseExtension();

            Server server = new Server(23456, "actserver");
            //server.Extensions.Add(miniparse);
            server.Start();

            while (Console.ReadKey().Key != ConsoleKey.X)
            {

            }

            server.Stop();
        }
    }
}
