using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Principal;
using System.Security.Authentication;
using System.IO;
using System.Diagnostics;

namespace RemoteServer
{
    class Server
    {
        private Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] buffer = new byte[8192];
        private NetworkStream nws;
        private NegotiateStream ns;
        private bool interactiveMode = false;

        public Server(int port)
        {
            server.Bind(new IPEndPoint(IPAddress.Any, port));
            server.Listen(1);
            server.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket acceptSocket;
                acceptSocket = server.EndAccept(ar);
                nws = new NetworkStream(acceptSocket);
                ns = new NegotiateStream(nws);
                ns.BeginAuthenticateAsServer(AuthCallback, ns);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void AuthCallback(IAsyncResult ar)
        {
            try
            {
                NegotiateStream nsCb = (NegotiateStream)ar.AsyncState;
                nsCb.EndAuthenticateAsServer(ar);

                WindowsIdentity remoteIdentity = (WindowsIdentity)ns.RemoteIdentity;
                Console.WriteLine("Пользователь: {0}", remoteIdentity.Name);
                Console.WriteLine("Аутентификация: {0}", remoteIdentity.IsAuthenticated);

                nsCb.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, nsCb);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                try
                {
                    ns.BeginAuthenticateAsServer(AuthCallback, ns);
                }
                catch (Exception)
                {
                }
            }
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

            string command = Encoding.UTF8.GetString(dataByte);
            Console.WriteLine(command);

            if (command == "exit")
                interactiveMode = false;

            ProcessStartInfo cmdOptions = new ProcessStartInfo("cmd.exe");

            cmdOptions.RedirectStandardOutput = true;
            cmdOptions.RedirectStandardInput = true;
            cmdOptions.RedirectStandardError = true;
            cmdOptions.UseShellExecute = false;

            cmdOptions.CreateNoWindow = true;

            Process cmd = Process.Start(cmdOptions);

            if (interactiveMode)
                cmd.StandardInput.WriteLine("nslookup");

            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Close();

            StreamReader srIncoming = cmd.StandardOutput;
            string answer = srIncoming.ReadToEnd();
            srIncoming.Close();

            if (answer.IndexOf(command) != -1)
                answer = answer.Remove(0, answer.IndexOf(command) + command.Length + 2);

            for (int i = 0; i < 2; i++)
                if (answer.LastIndexOf('\n') != -1)
                    answer = answer.Remove(answer.LastIndexOf('\n'));

            StreamReader srIncomingError = cmd.StandardError;
            string error = srIncomingError.ReadToEnd();
            srIncomingError.Close();

            if (interactiveMode)
                if (answer.IndexOf('>') != -1)
                    answer = answer.Remove(0, answer.LastIndexOf('>') + 1);

            string[] commandArgs = command.Split(' ');

            if ((commandArgs[0] == "nslookup") && (commandArgs.Length == 1))
                interactiveMode = true;

            byte[] answerByte = Encoding.UTF8.GetBytes(answer);

            if (!String.IsNullOrEmpty(error))
                answerByte = Encoding.UTF8.GetBytes(error + '\n' + answer);

            nsCb.BeginWrite(answerByte, 0, answerByte.Length, SendCallback, nsCb);

            cmd.WaitForExit();

            nsCb.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, nsCb);
        }

        private void SendCallback(IAsyncResult ar)
        {
            NegotiateStream nsCb = (NegotiateStream)ar.AsyncState;
            nsCb.EndWrite(ar);
        }
    }
}