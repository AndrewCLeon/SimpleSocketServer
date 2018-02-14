namespace SimpleSocketServer
{
    using System;
    using System.ComponentModel;
    using System.Net.Sockets;
    using System.Text;

    internal class ActiveClient : IDisposable
    {
        internal ActiveClient()
        {

        }

        internal ActiveClient(TcpClient client, BackgroundWorker workerProcess)
        {
            Client = client;
            WorkerProcess = workerProcess;
        }

        private NetworkStream _Stream;
        private NetworkStream Stream
        {
            get
            {
                if (_Stream == null && Client != null)
                {
                    _Stream = Client.GetStream();
                }
                return _Stream;
            }
        }

        internal TcpClient Client { get; set; }

        internal BackgroundWorker WorkerProcess { get; set; }

        internal int CurrentBufferOffset { get; set; }
        internal byte[] BufferedData = new byte[8192];

        internal void HandleClient(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                while (!_Stream.DataAvailable) ;

                if (Client.Available + CurrentBufferOffset >= 8192)
                {
                    byte[] message = Encoding.UTF8.GetBytes("Too big.");
                    Client.GetStream().Write(message, 0, message.Length);
                    return;
                }
                Stream.Read(BufferedData, CurrentBufferOffset, Client.Available);
                CurrentBufferOffset += Client.Available;
            }
        }

        public void Dispose()
        {
            //Buffered data.
            while (WorkerProcess.IsBusy)
            {
                WorkerProcess.CancelAsync();
            }
            BufferedData = null;
            //network stream.
            _Stream.Dispose();
            Client.Dispose();
            //Client
            //Background worker.
        }
    }
}
