using System;
using System.Text;

namespace WebSockets.Common.Common
{
    public interface IConnectionController : IDisposable
    {
        void CloseConnection(WebSocketCloseCode code);
        /// <summary>
        /// Received text or binary data. Returns null if received non data frame (ping, pong, invalid) or connection was closed
        /// </summary>
        /// <returns></returns>
        ReceivedData ReceiveOrNull();
        void Send(WebSocketOpCode code, byte[] input, bool isLastFrame = true);
    }

    public static class ConnectionControllerExtensions
    {
        public static void Send(this IConnectionController self, string input)
        {
            self.Send(WebSocketOpCode.TextFrame, Encoding.UTF8.GetBytes(input));
        }

        public static void Send(this IConnectionController self, byte[] input)
        {
            self.Send(WebSocketOpCode.BinaryFrame, input);
        }
    }
}
