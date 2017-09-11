namespace WebSockets.Common.Events
{
    public class BinaryMultiFrameEventArgs : BinaryFrameEventArgs
    {
        public bool IsLastFrame { get; private set; }

        public BinaryMultiFrameEventArgs(byte[] payload, bool isLastFrame) : base(payload)
        {
            IsLastFrame = isLastFrame;
        }
    }
}
