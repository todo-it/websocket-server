using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebSockets.Common.Common;
using WebSockets.Common.Exceptions;

namespace WebSockets.Client.Client
{
    public class ClientSideWebSocketProtocol : IConnectionProtocol
    {
        private readonly IWebSocketLogger _logger;
        private readonly IConnectionProtocol _adapted;
        private readonly Uri _uri;
        private readonly Stream _stream;
        private bool _closeWasSent,_serverConfirmedClose;

        public ClientSideWebSocketProtocol(IWebSocketLogger logger, IConnectionProtocol adapted, Uri uri, Stream stream, TcpClient tcpClient)
        {
            _logger = logger;
            _adapted = adapted;
            _uri = uri;
            _stream = stream;
        }

        public void OnConnectionStarted(IConnectionController ctx)
        {
            PerformHandshake();
            _adapted.OnConnectionStarted(ctx);
        }

        public void CloseConnection(IConnectionController ctx, WebSocketCloseCode code)
        {
            if (_closeWasSent)
            {
                return;
            }
            
            // set the close reason to GoingAway
            // send close message to server to begin the close handshake
            ctx.Send(WebSocketOpCode.ConnectionClose, WebSocketCloseCode.GoingAway.AsBytesForSend());
            _closeWasSent = true;

            _logger.Debug(GetType(), "Sent websocket close message to server. Reason: {0}", code);
            
            // as per the websocket spec, the server must close the connection, not the client. 
            // The client is free to close the connection after a timeout period if the server fails to do so
            
            ctx.ReceiveOrNull();
            //TODO should kill connection if it wasn't confirmed to be closed by server within given timeout period

            // this will only happen if the server has failed to reply with a close response
            if (_serverConfirmedClose)
            {
                _logger.Debug(GetType(), "Client: Already closed connection");
                return;
            }

            _logger.Warn(GetType(), "Server failed to respond with a close response. Closing the connection from the client side.");

            // wait for data to be sent before we close the stream and client
            ctx.CloseConnection(code);
            _logger.Debug(GetType(), "Client: Connection closed");
        }

        public void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code, string reason)
        {
            _logger.Debug(GetType(), "OnConnectionClosed()");
            
            // server has either responded to a client close request or closed the connection for its own reasons
            // the server will close the tcp connection so the client will not have to do it
            _serverConfirmedClose = true;
            ctx.CloseConnection(code);
            _adapted.OnConnectionClosed(ctx, code, reason);
        }
        
        public void Process(IConnectionController ctx)
        {
            _adapted.Process(ctx);
        }
        
        private void PerformHandshake()
        {
            var rand = new Random();
            var keyAsBytes = new byte[16];
            rand.NextBytes(keyAsBytes);
            var secWebSocketKey = Convert.ToBase64String(keyAsBytes);
            
            var handshakeHttpRequest = 
                $"GET {_uri.PathAndQuery} HTTP/1.1{Magics.CrLf}" +
                $"Host: {_uri.Host}:{_uri.Port}{Magics.CrLf}" +
                $"Upgrade: websocket{Magics.CrLf}" +
                $"Connection: Upgrade{Magics.CrLf}" +
                $"Sec-WebSocket-Key: {secWebSocketKey}{Magics.CrLf}" +
                $"Sec-WebSocket-Version: 13{Magics.CrLf}{Magics.CrLf}";

            var httpRequest = Encoding.UTF8.GetBytes(handshakeHttpRequest);
            _stream.Write(httpRequest, 0, httpRequest.Length);
            _logger.Debug(GetType(), "Handshake sent. Waiting for response.");

            // make sure we escape the accept string which could contain special regex characters
            var regexPattern = "Sec-WebSocket-Accept: (.*)";
            var regex = new Regex(regexPattern);

            string response;

            try
            {
                response = HttpHelper.ReadHttpHeader(_stream);
            }
            catch (Exception ex)
            {
                throw new WebSocketHandshakeFailedException("Handshake unexpected failure", ex);
            }

            // check the accept string
            var expectedAcceptString = Magics.ComputeSocketAcceptString(secWebSocketKey);
            var actualAcceptString = regex.Match(response).Groups[1].Value.Trim();
            if (expectedAcceptString != actualAcceptString)
            {
                throw new WebSocketHandshakeFailedException(
                    $"Handshake failed because the accept string from the server '{expectedAcceptString}' was not the expected string '{actualAcceptString}'");
            }
            _logger.Debug(GetType(), "Handshake response received. Connection upgraded to WebSocket protocol.");
        }
    }
}
