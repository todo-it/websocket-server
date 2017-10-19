using WebSockets.Common.Common;

namespace WebSockets.DemoApp.Server
{
    public class ChatServerProtocol : IConnectionProtocol
    {
        private readonly IWebSocketLogger _logger;

        public ChatServerProtocol(IWebSocketLogger logger)
        {
            _logger = logger;
        }
        
        public void OnConnectionStarted(IConnectionController ctx)
        {
            _logger.Debug(GetType(), "[Server] I got connected to my client");
        }

        public void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code)
        {
            _logger.Debug(GetType(), "[Server] I got disconnected from my client");
        }
        
        public void Process(IConnectionController ctx)
        {
            var req = "Hi my client";
            _logger.Debug(GetType(), "[Server] sending to client {0}", req);
            ctx.Send(req);

            var answer = ctx.ReceiveOrNull();   
            _logger.Debug(GetType(), "[Server] got message from client {0}", answer);

            req = "I still see you my client";
            _logger.Debug(GetType(), "[Server] sending to client {0}", req);
            ctx.Send(req);

            var ending = false;

            while (!ending)
            {
                answer = ctx.ReceiveOrNull();

                req = "You client sent me "+answer;

                if (answer != null && answer.IsText && answer.Text == "bye")
                {
                    ending = true;
                    req += " so I'm ending this conversation";
                }

                _logger.Debug(GetType(), "[Server] sending to client {0}", req);
                ctx.Send(req);
            }
            ctx.CloseConnection(WebSocketCloseCode.GoingAway);
        }
    }
}
