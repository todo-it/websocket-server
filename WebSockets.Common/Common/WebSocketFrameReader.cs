﻿using System;
using System.IO;
using System.Net.Sockets;

namespace WebSockets.Common.Common
{
    //  see http://tools.ietf.org/html/rfc6455 for specification

    public class WebSocketFrameReader
    {
        private byte[] _buffer;

        public WebSocketFrameReader()
        {
            _buffer = new byte[1024*64];
        }

        public WebSocketFrame Read(Stream stream, Socket socket)
        {
            byte byte1;

            try
            {
                byte1 = (byte) stream.ReadByte();
            }
            catch (IOException)
            {
                if (socket.Connected)
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }

            // process first byte
            byte finBitFlag = 0x80;
            byte opCodeFlag = 0x0F;
            var isFinBitSet = (byte1 & finBitFlag) == finBitFlag;
            var opCode = (WebSocketOpCode) (byte1 & opCodeFlag);

            // read and process second byte
            var byte2 = (byte) stream.ReadByte();
            byte maskFlag = 0x80;
            var isMaskBitSet = (byte2 & maskFlag) == maskFlag;
            var len = ReadLength(byte2, stream);
            byte[] decodedPayload;

            // use the masking key to decode the data if needed
            if (isMaskBitSet)
            {
                const int maskKeyLen = 4;
                var maskKey = BinaryReaderWriter.ReadExactly(maskKeyLen, stream);
                var encodedPayload = BinaryReaderWriter.ReadExactly((int) len, stream);
                decodedPayload = new byte[len];

                // apply the mask key
                for (var i = 0; i < encodedPayload.Length; i++)
                {
                    decodedPayload[i] = (Byte) (encodedPayload[i] ^ maskKey[i%maskKeyLen]);
                }
            }
            else
            {
                decodedPayload = BinaryReaderWriter.ReadExactly((int) len, stream);
            }

            var frame = new WebSocketFrame(isFinBitSet, opCode, decodedPayload, true);
            return frame;
        }

        private static uint ReadLength(byte byte2, Stream stream)
        {
            byte payloadLenFlag = 0x7F;
            var len = (uint) (byte2 & payloadLenFlag);

            // read a short length or a long length depending on the value of len
            if (len == 126)
            {
                len = BinaryReaderWriter.ReadUShortExactly(stream, false);
            }
            else if (len == 127)
            {
                len = (uint) BinaryReaderWriter.ReadULongExactly(stream, false);
                const uint maxLen = 2147483648; // 2GB

                // protect ourselves against bad data
                if (len > maxLen || len < 0)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Payload length out of range. Min 0 max 2GB. Actual {0:#,##0} bytes.", len));
                }
            }

            return len;
        }
    }
}
