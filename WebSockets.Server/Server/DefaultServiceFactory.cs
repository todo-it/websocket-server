using System;
using WebSockets.Common.Common;
using WebSockets.Server.Server.Http;
using WebSockets.Server.Server.WebSocket;

namespace WebSockets.Server.Server
{
    public class DefaultServiceFactory : IServiceFactory
    {
        private readonly IWebSocketLogger _logger;
        private readonly Func<ConnectionDetails, IConnectionProtocol> _webSocketProtocolProvider;

        public DefaultServiceFactory(
            IWebSocketLogger logger, 
            Func<ConnectionDetails, IConnectionProtocol> webSocketProtocolProvider)
        {
            _logger = logger;
            _webSocketProtocolProvider = webSocketProtocolProvider;
        }

        public IService CreateInstance(ConnectionDetails connectionDetails)
        {
            switch (connectionDetails.ConnectionType)
            {
                case ConnectionType.WebSocket:
                    // you can support different kinds of web socket connections using a different path
                    var protocolOrNull = _webSocketProtocolProvider(connectionDetails);

                    if (protocolOrNull == null)
                    {
                        return new RequestNotSupportedService("websocket handler not present for given url", connectionDetails.Stream, connectionDetails.Header, _logger);
                    }

                    return new WebSocketService(
                        connectionDetails.Stream,
                        connectionDetails.TcpClient,
                        connectionDetails.Header,
                        true,
                        _logger,
                        protocolOrNull);
                    
                case ConnectionType.Http:
                    return new RequestNotSupportedService("http connections are not supported", connectionDetails.Stream, connectionDetails.Header, _logger);
            }

            return new BadRequestService(connectionDetails.Stream, connectionDetails.Header, _logger);
        }
    }
}
