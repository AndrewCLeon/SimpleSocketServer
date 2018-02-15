namespace SimpleSocketServer
{
    using System;
    using System.ComponentModel;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;

    internal class ActiveClient : IDisposable
    {
        internal ActiveClient(WebSocketContext socketContext)
        {
            Context = socketContext;
            Socket = Context.WebSocket;

            WorkerProcess = new BackgroundWorker();
            WorkerProcess.DoWork += new DoWorkEventHandler(this.HandleClient);
            this.WorkerProcess.RunWorkerAsync();
        }

        internal readonly CancellationToken _CancelToken = CancellationToken.None;

        internal WebSocketReceiveResult LastMessage { get; set; }

        private WebSocketContext Context { get; set; }

        internal WebSocket Socket { get; set; }

        internal BackgroundWorker WorkerProcess { get; set; }

        internal int CurrentBufferOffset { get; set; }

        //Create a jagged array for buffered data history.
        internal byte[] BufferedData = new byte[8192];

        internal async void HandleClient(object sender, DoWorkEventArgs args)
        {
            do
            {
                LastMessage = await Socket.ReceiveAsync(new ArraySegment<byte>(BufferedData), System.Threading.CancellationToken.None);

                byte[] tempBuffer = new byte[LastMessage.Count];
                for (int i = 0; i < LastMessage.Count; i++)
                {
                    tempBuffer[i] = BufferedData[i];
                }
                Console.WriteLine(Encoding.UTF8.GetString(tempBuffer));
            }
            while (LastMessage.MessageType != WebSocketMessageType.Close);

            await Socket.CloseAsync(LastMessage.CloseStatus.Value, LastMessage.CloseStatusDescription, _CancelToken);
            Socket.Dispose();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Other end initiated disconnect.");
            Console.ResetColor();
        }

        public void Dispose()
        {
            //Buffered data.
            while (WorkerProcess.IsBusy)
            {
                WorkerProcess.CancelAsync();
            }
            Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None);

            BufferedData = null;

            Socket.Dispose();
            //Client
            //Background worker.
        }
    }
}
