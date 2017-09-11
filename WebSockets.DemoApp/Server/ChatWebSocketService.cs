﻿using System.Net.Sockets;
using WebSockets.Server.WebSocket;
using System.IO;
using WebSockets.Common.Common;

namespace WebSocketsCmd.Server
{
    internal class ChatWebSocketService : WebSocketService
    {
        private readonly IWebSocketLogger _logger;

        public ChatWebSocketService(Stream stream, TcpClient tcpClient, string header, IWebSocketLogger logger)
            : base(stream, tcpClient, header, true, logger)
        {
            _logger = logger;
        }

        protected override void OnTextFrame(string text)
        {
            var response = "ServerABC: " + text;
            Send(response);
            _logger.Information(GetType(), response);
        }
    }
}
