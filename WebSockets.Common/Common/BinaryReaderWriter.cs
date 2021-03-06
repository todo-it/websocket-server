﻿using System;
using System.IO;

namespace WebSockets.Common.Common
{
    public class BinaryReaderWriter
    {
        public static byte[] ReadExactly(int length, Stream stream)
        {
            var buffer = new byte[length];
            if (length == 0)
            {
                return buffer;
            }

            var offset = 0;
            do
            {
                var bytesRead = stream.Read(buffer, offset, length - offset);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException(string.Format("Unexpected end of stream encountered whilst attempting to read {0:#,##0} bytes", length));
                }

                offset += bytesRead;
            } while (offset < length);

            return buffer;
        }

        public static ushort ReadUShortExactly(Stream stream, bool isLittleEndian)
        {
            var lenBuffer = ReadExactly(2, stream);

            if (!isLittleEndian)
            {
                Array.Reverse(lenBuffer); // big endian
            }

            return BitConverter.ToUInt16(lenBuffer, 0);
        }

        public static ulong ReadULongExactly(Stream stream, bool isLittleEndian)
        {
            var lenBuffer = ReadExactly(8, stream);

            if (!isLittleEndian)
            {
                Array.Reverse(lenBuffer); // big endian
            }

            return BitConverter.ToUInt64(lenBuffer, 0);
        }

        public static long ReadLongExactly(Stream stream, bool isLittleEndian)
        {
            var lenBuffer = ReadExactly(8, stream);

            if (!isLittleEndian)
            {
                Array.Reverse(lenBuffer); // big endian
            }

            return BitConverter.ToInt64(lenBuffer, 0);
        }

        public static void WriteULong(ulong value, Stream stream, bool isLittleEndian)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && ! isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteLong(long value, Stream stream, bool isLittleEndian)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteUShort(ushort value, Stream stream, bool isLittleEndian)
        {
            var buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
