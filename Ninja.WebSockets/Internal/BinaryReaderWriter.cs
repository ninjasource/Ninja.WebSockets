﻿// ---------------------------------------------------------------------
// Copyright 2017 David Haig
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// ---------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ninja.WebSockets.Internal
{
    internal class BinaryReaderWriter
    {
        public static async Task ReadExactly(int length, Stream stream, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (length == 0)
            {
                return;
            }

            if (buffer.Count < length)
            {
                // TODO: it is not impossible to get rid of this, just a little tricky
                // if the supplied buffer is too small for the payload then we should only return the number of bytes in the buffer
                // this will have to propogate all the way up the chain
                throw new InternalBufferOverflowException($"Unable to read {length} bytes into buffer (offset: {buffer.Offset} size: {buffer.Count}). Use a larger read buffer");
            }

            int offset = 0;
            do
            {
                int bytesRead = await stream.ReadAsync(buffer.Array, buffer.Offset + offset, length - offset, cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException($"Unexpected end of stream encountered whilst attempting to read {length:#,##0} bytes");
                }

                offset += bytesRead;
            } while (offset < length);
        }

        public static async Task<ushort> ReadUShortExactly(Stream stream, bool isLittleEndian, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await ReadExactly(2, stream, buffer, cancellationToken).ConfigureAwait(false);

            if (!isLittleEndian)
            {
                Array.Reverse(buffer.Array, buffer.Offset, 2); // big endian
            }

            return BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        }

        public static async Task<ulong> ReadULongExactly(Stream stream, bool isLittleEndian, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await ReadExactly(8, stream, buffer, cancellationToken).ConfigureAwait(false);

            if (!isLittleEndian)
            {
                Array.Reverse(buffer.Array, buffer.Offset, 8); // big endian
            }

            return BitConverter.ToUInt64(buffer.Array, buffer.Offset);
        }

        public static async Task<long> ReadLongExactly(Stream stream, bool isLittleEndian, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await ReadExactly(8, stream, buffer, cancellationToken).ConfigureAwait(false);

            if (!isLittleEndian)
            {
                Array.Reverse(buffer.Array, buffer.Offset, 8); // big endian
            }

            return BitConverter.ToInt64(buffer.Array, buffer.Offset);
        }

        public static void WriteInt(int value, Stream stream, bool isLittleEndian)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteULong(ulong value, Stream stream, bool isLittleEndian)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && ! isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteLong(long value, Stream stream, bool isLittleEndian)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteUShort(ushort value, Stream stream, bool isLittleEndian)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian && !isLittleEndian)
            {
                Array.Reverse(buffer);
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
