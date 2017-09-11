using System;
using System.Collections.Generic;
using WebSockets.Server;
using WebSockets.Server.Http;
using WebSockets.Common.Common;

namespace WebSocketsCmd.Server
{
    internal class DemoServiceFactory : IServiceFactory
    {
        private readonly IWebSocketLogger _logger;
        private readonly Dictionary<string,Func<ConnectionDetails,IService>> _webSocketHandlers;
        
        public DemoServiceFactory(IWebSocketLogger logger)
        {
            _logger = logger;
            _webSocketHandlers = new Dictionary<string, Func<ConnectionDetails, IService>> {
                {"/chat", x => new ChatWebSocketService(x.Stream, x.TcpClient, x.Header, _logger)}
            };
        }

        public IService CreateInstance(ConnectionDetails connectionDetails)
        {
            switch (connectionDetails.ConnectionType)
            {
                case ConnectionType.WebSocket:
                    // you can support different kinds of web socket connections using a different path
                    Func<ConnectionDetails,IService> handler;

                    if (_webSocketHandlers.TryGetValue(connectionDetails.Path, out handler))
                    {
                        return handler(connectionDetails);
                    }
                    return new RequestNotSupportedService("websocket handler not present for given url", connectionDetails.Stream, connectionDetails.Header, _logger);
                    
                case ConnectionType.Http:
                    return new RequestNotSupportedService("http connections are not supported", connectionDetails.Stream, connectionDetails.Header, _logger);
            }

            return new BadRequestService(connectionDetails.Stream, connectionDetails.Header, _logger);
        }
    }
}
