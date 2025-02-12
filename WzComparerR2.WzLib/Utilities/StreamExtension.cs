using System;
using System.Buffers;
using System.IO;

namespace WzComparerR2.WzLib.Utilities
{
    public static class StreamExtension
    {
#if NETFRAMEWORK
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int numRead = stream.Read(sharedBuffer, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException();
                }
                new Span<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
#endif

#if NETFRAMEWORK || NET6_0
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int actual = stream.Read(buffer, offset, count);
                if (actual == 0)
                    throw new System.IO.EndOfStreamException();
                offset += actual;
                count -= actual;
            }
        }
#endif

#if NETFRAMEWORK
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(Math.Min(buffer.Length, 16384));
            try
            {
                while (buffer.Length > 0)
                {
                    int actual = stream.Read(sharedBuffer, 0, Math.Min(sharedBuffer.Length, buffer.Length));
                    if (actual == 0)
                        throw new System.IO.EndOfStreamException();
                    new Span<byte>(sharedBuffer, 0, actual).CopyTo(buffer);
                    buffer = buffer.Slice(actual);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
#elif NET6_0
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                int actual = stream.Read(buffer);
                if (actual == 0)
                    throw new System.IO.EndOfStreamException();
                buffer = buffer.Slice(actual);
            }
        }
#endif

        public static int ReadAvailableBytes(this Stream stream, Span<byte> buffer)
        {
            int totalRead = 0;
            while (buffer.Length > 0)
            {
                int bytesRead = stream.Read(buffer);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
                buffer = buffer.Slice(bytesRead);
            }
            return totalRead;
        }
    }
}
