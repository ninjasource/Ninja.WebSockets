using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ninja.WebSockets
{
    /// <summary>
    /// This buffer pool is instance thread safe
    /// Use GetBuffer to get a MemoryStream (with a publically accessible buffer)
    /// Calling Dispose on this MemoryStream will clear its internal buffer and return the buffer to the pool for reuse
    /// </summary>
    public class BufferPool : IBufferPool
    {
        const int DEFAULT_BUFFER_SIZE = 16384;
        private readonly ConcurrentStack<byte[]> _bufferPoolStack;
        private readonly int _bufferSize;

        public BufferPool() : this(DEFAULT_BUFFER_SIZE)
        {
        }

        public BufferPool(int bufferSize)
        {
            _bufferSize = bufferSize;
            _bufferPoolStack = new ConcurrentStack<byte[]>();
        }

        protected class PublicBufferMemoryStream : MemoryStream
        {
            private readonly BufferPool _bufferPoolInternal;
            private readonly object _locker = new object();
            private readonly byte[] _buffer;
            private bool _isDisposed = false;

            public PublicBufferMemoryStream(byte[] buffer, BufferPool bufferPool) : base(buffer, 0, buffer.Length, true, true)
            {
                _bufferPoolInternal = bufferPool;
                _buffer = buffer;
            }

            protected override void Dispose(bool disposing)
            {
                // just in case someone tries to call dispose from multiple threads on the same instance
                lock (_locker)
                {
                    if (disposing && !_isDisposed)
                    {
                        _isDisposed = true;

                        // clear the buffer - we only need to clear up to the number of bytes we have already written
                        Array.Clear(_buffer, 0, (int)this.Position);

                        // return the buffer to the pool
                        _bufferPoolInternal.ReturnBuffer(_buffer);

                        base.Dispose(disposing);
                    }
                }
            }
        }

        public MemoryStream GetBuffer()
        {
            if (!_bufferPoolStack.TryPop(out byte[] buffer))
            {
                buffer = new byte[_bufferSize];
            }

            return new PublicBufferMemoryStream(buffer, this);
        }

        protected void ReturnBuffer(byte[] buffer)
        {
            _bufferPoolStack.Push(buffer);
        }
    }
}
