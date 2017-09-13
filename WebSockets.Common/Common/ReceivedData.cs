namespace WebSockets.Common.Common
{
    public class ReceivedData
    {
        public bool IsText { get; }
        public bool IsBinary => !IsText;

        public string Text {get; }
        public byte[] Binary { get; }
        public MultiFrame? Multi { get; }

        private ReceivedData(bool isText, string text, byte[] binary, MultiFrame? multi)
        {
            IsText = isText;
            Text = text;
            Binary = binary;
            Multi = multi;
        }

        public static ReceivedData CreateText(string text, MultiFrame? multi)
        {
            return new ReceivedData(true, text, null, multi);
        }

        public static ReceivedData CreateBinary(byte[] binary, MultiFrame? multi)
        {
            return new ReceivedData(false, null, binary, multi);
        }

        public override string ToString()
        {
            return $"<ReceivedData IsText={IsText} Text=[{Text ?? ""}] BinaryLen=[{Binary?.Length}] Multi={Multi}>";
        }
    }
}
