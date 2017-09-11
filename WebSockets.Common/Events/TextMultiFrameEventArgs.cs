namespace WebSockets.Common.Events
{
    public class TextMultiFrameEventArgs : TextFrameEventArgs
    {
        public bool IsLastFrame { get; }

        public TextMultiFrameEventArgs(string text, bool isLastFrame) : base(text)
        {
            IsLastFrame = isLastFrame;
        }
    }
}
