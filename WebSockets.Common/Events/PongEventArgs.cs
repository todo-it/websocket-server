using System;

namespace WebSockets.Common.Events
{
    public class PongEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }

        public PongEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
