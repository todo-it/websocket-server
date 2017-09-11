using System;

namespace WebSockets.Common.Events
{
    public class PingEventArgs : EventArgs
    {
        public byte[] Payload { get; }

        public PingEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
