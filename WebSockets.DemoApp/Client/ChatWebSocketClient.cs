using System.Text;
using WebSockets.Client.Client;
using WebSockets.Common.Common;

namespace WebSocketsCmd.Client
{
    class ChatWebSocketClient : WebSocketClient
    {
        public ChatWebSocketClient(bool noDelay, IWebSocketLogger logger) : base(noDelay, logger)
        {
            
        }

        public void Send(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            Send(WebSocketOpCode.TextFrame, buffer);
        }
    }
}
