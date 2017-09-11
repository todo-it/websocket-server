namespace WebSockets.Common.Common
{
    public class WebSocketFrame
    {
        public bool IsFinBitSet { get; }
        public WebSocketOpCode OpCode { get; }
        public byte[] DecodedPayload { get; }
        public bool IsValid { get; }

        public WebSocketFrame(bool isFinBitSet, WebSocketOpCode webSocketOpCode, byte[] decodedPayload, bool isValid)
        {
            IsFinBitSet = isFinBitSet;
            OpCode = webSocketOpCode;
            DecodedPayload = decodedPayload;
            IsValid = isValid;
        }
    }
}
