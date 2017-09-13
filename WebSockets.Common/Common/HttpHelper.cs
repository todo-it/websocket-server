using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebSockets.Common.Exceptions;

namespace WebSockets.Common.Common
{
    public class HttpHelper
    {
        public static string ReadHttpHeader(Stream stream)
        {
            var length = 1024*16; // 16KB buffer more than enough for http header
            var buffer = new byte[length];
            var offset = 0;
            var bytesRead = 0;
            do
            {
                if (offset >= length)
                {
                    throw new EntityTooLargeException("Http header message too large to fit in buffer (16KB)");
                }

                bytesRead = stream.Read(buffer, offset, length - offset);
                offset += bytesRead;
                var header = Encoding.UTF8.GetString(buffer, 0, offset);

                // as per http specification, all headers should end this this
                if (header.Contains("\r\n\r\n"))
                {
                    return header;
                }

            } while (bytesRead > 0);

            return string.Empty;
        }

        public static void WriteHttpHeader(string response, Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(response.Trim() + Magics.CrLf + Magics.CrLf);
            stream.Write(bytes, 0, bytes.Length);
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(IWebSocketLogger logger, object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            logger.Err(typeof(HttpHelper), "Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public static Stream GetStream(IWebSocketLogger logger, TcpClient tcpClient, bool isSecure, string targetHostForSsl, RemoteCertificateValidationCallback validator)
        {
            if (!isSecure)
            {
                logger.Debug(typeof(HttpHelper), "Connection is not secure");
                return tcpClient.GetStream();
            }
            
            var sslStream = new SslStream(tcpClient.GetStream(), false, validator, null);
            logger.Debug(typeof(HttpHelper), "Attempting to secure connection...");
            sslStream.AuthenticateAsClient(targetHostForSsl);
            logger.Debug(typeof(HttpHelper), "Connection successfully secured.");
            return sslStream;
        }
    }
}
