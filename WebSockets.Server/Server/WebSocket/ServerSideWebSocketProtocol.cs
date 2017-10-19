using System;
using System.IO;
using System.Text.RegularExpressions;
using WebSockets.Common.Common;

namespace WebSockets.Server.Server.WebSocket
{
    public class ServerSideWebSocketProtocol : IConnectionProtocol
    {
        private readonly IWebSocketLogger _logger;
        private readonly IConnectionProtocol _adapted;
        private readonly Stream _stream;
        private readonly string _header;
        public bool CloseWasSent { get; set; }

        public ServerSideWebSocketProtocol(IWebSocketLogger logger, IConnectionProtocol adapted, Stream stream, string header)
        {
            _logger = logger;
            _adapted = adapted;
            _stream = stream;
            _header = header;
        }

        public void OnConnectionStarted(IConnectionController ctx)
        {
            PerformHandshake(_stream);
            _adapted.OnConnectionStarted(ctx);
        }

        public void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code)
        {
            _adapted.OnConnectionClosed(ctx, code);
        }
        
        public void Process(IConnectionController ctx)
        {
            _adapted.Process(ctx);
        }
        
        private void PerformHandshake(Stream stream)
        {
            SuccessOrFailure<string> response;

            try
            {
                response = ComputeHandshake();
            } catch(Exception)
            {
                HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", stream);
                throw;
            }

            HttpHelper.WriteHttpHeader(response.Value, stream);
            
            if (response.IsSuccess)
            {
                _logger.Debug(GetType(), "Web Socket handshake successfully sent");
            }
            else
            {
                _logger.Err(GetType(), "Web Socket handshake failed to send");
            }
        }

        private SuccessOrFailure<string> ComputeHandshake()
        {
            var webSocketKeyRegex = new Regex("Sec-WebSocket-Key: (.*)");
            var webSocketVersionRegex = new Regex("Sec-WebSocket-Version: (.*)");

            // check the version. Support version 13 and above
                
            var secWebSocketVersion = Convert.ToInt32(webSocketVersionRegex.Match(_header).Groups[1].Value.Trim());
            if (secWebSocketVersion < Magics.WebSocketMinimumVersion)
            {
                _logger.Err(
                    GetType(), 
                    $"WebSocket Version {secWebSocketVersion} not suported. Must be {Magics.WebSocketMinimumVersion} or above");

                return SuccessOrFailure<string>.CreateFailure($"HTTP/1.1 426 Upgrade Required{Magics.CrLf}Sec-WebSocket-Version: 13");
            }

            var secWebSocketKey = webSocketKeyRegex.Match(_header).Groups[1].Value.Trim();
            var setWebSocketAccept = Magics.ComputeSocketAcceptString(secWebSocketKey);
            var response = 
                $"HTTP/1.1 101 Switching Protocols{Magics.CrLf}" +
                $"Connection: Upgrade{Magics.CrLf}" +
                $"Upgrade: websocket{Magics.CrLf}" +
                $"Sec-WebSocket-Accept: {setWebSocketAccept}";

            return SuccessOrFailure<string>.CreateSuccess(response);
        }
    }
}
