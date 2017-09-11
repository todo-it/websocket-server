using System;
using WebSockets.Server;
using System.Diagnostics;
using WebSocketsCmd.Client;
using WebSocketsCmd.Properties;
using System.Threading;
using WebSocketsCmd.Server;
using WebSockets.Common.Common;
using WebSockets.Common.Events;

namespace WebSocketsCmd
{
    public class Program
    {
        private static void TestClient(IWebSocketLogger logger, string hostname, int port)
        {
            using (var client = new ChatWebSocketClient(true, logger))
            {
                var uri = new Uri($"ws://{hostname}:{port}/chat");
                client.TextFrame += Client_TextFrame;
                client.ConnectionOpened += Client_ConnectionOpened;

                // test the open handshake
                client.OpenBlocking(uri);
            }

            Trace.TraceInformation("Client finished, press any key");
            Console.ReadKey();
        }

        private static void Client_ConnectionOpened(object sender, EventArgs e)
        {
            Trace.TraceInformation("Client: Connection Opened");
            var client = (ChatWebSocketClient) sender;

            // test sending a message to the server
            client.Send("Hi");
        }

        private static void Client_TextFrame(object sender, TextFrameEventArgs e)
        {
            Trace.TraceInformation("Client: {0}", e.Text);
            var client = (ChatWebSocketClient) sender;

            // lets test the close handshake
            client.Dispose();
        }

        private static void Main(string[] args)
        {
            var logger = new DiagnosticsTraceBasedLogger();

            try
            {
                var port = Settings.Default.Port;
                
                // used to decide what to do with incoming connections
                var serviceFactory = new ServiceFactory(logger);

                using (var server = new WebServer(serviceFactory, logger))
                {
                    server.Listen(port);

                    ThreadPool.QueueUserWorkItem(x => TestClient(logger, "localhost", port));

                    Console.WriteLine("Press any key to stop server");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                logger.Error(typeof(Program), ex);
                Console.ReadKey();
            }
        }
    }
}
