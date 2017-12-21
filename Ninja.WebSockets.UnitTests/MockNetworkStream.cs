using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ninja.WebSockets.UnitTests
{
    class MockNetworkStream : Stream
    {
        private readonly string _streamName;
        private readonly MemoryStream _remoteStream;
        private readonly MemoryStream _localStream;
        private readonly ManualResetEventSlim _localReadSlim;
        private readonly ManualResetEventSlim _remoteReadSlim;
        private readonly ManualResetEventSlim _localWriteSlim;
        private readonly ManualResetEventSlim _remoteWriteSlim;

        public MockNetworkStream(string streamName, 
            MemoryStream localStream,
            MemoryStream remoteStream,
            ManualResetEventSlim localReadSlim,
            ManualResetEventSlim remoteReadSlim, 
            ManualResetEventSlim localWriteSlim,
            ManualResetEventSlim remoteWriteSlim)
        {
            _streamName = streamName;
            _localStream = localStream;
            _remoteStream = remoteStream;
            _localReadSlim = localReadSlim;
            _remoteReadSlim = remoteReadSlim;
            _localWriteSlim = localWriteSlim;
            _remoteWriteSlim = remoteWriteSlim;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _remoteReadSlim.Wait(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            int numBytesRead = await _remoteStream.ReadAsync(buffer, offset, count, cancellationToken);

            if (_remoteStream.Position >= _remoteStream.Length)
            {
                _remoteReadSlim.Reset();
                _remoteWriteSlim.Set();
            }

            return numBytesRead;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _localWriteSlim.Wait(cancellationToken);
            _localWriteSlim.Reset();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            _localStream.Position = 0;
            await _localStream.WriteAsync(buffer, offset, count, cancellationToken);
            _localStream.Position = 0;
            _localReadSlim.Set();
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return _streamName;
        }
    }
}
