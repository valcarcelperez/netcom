using System;
using System.CommunicationFramework.Common;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    class Program
    {
        static CancellationTokenSource cancellationTokenSourceForListener = new CancellationTokenSource();

        static void Main(string[] args)
        {
            StartListener();

            //ConnectAndReceiveUsingCancelationToken();

            ConnectAndReceiveUsingProcessExecutor();

            StopListener();
            Thread.Sleep(1000);
        }

        static void ConnectAndReceiveUsingCancelationToken()
        {
            CancellationTokenSource cancellationTokenSourceForClient = new CancellationTokenSource();

            Task.Run(() =>
            {
                // cancel client operation in 3 sec
                Thread.Sleep(3000);
                cancellationTokenSourceForClient.Cancel();
                Console.WriteLine("cancellationTokenSourceForClient canceled");
            });
            
            Task.Run(async () =>
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect("127.0.0.1", 8080);
                NetworkStream stream = new NetworkStream(socket);

                byte[] buffer = new byte[1024];
                Console.WriteLine("Client - Receiving asynchronously ...");

                try
                {
                    // this line never returns because the CancellationToken is not used inside Stream.ReadAsync
                    int count = await stream.ReadAsync(buffer, 1, 1, cancellationTokenSourceForClient.Token);
                    Console.WriteLine("Client - Received {0} bytes", count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client - Exception: {0}", ex.GetType().Name);
                }
            })
            .Wait();
        }

        static void ConnectAndReceiveUsingProcessExecutor()
        {
            Task.Run(async () =>
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect("127.0.0.1", 8080);
                NetworkStream stream = new NetworkStream(socket);

                byte[] buffer = new byte[1024];
                Console.WriteLine("Client - Receiving asynchronously ...");

                try
                {
                    // this lines raises a TimeoutException after 3 sec.
                    int count = await ProcessExecutor.ExecuteProcessAsync(async () => { return await stream.ReadAsync(buffer, 1, 1); }, () => { socket.Dispose(); }, 3000);
                    Console.WriteLine("Client - Received {0} bytes", count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client - Exception: {0}", ex.GetType().Name);
                }
            })
            .Wait();
        }

        static void StartListener()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 8080);

            Task.Run(() =>
            {
                tcpListener.Start();
                Console.WriteLine("TcpListener - Started");

                while (!cancellationTokenSourceForListener.IsCancellationRequested)
                {
                    if (!tcpListener.Pending())
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    Socket socket = tcpListener.AcceptSocket();
                    Console.WriteLine("TcpListener - Client connected");
                }

                tcpListener.Start();
                Console.WriteLine("TcpListener - Stopped");
            });
        }

        static void StopListener()
        {
            cancellationTokenSourceForListener.Cancel();
        }
    }
}
