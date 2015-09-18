using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "RemoteServer";
            Server server = new Server(7000);
            Console.WriteLine("Сервер запущен.");
            while (true);
        }
    }
}
