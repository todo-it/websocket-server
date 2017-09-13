using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace WebSockets.Common.Common
{
    public static class Magics
    {
        public const string CrLf = "\r\n"; //no matter if Windows or Unix
        public const int WebSocketMinimumVersion = 13;
        public static readonly TimeSpan ClientWaitForServerClose = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Combines the key supplied by the client with a guid and returns the sha1 hash of the combination
        /// </summary>
        public static string ComputeSocketAcceptString(string secWebSocketKey)
        {
            // this is a guid as per the web socket spec
            const string webSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            var concatenated = secWebSocketKey + webSocketGuid;
            var concatenatedAsBytes = Encoding.UTF8.GetBytes(concatenated);
            var sha1Hash = SHA1.Create().ComputeHash(concatenatedAsBytes);
            var secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }
    }
}
