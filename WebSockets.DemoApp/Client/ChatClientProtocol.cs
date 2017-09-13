using WebSockets.Common.Common;

namespace WebSockets.DemoApp.Client
{
    public class ChatClientProtocol : IConnectionProtocol
    {
        private readonly IWebSocketLogger _logger;

        public ChatClientProtocol(IWebSocketLogger logger)
        {
            _logger = logger;
        }
        
        public void OnConnectionStarted(IConnectionController ctx)
        {
            _logger.Debug(GetType(), "[Client] I got connected to my server");
        }

        public void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code, string reason)
        {
            _logger.Debug(GetType(), "[Client] I got disconnected from my server");
        }
        
        public void Process(IConnectionController ctx)
        {
            var req = "Hi my server";
            _logger.Debug(GetType(), "[Client] sending to server {0}", req);
            ctx.Send(req);

            var answer = ctx.ReceiveOrNull();
            _logger.Debug(GetType(), "[Client] got message from server {0}", answer);

            req = "I still see you my server";
            _logger.Debug(GetType(), "[Client] sending to server {0}", req);
            ctx.Send(req);

            answer = ctx.ReceiveOrNull();
            _logger.Debug(GetType(), "[Client] got message from server {0}", answer);

            req = "bye";
            _logger.Debug(GetType(), "[Client] sending to server {0}", req);
            ctx.Send(req);

            ctx.ReceiveOrNull();
        }
    }
}
