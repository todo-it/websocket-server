using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace WebSockets.Common.Common
{
    public abstract class WebSocketBase : IConnectionController
    {
        private readonly IWebSocketLogger _logger;
        private readonly object _rcvLock = new object();
        private readonly object _sendLock = new object();
        private Stream _stream;
        private WebSocketOpCode _multiFrameOpcode;
        private Socket _socket;
        private volatile bool _isOpen; //NOTE: volatile is needed because access to _isOpen doesn't always happens after lock (that internally creates memory barrier)
        private readonly WebSocketFrameReader _reader = new WebSocketFrameReader();
        private readonly WebSocketFrameWriter _writer = new WebSocketFrameWriter();
        private readonly IConnectionProtocol _protocol;
        private bool _onClosedCalled = false;

        protected bool IsOpen
        {
            get { return _isOpen; }
            set { _isOpen = value; }
        }

        protected WebSocketBase(IWebSocketLogger logger, IConnectionProtocol protocol)
        {
            _logger = logger;
            _protocol = protocol;
            _isOpen = false;
        }

        public void CloseConnection(WebSocketCloseCode code)
        {
            if (!_onClosedCalled)
            {
                _onClosedCalled = true;
                _protocol.OnConnectionClosed(this, code);    
            }
            
            CloseConnectionImpl(code);    
        }

        protected abstract void CloseConnectionImpl(WebSocketCloseCode code);
        
        public void Send(WebSocketOpCode opCode, byte[] input, bool isLastFrame = true)
        {
            RawSend(opCode, input, isLastFrame);
        }
        
        protected void ProcessBlocking(Stream stream, Socket socket)
        {
            _socket = socket;
            _stream = stream;
            
            _protocol.OnConnectionStarted(this);
            _isOpen = true; //if above line thrown exception then connection is not really open as one cannot send valid frames
            
            _protocol.Process(this);
            if (!_onClosedCalled)
            {
                _onClosedCalled = true;
                _protocol.OnConnectionClosed(this, WebSocketCloseCode.Normal);
            }
        }

        protected virtual void RawSend(WebSocketOpCode opCode, byte[] toSend, bool isLastFrame)
        {
            lock (_sendLock)
            {
                if (!_isOpen)
                {
                    throw new ArgumentException("Could not send data because connection is not open");   
                }
                
                _writer.Write(_stream, opCode, toSend, isLastFrame);
            }
        }

        protected virtual void RawSend(WebSocketOpCode opCode, byte[] toSend)
        {
            RawSend(opCode, toSend, true);
        }
        
        protected Tuple<WebSocketCloseCode,string> GetConnectionCloseCodeAndReason(byte[] payload)
        {
            if (payload.Length < 2)
            {
                return Tuple.Create(WebSocketCloseCode.Normal, (string)null);
            }

            using (var stream = new MemoryStream(payload))
            {
                var code = BinaryReaderWriter.ReadUShortExactly(stream, false);

                try
                {
                    var closeCode = (WebSocketCloseCode)code;

                    return payload.Length > 2 ? 
                            Tuple.Create(closeCode, Encoding.UTF8.GetString(payload, 2, payload.Length - 2)) 
                        : 
                            Tuple.Create(closeCode, (string)null);
                }
                catch (InvalidCastException)
                {
                    _logger.Warn(GetType(), "Close code {0} not recognised", code);
                    return Tuple.Create(WebSocketCloseCode.Normal, (string)null);
                }
            }
        }
        
        public ReceivedData ReceiveOrNull()
        {
            WebSocketFrame frame;

            lock (_rcvLock)
            {
                if (!IsOpen)
                {
                    throw new InvalidOperationException("Cannot receive from close connection");
                }

                frame = _reader.SafeReadValidOrNull(_stream, _socket);    
            }
            
            if (frame == null)
            {
                IsOpen = false;
                return null;
            }
                
            switch (frame.OpCode)
            {
                case WebSocketOpCode.ContinuationFrame:
                    var multi = MultiFrameExtensions.FromIsFinBit(frame.IsFinBitSet);

                    switch (_multiFrameOpcode)
                    {
                        case WebSocketOpCode.TextFrame:
                            var contData = Encoding.UTF8.GetString(frame.DecodedPayload, 0, frame.DecodedPayload.Length);
                            return ReceivedData.CreateText(contData, multi);

                        case WebSocketOpCode.BinaryFrame:
                            return ReceivedData.CreateBinary(frame.DecodedPayload, multi);

                        default:
                            throw new ArgumentException("Unsupported frame as mulitframe");
                    }

                case WebSocketOpCode.ConnectionClose:
                    IsOpen = false;
                    var codeAndReason = GetConnectionCloseCodeAndReason(frame.DecodedPayload);
                    return null;

                case WebSocketOpCode.Ping:
                    RawSend(WebSocketOpCode.Pong, frame.DecodedPayload); //resend to client
                    return null;

                case WebSocketOpCode.Pong:
                    //nothing to do
                    return null;

                case WebSocketOpCode.TextFrame:
                    var data = Encoding.UTF8.GetString(frame.DecodedPayload, 0, frame.DecodedPayload.Length);
                    if (!frame.IsFinBitSet)
                    {
                        _multiFrameOpcode = frame.OpCode;
                    }
                    
                    return ReceivedData.CreateText(
                        data, 
                        frame.IsFinBitSet ? (MultiFrame?)null : MultiFrameExtensions.FromIsFinBit(frame.IsFinBitSet));
                    
                case WebSocketOpCode.BinaryFrame:
                    if (!frame.IsFinBitSet)
                    {
                        _multiFrameOpcode = frame.OpCode;
                    }
                        
                    return ReceivedData.CreateBinary(
                        frame.DecodedPayload, 
                        frame.IsFinBitSet ? (MultiFrame?)null : MultiFrameExtensions.FromIsFinBit(frame.IsFinBitSet));
                    
                default:
                    throw new ArgumentException("Unrecognized frame");
            }
        }
    }
}
