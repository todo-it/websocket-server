using System;

namespace WebSockets.Common.Events
{
    public class TextFrameEventArgs : EventArgs
    {
        public string Text { get; }

        public TextFrameEventArgs(string text)
        {
            Text = text;
        }
    }
}
