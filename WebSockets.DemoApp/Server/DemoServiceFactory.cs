using System;
using System.Collections.Generic;
using WebSockets.Common.Common;
using WebSockets.Server.Server;
using WebSockets.Server.Server.Http;
using WebSockets.Server.Server.WebSocket;

namespace WebSockets.DemoApp.Server
{
    internal class DemoServiceFactory : IServiceFactory
    {
        private readonly IWebSocketLogger _logger;
        private readonly Dictionary<string,Func<ConnectionDetails,IConnectionProtocol>> _webSocketHandlers;
        
        public DemoServiceFactory(IWebSocketLogger logger)
        {
            _logger = logger;
            _webSocketHandlers = new Dictionary<string, Func<ConnectionDetails, IConnectionProtocol>> {
                {"/chat", x => new ChatServerProtocol(_logger)}
            };
        }

        public IService CreateInstance(ConnectionDetails connectionDetails)
        {
            switch (connectionDetails.ConnectionType)
            {
                case ConnectionType.WebSocket:
                    // you can support different kinds of web socket connections using a different path
                    Func<ConnectionDetails,IConnectionProtocol> handler;

                    if (_webSocketHandlers.TryGetValue(connectionDetails.Path, out handler))
                    {
                        return new WebSocketService(
                            connectionDetails.Stream,
                            connectionDetails.TcpClient,
                            connectionDetails.Header,
                            true,
                            _logger,
                            handler(connectionDetails));
                    }
                    return new RequestNotSupportedService("websocket handler not present for given url", connectionDetails.Stream, connectionDetails.Header, _logger);
                    
                case ConnectionType.Http:
                    return new RequestNotSupportedService("http connections are not supported", connectionDetails.Stream, connectionDetails.Header, _logger);
            }

            return new BadRequestService(connectionDetails.Stream, connectionDetails.Header, _logger);
        }
    }
}
