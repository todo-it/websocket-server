﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using WebSockets.Common.Common;
using WebSockets.Common.Exceptions;

namespace WebSockets.Server.Server
{
    public class WebServer : IDisposable
    {
        // maintain a list of open connections so that we can notify the client if the server shuts down
        private readonly List<IDisposable> _openConnections;
        private readonly IServiceFactory _serviceFactory;
        private readonly IWebSocketLogger _logger;
        private X509Certificate2 _sslCertificate;
        private TcpListener _listener;
        private bool _isDisposed = false;

        public WebServer(IServiceFactory serviceFactory, IWebSocketLogger logger)
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
            _openConnections = new List<IDisposable>();
        }

        public void Listen(int port, X509Certificate2 sslCertificate)
        {
            try
            {
                _sslCertificate = sslCertificate;
                var localAddress = IPAddress.Any;
                _listener = new TcpListener(localAddress, port);
                _listener.Start();
                _logger.Info(GetType(), "Server started listening on port {0}", port);
                StartAccept();
            }
            catch (SocketException ex)
            {
                throw new ServerListenerSocketException(
                    $"Error listening on port {port}. Make sure another application is not running and consuming your port.", 
                    ex);
            }
        }

        /// <summary>
        /// Listens on the port specified
        /// </summary>
        public void Listen(int port)
        {
            Listen(port, null);
        }

        /// <summary>
        /// Gets the first available port and listens on it. Returns the port
        /// </summary>
        public int Listen()
        {
            var localAddress = IPAddress.Any;
            _listener = new TcpListener(localAddress, 0);
            _listener.Start();
            StartAccept();
            var port = ((IPEndPoint) _listener.LocalEndpoint).Port;
            _logger.Info(GetType(), "Server started listening on port {0}", port);
            return port;
        }

        private void StartAccept()
        {
            // this is a non-blocking operation. It will consume a worker thread from the threadpool
            _listener.BeginAcceptTcpClient(HandleAsyncConnection, null);
        }

        private static ConnectionDetails GetConnectionDetails(Stream stream, TcpClient tcpClient)
        {
            // read the header and check that it is a GET request
            var header = HttpHelper.ReadHttpHeader(stream);
            var getRegex = new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase);

            var getRegexMatch = getRegex.Match(header);
            if (!getRegexMatch.Success)
            {
                return new ConnectionDetails(stream, tcpClient, string.Empty, ConnectionType.Unknown, header);
            }
            // extract the path attribute from the first line of the header
            var path = getRegexMatch.Groups[1].Value.Trim();

            // check if this is a web socket upgrade request
            var webSocketUpgradeRegex = new Regex("Upgrade: websocket", RegexOptions.IgnoreCase);
            var webSocketUpgradeRegexMatch = webSocketUpgradeRegex.Match(header);

            return webSocketUpgradeRegexMatch.Success ? 
                    new ConnectionDetails(stream, tcpClient, path, ConnectionType.WebSocket, header) 
                : 
                    new ConnectionDetails(stream, tcpClient, path, ConnectionType.Http, header);
        }

        private Stream GetStream(TcpClient tcpClient)
        {
            Stream stream = tcpClient.GetStream();

            // we have no ssl certificate
            if (_sslCertificate == null)
            {
                _logger.Info(GetType(), "Connection is not secure");
                return stream;
            }

            try
            {
                var sslStream = new SslStream(stream, false);
                _logger.Debug(GetType(), "Attempting to secure connection...");
                sslStream.AuthenticateAsServer(_sslCertificate, false, SslProtocols.Tls, true);
                _logger.Info(GetType(), "Connection successfully secured");
                return sslStream;
            }
            catch (AuthenticationException e)
            {
                // TODO: send 401 Unauthorized
                throw;
            }
        }

        private void HandleAsyncConnection(IAsyncResult res)
        {
            try
            {
                if (_isDisposed)
                {
                    return;
                }

                // this worker thread stays alive until either of the following happens:
                // Client sends a close conection request OR
                // An unhandled exception is thrown OR
                // The server is disposed
                using (var tcpClient = _listener.EndAcceptTcpClient(res))
                {
                    // we are ready to listen for more connections (on another thread)
                    StartAccept();
                    _logger.Debug(GetType(), "Server: Connection opened");

                    // get a secure or insecure stream
                    var stream = GetStream(tcpClient);

                    // extract the connection details and use those details to build a connection
                    var connectionDetails = GetConnectionDetails(stream, tcpClient);
                    using (var service = _serviceFactory.CreateInstance(connectionDetails))
                    {
                        try
                        {
                            // record the connection so we can close it if something goes wrong
                            lock (_openConnections)
                            {
                                _openConnections.Add(service);
                            }

                            // respond to the http request.
                            // Take a look at the WebSocketConnection or HttpConnection classes
                            service.Respond();
                        }
                        finally
                        {
                            // forget the connection, we are done with it
                            lock (_openConnections)
                            {
                                _openConnections.Remove(service);
                            }
                        }
                    }
                }

                _logger.Debug(GetType(), "Server: Connection closed");
            }
            catch (ObjectDisposedException)
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch (Exception ex)
            {
                _logger.Err(GetType(), ex);
            }
        }

        private void CloseAllConnections()
        {
            IDisposable[] openConnections;

            lock (_openConnections)
            {
                openConnections = _openConnections.ToArray();
                _openConnections.Clear();
            }

            // safely attempt to close each connection
            foreach (var openConnection in openConnections)
            {
                try
                {
                    openConnection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Err(GetType(), ex);
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                // safely attempt to shut down the listener
                try
                {
                    if (_listener != null)
                    {
                        _listener.Server?.Close();

                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Err(GetType(), ex);
                }

                CloseAllConnections();
                _logger.Debug(GetType(), "Web Server disposed");
            }
        }
    }
}
