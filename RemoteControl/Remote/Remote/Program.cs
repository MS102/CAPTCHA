using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Remote
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Remote";
            Client client = new Client();

            Console.WriteLine("Введите IP-адрес:");
            string ip = Console.ReadLine();
            IPAddress tryIp;
            while (!IPAddress.TryParse(ip, out tryIp))
            {
                Console.WriteLine("Введите IP-адрес:");
                ip = Console.ReadLine();
            }

            Console.WriteLine("Введите логин:");
            string login = Console.ReadLine();
            while (login.Length == 0)
            {
                Console.WriteLine("Введите логин:");
                login = Console.ReadLine();
            }
            Console.WriteLine("Введите пароль:");
            string password = Console.ReadLine();

            client.Connect(ip, 7000, login, password);

            while (!client.successful)
                Thread.Sleep(250);
            while (!client.authenticated)
                Thread.Sleep(250);

            Console.WriteLine("Введите команду:");
            client.Send(Console.ReadLine());
            while (true);
        }
    }
}
