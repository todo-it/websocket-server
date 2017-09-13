using System.IO;
using System.Net.Sockets;

namespace WebSockets.Server.Server
{
    public class ConnectionDetails
    {
        public Stream Stream { get; }
        public TcpClient TcpClient { get; }
        public ConnectionType ConnectionType { get; }
        public string Header { get; }

        // this is the path attribute in the first line of the http header
        public string Path { get; }

        public ConnectionDetails (Stream stream, TcpClient tcpClient, string path, ConnectionType connectionType, string header)
        {
            Stream = stream;
            TcpClient = tcpClient;
            Path = path;
            ConnectionType = connectionType;
            Header = header;
        }
    }
}
