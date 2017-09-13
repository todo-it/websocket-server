using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebSockets.Common.Common;

namespace WebSockets.Client.Client
{
    public class WebSocketClient : WebSocketBase, IDisposable
    {
        private readonly bool _noDelay;
        private readonly IWebSocketLogger _logger;
        private readonly Stream _stream;
        private readonly TcpClient _tcpClient;

        public WebSocketClient(bool noDelay, IWebSocketLogger logger, IConnectionProtocol protocol, Uri uri, Stream stream, TcpClient tcpClient)
            : base(logger, new ClientSideWebSocketProtocol(logger, protocol, uri, stream, tcpClient))
        {
            _noDelay = noDelay;
            _logger = logger;
            _stream = stream;
            _tcpClient = tcpClient;
        }
        
        public override void CloseConnection(WebSocketCloseCode _)
        {
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
        
        public void ProcessBlocking()
        {
            if (IsOpen)
            {
                throw new ArgumentException("Connection is already open");    
            }

            IsOpen = true;
            ProcessBlocking(_stream, _tcpClient.Client);
            IsOpen = false;
        }

        public virtual void Dispose()
        {
            try
            {
                _stream.Close();    
            } catch(Exception)
            {
                _logger.Warn(GetType(), "Failed to call stream->Close");
            }
            
            try {
            _tcpClient.Close();
            } catch(Exception)
            {
                _logger.Warn(GetType(), "Failed to call tcpClient->Close");
            }
        }
    }
}
