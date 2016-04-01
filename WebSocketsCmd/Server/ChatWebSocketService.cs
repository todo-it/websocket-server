﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using WebSockets.Server.WebSocket;
using WebSockets.Common;

namespace WebSocketsCmd.Server
{
    internal class ChatWebSocketService : WebSocketService
    {
        private readonly IWebSocketLogger _logger;

        public ChatWebSocketService(NetworkStream networkStream, TcpClient tcpClient, string header, IWebSocketLogger logger)
            : base(networkStream, tcpClient, header, true, logger)
        {
            _logger = logger;
        }

        protected override void OnTextFrame(string text)
        {
            string response = "ServerABC: " + text;
            base.Send(response);
            _logger.Information(this.GetType(), response);
        }
    }
}
