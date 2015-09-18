using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Principal;

namespace Remote
{
    class Client
    {
        private Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Socket socket;
        private NetworkStream nws;
        private NegotiateStream ns;
        private NetworkCredential netCred;
        private byte[] buffer = new byte[8192];
        private string ip;
        private int port;
        private string login;
        private string password;
        public bool successful = false;
        public bool authenticated = false;
        private bool interactiveMode = false;

        public void Connect(string ip, int port, string login, string password)
        {
            this.ip = ip;
            this.port = port;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            this.login = login;
            this.password = password;
            client.BeginConnect(endPoint, ConnectCallback, client);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                nws = new NetworkStream(socket);
                ns = new NegotiateStream(nws, true);
                netCred = new NetworkCredential(login, password);
                ns.BeginAuthenticateAsClient(netCred, String.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Delegation, AuthCallback, ns);
                successful = true;
            }
            catch (Exception e)
            {
                successful = false;
                Console.WriteLine(e.Message);

                Console.WriteLine("Введите IP-адрес:");
                ip = Console.ReadLine();
                Console.WriteLine("Введите логин:");
                login = Console.ReadLine();
                while (login.Length == 0)
                {
                    Console.WriteLine("Введите логин:");
                    login = Console.ReadLine();
                }
                Console.WriteLine("Введите пароль:");
                password = Console.ReadLine();

                IPAddress tryIp;
                while (!IPAddress.TryParse(ip, out tryIp))
                {
                    Console.WriteLine("Введите IP-адрес:");
                    ip = Console.ReadLine();
                    Console.WriteLine("Введите логин:");
                    login = Console.ReadLine();
                    while (login.Length == 0)
                    {
                        Console.WriteLine("Введите логин:");
                        login = Console.ReadLine();
                    }
                    Console.WriteLine("Введите пароль:");
                    password = Console.ReadLine();
                }

                Connect(ip, port, login, password);
            }
        }

        private void AuthCallback(IAsyncResult ar)
        {
            try
            {
                NegotiateStream nsCb = (NegotiateStream)ar.AsyncState;
                nsCb.EndAuthenticateAsClient(ar);
                nsCb.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, nsCb);
                authenticated = true;
            }
            catch (Exception e)
            {
                authenticated = false;
                Console.WriteLine(e.Message);

                Console.WriteLine("Введите логин:");
                login = Console.ReadLine();
                while (login.Length == 0)
                {
                    Console.WriteLine("Введите логин:");
                    login = Console.ReadLine();
                }
                Console.WriteLine("Введите пароль:");
                password = Console.ReadLine();
                nws = new NetworkStream(socket);
                ns = new NegotiateStream(nws, true);
                netCred = new NetworkCredential(login, password);
                ns.BeginAuthenticateAsClient(netCred, String.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Delegation, AuthCallback, ns);
            }
        }     

        public void Send(string command)
        {
            command = command.Trim();
            string[] commandArgs = command.Split(' ');
            commandArgs[0] = commandArgs[0].ToLower();

            bool list = false;

            if (command == "commands")
            {
                list = true;
                Console.WriteLine("Поддерживаемые команды: ipconfig, ping, nslookup, dir, where");
            }

            if ((commandArgs[0] != "ipconfig") && (commandArgs[0] != "ping") && (commandArgs[0] != "nslookup")
                && (commandArgs[0] != "dir") && (commandArgs[0] != "where") && (!interactiveMode) && (!list))
            {
                Console.WriteLine("Неподдерживаемая команда.\n");
                Console.WriteLine("Введите команду:");
                Send(Console.ReadLine());
            }
            else
            {
                if (!list)
                {
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(command);
                        ns.BeginWrite(buffer, 0, buffer.Length, SendCallback, ns);

                        if ((command == "exit") && (interactiveMode))
                        {
                            interactiveMode = false;
                            Console.WriteLine("Введите команду:");
                            Send(Console.ReadLine());
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Введите команду:");
                    Send(Console.ReadLine());
                }
            }

            if ((commandArgs[0] == "nslookup") && (commandArgs.Length == 1))
                interactiveMode = true;
        }

        private void SendCallback(IAsyncResult ar)
        {
            NegotiateStream nsCb = (NegotiateStream)ar.AsyncState;
            nsCb.EndWrite(ar);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            NegotiateStream nsCb = (NegotiateStream)ar.AsyncState;
            int received;

            try
            {
                received = nsCb.EndRead(ar);
            }
            catch (Exception)
            {
                nsCb.Close();
                return;
            }

            byte[] dataByte = new byte[received];
            Array.Copy(buffer, dataByte, received);

            string answer = Encoding.UTF8.GetString(dataByte);

            Console.WriteLine(answer);

            nsCb.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, nsCb);
            Console.WriteLine("Введите команду:");
            Send(Console.ReadLine());
        }
    }
}
