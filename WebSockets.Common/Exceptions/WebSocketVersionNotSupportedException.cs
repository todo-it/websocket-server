using System;
using System.Runtime.Serialization;

namespace WebSockets.Common.Exceptions
{
    [Serializable]
    public class WebSocketVersionNotSupportedException : Exception
    {
        public WebSocketVersionNotSupportedException() : base()
        {
            
        }

        public WebSocketVersionNotSupportedException(string message) : base(message)
        {
            
        }

        public WebSocketVersionNotSupportedException(string message, Exception inner) : base(message, inner)
        {

        }

        public WebSocketVersionNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
