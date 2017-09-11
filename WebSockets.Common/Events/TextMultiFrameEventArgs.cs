namespace WebSockets.Common.Events
{
    public class TextMultiFrameEventArgs : TextFrameEventArgs
    {
        public bool IsLastFrame { get; private set; }

        public TextMultiFrameEventArgs(string text, bool isLastFrame) : base(text)
        {
            IsLastFrame = isLastFrame;
        }
    }
}
