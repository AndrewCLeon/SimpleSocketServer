using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketServer
{
    class Program
    {
        private static int _Port;
        private static TcpListener _Server;
        private static string _Loopback = "127.0.0.1";
        private static string _OurIp = "192.168.0.2";
        private static readonly List<ActiveClient> _Clients = new List<ActiveClient>();

        private static BackgroundWorker _Bouncer;
        private static void Init()
        {
            Random rnd = new Random(_Clients.GetHashCode() * DateTime.Now.Second * DateTime.Now.Hour);
            _Port = rnd.Next(1, 1000);
            _Server = new TcpListener(IPAddress.Parse(_OurIp), _Port);
            _Server.Start();
            _Bouncer = new BackgroundWorker();
            _Bouncer.DoWork += new DoWorkEventHandler(WelcomeGuests);
            _Bouncer.RunWorkerAsync();
            Console.WriteLine($"Server has started on { _OurIp }:{ _Port }.\r\nWaiting for a connection...");
        }

        static void Main(string[] args)
        {
            Init();
            while (true)
            {
                Console.WriteLine("Please give me an IP");
                string ipAddress = Console.ReadLine();
                Console.WriteLine("Please give me a port");
                string userPort = Console.ReadLine();
                int.TryParse(userPort, out int port);

                TcpClient client = new TcpClient();
                client.BeginConnect(ipAddress, port, new AsyncCallback(ConnectionComplete), client);
            }
        }

        private static void ConnectionComplete(IAsyncResult result)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Connected...");
            Console.ResetColor();
        }

        private static void WelcomeGuests(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                if(_Server.Pending())
                {
                    TcpClient client = _Server.AcceptTcpClient();
                    ActiveClient activeClient = new ActiveClient();
                    activeClient.Client = client;
                    activeClient.WorkerProcess = new BackgroundWorker();
                    activeClient.WorkerProcess.DoWork += new DoWorkEventHandler(activeClient.HandleClient);
                    Console.WriteLine("A client connected.");
                }
                else
                {
                    SanitizeClients();
                }
            }
        }

        private static void SanitizeClients()
        {
            foreach (ActiveClient leaving in _Clients.Where(x => !x.Client.Connected))
            {
                leaving.Client.Close();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Client disconnected...");
                Console.ResetColor();
            }
        }
    }
}
