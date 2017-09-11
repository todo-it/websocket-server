using WebSockets.Server;
using WebSockets.Server.Http;
using WebSockets.Common.Common;

namespace WebSocketsCmd.Server
{
    internal class ServiceFactory : IServiceFactory
    {
        private readonly IWebSocketLogger _logger;
        
        public ServiceFactory(IWebSocketLogger logger)
        {
            _logger = logger;
        }

        public IService CreateInstance(ConnectionDetails connectionDetails)
        {
            switch (connectionDetails.ConnectionType)
            {
                case ConnectionType.WebSocket:
                    // you can support different kinds of web socket connections using a different path
                    if (connectionDetails.Path == "/chat")
                    {
                        return new ChatWebSocketService(connectionDetails.Stream, connectionDetails.TcpClient, connectionDetails.Header, _logger);
                    }
                    break;
                case ConnectionType.Http:
                    return new RegularWebRequestNotSupportedService(connectionDetails.Stream, connectionDetails.Header, _logger);
            }

            return new BadRequestService(connectionDetails.Stream, connectionDetails.Header, _logger);
        }
    }
}
