using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSockets.Common.Common;
using WebSockets.Common.Exceptions;

namespace WebSockets.Client.Client
{
    public class WebSocketClient : WebSocketBase, IDisposable
    {
        private readonly bool _noDelay;
        private readonly IWebSocketLogger _logger;
        private TcpClient _tcpClient;
        private Stream _stream;
        private Uri _uri;
        private ManualResetEvent _conectionCloseWait;

        public WebSocketClient(bool noDelay, IWebSocketLogger logger)
            : base(logger)
        {
            _noDelay = noDelay;
            _logger = logger;
            _conectionCloseWait = new ManualResetEvent(false);
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private Stream GetStream(TcpClient tcpClient, bool isSecure)
        {
            if (isSecure)
            {
                var sslStream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                _logger.Information(GetType(), "Attempting to secure connection...");
                sslStream.AuthenticateAsClient("clusteredanalytics.com");
                _logger.Information(GetType(), "Connection successfully secured.");
                return sslStream;
            }
            _logger.Information(GetType(), "Connection not secure");
            return tcpClient.GetStream();
        }

        public virtual void OpenBlocking(Uri uri)
        {
            if (!_isOpen)
            {
                var host = uri.Host;
                var port = uri.Port;
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = _noDelay;

                IPAddress ipAddress;
                if (IPAddress.TryParse(host, out ipAddress))
                {
                    _tcpClient.Connect(ipAddress, port);
                }
                else
                {
                    _tcpClient.Connect(host, port);
                }

                var isSecure = port == 443;
                _stream = GetStream(_tcpClient, isSecure);

                _uri = uri;
                _isOpen = true;
                base.OpenBlocking(_stream, _tcpClient.Client);
                _isOpen = false;
            }
        }

        protected override void PerformHandshake(Stream stream)
        {
            var uri = _uri;
            var rand = new Random();
            var keyAsBytes = new byte[16];
            rand.NextBytes(keyAsBytes);
            var secWebSocketKey = Convert.ToBase64String(keyAsBytes);

            var handshakeHttpRequestTemplate = @"GET {0} HTTP/1.1{4}" +
                                                  "Host: {1}:{2}{4}" +
                                                  "Upgrade: websocket{4}" +
                                                  "Connection: Upgrade{4}" +
                                                  "Sec-WebSocket-Key: {3}{4}" +
                                                  "Sec-WebSocket-Version: 13{4}{4}";

            var handshakeHttpRequest = string.Format(handshakeHttpRequestTemplate, uri.PathAndQuery, uri.Host, uri.Port, secWebSocketKey, Environment.NewLine);
            var httpRequest = Encoding.UTF8.GetBytes(handshakeHttpRequest);
            stream.Write(httpRequest, 0, httpRequest.Length);
            _logger.Information(GetType(), "Handshake sent. Waiting for response.");

            // make sure we escape the accept string which could contain special regex characters
            var regexPattern = "Sec-WebSocket-Accept: (.*)";
            var regex = new Regex(regexPattern);

            var response = string.Empty;

            try
            {
                response = HttpHelper.ReadHttpHeader(stream);
            }
            catch (Exception ex)
            {
                throw new WebSocketHandshakeFailedException("Handshake unexpected failure", ex);
            }

            // check the accept string
            var expectedAcceptString = ComputeSocketAcceptString(secWebSocketKey);
            var actualAcceptString = regex.Match(response).Groups[1].Value.Trim();
            if (expectedAcceptString != actualAcceptString)
            {
                throw new WebSocketHandshakeFailedException(string.Format("Handshake failed because the accept string from the server '{0}' was not the expected string '{1}'", expectedAcceptString, actualAcceptString));
            }
            _logger.Information(GetType(), "Handshake response received. Connection upgraded to WebSocket protocol.");
        }

        public virtual void Dispose()
        {
            if (_isOpen)
            {
                using (var stream = new MemoryStream())
                {
                    // set the close reason to GoingAway
                    BinaryReaderWriter.WriteUShort((ushort) WebSocketCloseCode.GoingAway, stream, false);

                    // send close message to server to begin the close handshake
                    Send(WebSocketOpCode.ConnectionClose, stream.ToArray());
                    _logger.Information(GetType(), "Sent websocket close message to server. Reason: GoingAway");
                }

                // this needs to run on a worker thread so that the read loop (in the base class) is not blocked
                Task.Factory.StartNew(WaitForServerCloseMessage);
            }
        }

        private void WaitForServerCloseMessage()
        {
            // as per the websocket spec, the server must close the connection, not the client. 
            // The client is free to close the connection after a timeout period if the server fails to do so
            _conectionCloseWait.WaitOne(TimeSpan.FromSeconds(10));

            // this will only happen if the server has failed to reply with a close response
            if (_isOpen)
            {
                _logger.Warning(GetType(), "Server failed to respond with a close response. Closing the connection from the client side.");

                // wait for data to be sent before we close the stream and client
                _tcpClient.Client.Shutdown(SocketShutdown.Both);
                _stream.Close();
                _tcpClient.Close();
            }

            _logger.Information(GetType(), "Client: Connection closed");
        }

        protected override void OnConnectionClose(byte[] payload)
        {
            // server has either responded to a client close request or closed the connection for its own reasons
            // the server will close the tcp connection so the client will not have to do it
            _isOpen = false;
            _conectionCloseWait.Set();
            base.OnConnectionClose(payload);
        }

    }
}
