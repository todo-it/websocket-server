using System;
using System.IO;
using System.Net.Sockets;
using WebSockets.Common.Common;

namespace WebSockets.Server.Server.WebSocket
{
    public class WebSocketService : WebSocketBase, IService
    {
        private readonly Stream _stream;
        private readonly IWebSocketLogger _logger;
        private readonly TcpClient _tcpClient;
        private bool _closeWasSent;

        public WebSocketService(Stream stream, TcpClient tcpClient, string header, bool noDelay, IWebSocketLogger logger, IConnectionProtocol clientProtocol)
            : base(logger, new ServerSideWebSocketProtocol(logger, clientProtocol, stream, header))
        {
            _stream = stream;
            _logger = logger;
            _tcpClient = tcpClient;

            // send requests immediately if true (needed for small low latency packets but not a long stream). 
            // Basically, dont wait for the buffer to be full before before sending the packet
            tcpClient.NoDelay = noDelay;
        }

        public void Respond()
        {
            ProcessBlocking(_stream, _tcpClient.Client);
        }
        
        protected override void CloseConnectionImpl(WebSocketCloseCode code)
        {
            _logger.Debug(GetType(), "CloseConnection {0}", code);
            if (_closeWasSent)
            {
                return;
            }
            
            if (_stream.CanWrite)
            {
                Send(WebSocketOpCode.ConnectionClose, WebSocketCloseCode.Normal.AsBytesForSend());
                _closeWasSent = true;
                _logger.Debug(GetType(), "Sent web socket close message to client");
            }
            
            CleanupConnection();
        }
        
        private void CleanupConnection()
        {
            _logger.Debug(GetType(), "CleanupConnection");
            try
            {
                _tcpClient.Client.Shutdown(SocketShutdown.Both);    
            } catch(Exception)
            {
                _logger.Warn(GetType(), "Client: Failed to call tcpClient->Client->Shutdown");
            }
            
            try
            {
                _stream.Close();
            } catch(Exception)
            {
                _logger.Warn(GetType(), "Client: Failed to call stream->Close");
            }

            try
            {
                _tcpClient.Close();
            } catch(Exception)
            {
                _logger.Warn(GetType(), "Client: Failed to call _tcpClient->Close");
            }
        }
        
        public virtual void Dispose()
        {
            CleanupConnection();
        }
    }
}
