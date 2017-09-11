using System;

namespace WebSockets.Common.Events
{
    public class PongEventArgs : EventArgs
    {
        public byte[] Payload { get; }

        public PongEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
