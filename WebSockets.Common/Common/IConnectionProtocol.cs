using System;

namespace WebSockets.Common.Common
{
    public interface IConnectionProtocol : IDisposable
    {
        void OnConnectionStarted(IConnectionController ctx);
        void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code);
        void Process(IConnectionController ctx);
    }
}
