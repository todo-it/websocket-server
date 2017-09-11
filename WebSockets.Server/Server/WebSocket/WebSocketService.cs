﻿using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;
using WebSockets.Common.Common;
using WebSockets.Common.Exceptions;

namespace WebSockets.Server.WebSocket
{
    public class WebSocketService : WebSocketBase, IService
    {
        private readonly Stream _stream;
        private readonly string _header;
        private readonly IWebSocketLogger _logger;
        private readonly TcpClient _tcpClient;
        private bool _isDisposed = false;

        public WebSocketService(Stream stream, TcpClient tcpClient, string header, bool noDelay, IWebSocketLogger logger)
            : base(logger)
        {
            _stream = stream;
            _header = header;
            _logger = logger;
            _tcpClient = tcpClient;

            // send requests immediately if true (needed for small low latency packets but not a long stream). 
            // Basically, dont wait for the buffer to be full before before sending the packet
            tcpClient.NoDelay = noDelay;
        }

        public void Respond()
        {
            OpenBlocking(_stream, _tcpClient.Client);
        }

        protected override void PerformHandshake(Stream stream)
        {
            var header = _header;

            try
            {
                var webSocketKeyRegex = new Regex("Sec-WebSocket-Key: (.*)");
                var webSocketVersionRegex = new Regex("Sec-WebSocket-Version: (.*)");

                // check the version. Support version 13 and above
                const int WebSocketVersion = 13;
                var secWebSocketVersion = Convert.ToInt32(webSocketVersionRegex.Match(header).Groups[1].Value.Trim());
                if (secWebSocketVersion < WebSocketVersion)
                {
                    throw new WebSocketVersionNotSupportedException(string.Format("WebSocket Version {0} not suported. Must be {1} or above", secWebSocketVersion, WebSocketVersion));
                }

                var secWebSocketKey = webSocketKeyRegex.Match(header).Groups[1].Value.Trim();
                var setWebSocketAccept = ComputeSocketAcceptString(secWebSocketKey);
                var response = ("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                   + "Connection: Upgrade" + Environment.NewLine
                                   + "Upgrade: websocket" + Environment.NewLine
                                   + "Sec-WebSocket-Accept: " + setWebSocketAccept);

                HttpHelper.WriteHttpHeader(response, stream);
                _logger.Information(GetType(), "Web Socket handshake sent");
            }
            catch (WebSocketVersionNotSupportedException ex)
            {
                var response = "HTTP/1.1 426 Upgrade Required" + Environment.NewLine + "Sec-WebSocket-Version: 13";
                HttpHelper.WriteHttpHeader(response, stream);
                throw;
            }
            catch (Exception ex)
            {
                HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", stream);
                throw;
            }
        }

        private static void CloseConnection(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public virtual void Dispose()
        {
            // send special web socket close message. Don't close the network stream, it will be disposed later
            if (_stream.CanWrite && !_isDisposed)
            {
                using (var stream = new MemoryStream())
                {
                    // set the close reason to Normal
                    BinaryReaderWriter.WriteUShort((ushort) WebSocketCloseCode.Normal, stream, false);

                    // send close message to client to begin the close handshake
                    Send(WebSocketOpCode.ConnectionClose, stream.ToArray());
                }

                _isDisposed = true;
                _logger.Information(GetType(), "Sent web socket close message to client");
                CloseConnection(_tcpClient.Client);
            }
        }

        protected override void OnConnectionClose(byte[] payload)
        {
            Send(WebSocketOpCode.ConnectionClose, payload);
            _logger.Information(GetType(), "Sent response close message to client");
            _isDisposed = true;

            base.OnConnectionClose(payload);
        }
    }
}