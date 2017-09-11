using System;
using WebSockets.Common.Common;

namespace WebSockets.Common.Events
{
    public class ConnectionCloseEventArgs : EventArgs
    {
        public WebSocketCloseCode Code { get; }
        public string Reason { get; }

        public ConnectionCloseEventArgs(WebSocketCloseCode code, string reason)
        {
            Code = code;
            Reason = reason;
        }
    }
}
