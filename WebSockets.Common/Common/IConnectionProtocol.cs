namespace WebSockets.Common.Common
{
    public interface IConnectionProtocol
    {
        void OnConnectionStarted(IConnectionController ctx);
        void OnConnectionClosed(IConnectionController ctx, WebSocketCloseCode code, string reason);
        void Process(IConnectionController ctx);
    }
}
