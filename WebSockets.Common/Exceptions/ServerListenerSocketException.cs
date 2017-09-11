using System;
using System.Runtime.Serialization;

namespace WebSockets.Common.Exceptions
{
    [Serializable]
    public class ServerListenerSocketException : Exception
    {
        public ServerListenerSocketException() : base()
        {
            
        }

        public ServerListenerSocketException(string message) : base(message)
        {
            
        }

        public ServerListenerSocketException(string message, Exception inner) : base(message, inner)
        {

        }

        public ServerListenerSocketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
