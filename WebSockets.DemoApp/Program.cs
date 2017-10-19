using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using WebSockets.Client.Client;
using WebSockets.Common.Common;
using WebSockets.DemoApp.Client;
using WebSockets.DemoApp.Properties;
using WebSockets.DemoApp.Server;
using WebSockets.Server.Server;

namespace WebSockets.DemoApp
{
    public class Program
    {
        private static void TestClient(IWebSocketLogger logger, string hostname, int port)
        {
            var uri = new Uri($"ws://{hostname}:{port}/chat");
            
            var tcpClient = new TcpClient();
            tcpClient.NoDelay = true;
                
            tcpClient.Connect(hostname, port);

            var isSecure = port == 443;
            var targetHostForSsl = "somehost";
            var stream = HttpHelper.GetStream(logger, tcpClient, isSecure, targetHostForSsl,
                (sender, cert, chain, err) => HttpHelper.ValidateServerCertificate(logger, sender, cert, chain, err));
            
            using (var client = new WebSocketClient(true, logger, new ChatClientProtocol(logger), uri, stream, tcpClient))
            {
                // test the open handshake
                client.ProcessBlocking();
                
                logger.Debug(typeof(Program), "Client finished");
                //Console.ReadKey();
            }
        }

        private static void Main(string[] args)
        {
            var logger = new ConsoleWriteLineBasedLogger();

            try
            {
                var port = Settings.Default.Port;
                
                // used to decide what to do with incoming connections
                var webSocketHandlers = new Dictionary<string, Func<IConnectionProtocol>> {
                    {"/chat", () => new ChatServerProtocol(logger)}
                };

                var serviceFactory = new DefaultServiceFactory(logger, x => {
                    Func<IConnectionProtocol> handler;
                    return webSocketHandlers.TryGetValue(x.Path, out handler) ? handler() : null;
                });

                using (var server = new WebServer(serviceFactory, logger))
                {
                    server.Listen(port);

                    if (!args.Contains("--noclient"))
                    {
                        ThreadPool.QueueUserWorkItem(x => TestClient(logger, "localhost", port));    
                    }
                    
                    Console.WriteLine("Press any key to stop server");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                logger.Err(typeof(Program), ex);
                Console.ReadKey();
            }
        }
    }
}
