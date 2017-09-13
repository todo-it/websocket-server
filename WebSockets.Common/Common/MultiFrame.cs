namespace WebSockets.Common.Common
{
    public enum MultiFrame
    {
        NonLastFrame,
        LastFrame
    }

    public static class MultiFrameExtensions {
        public static MultiFrame FromIsFinBit(bool finBit)
        {
            return finBit ? MultiFrame.LastFrame : MultiFrame.NonLastFrame;
        }
    }
}
