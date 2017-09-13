using System;

namespace WebSockets.Common.Common
{
    public interface IWebSocketLogger
    {
        void Debug(Type type, string format, params object[] args);
        void Info(Type type, string format, params object[] args);
        void Warn(Type type, string format, params object[] args);
        void Err(Type type, string format, params object[] args);
        void Err(Type type, Exception exception);
    }
}
