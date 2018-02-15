using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketServer
{
    class Program
    {
        private static int _Port;
        private static HttpListener _Server;
        private static string _Loopback = "127.0.0.1";
        private static string _OurIp = FindHostIp();
        private static readonly List<ActiveClient> _Clients = new List<ActiveClient>();

        private static BackgroundWorker _Bouncer;

        private static string FindHostIp()
        {
            string response = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    response = ip.ToString();
                    continue;
                }
            }
            return response ?? throw new InvalidOperationException("Unable to start server. You do not seem to be connected to the internet.");
        }

        private static void Init()
        {
            Random rnd = new Random(_Clients.GetHashCode() * DateTime.Now.Second * DateTime.Now.Hour);
            _Port = rnd.Next(12340, 12350);
            _Server = new HttpListener();
            //_Server.Prefixes.Add($"http://{ IPAddress.Parse(_OurIp) }:{ _Port }/");
            _Server.Prefixes.Add($"http://localhost:{ _Port }/");
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

                //Not sure how to initiate a WebSocket request yet through c#.
                //To connect in to the client open up chrome, hit F12 and 
                //type the following into the javascript console.
                /*
                 * var socket = new WebSocket("ws://localhost:port/);
                 * 
                 * socket.onopen = function(event){
                 *       console.log('Connection established');
                 *  };
                 * 
                 * socket.onmessage = function(event){
                 *     console.log(event.data);
                 * };
                 * 
                 */

            }
        }
        
        private static async void WelcomeGuests(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                HttpListenerContext context = await _Server.GetContextAsync();
                if(context.Request.IsWebSocketRequest)
                {

                    WebSocketContext wsContext = await context.AcceptWebSocketAsync(null);

                    //We have the socket, we can now read and write to it.
                    _Clients.Add(new ActiveClient(wsContext));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Client connected... from {0}", context.Request.RemoteEndPoint);
                    Console.ResetColor();
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8, 8192, true))
                    {
                        writer.Write("We only service WebSocket requests.");
                        writer.Close();
                    }
                    context.Response.Close();
                }
            }
        }

        private static void SanitizeClients()
        {
            foreach (ActiveClient leaving in _Clients.Where(x => x.Socket.CloseStatus == null))
            {
                leaving.Dispose();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Client disconnected...");
                Console.ResetColor();
            }
        }
    }
}
