using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Ninja.WebSockets.UnitTests
{
    class TheInternet
    {
        public MockNetworkStream ClientNetworkStream { get; private set; }
        public MockNetworkStream ServerNetworkStream { get; private set; }

        public TheInternet()
        {
            MemoryStream clientStream = new MemoryStream();
            MemoryStream serverStream = new MemoryStream();

            ManualResetEventSlim clientReadSlim = new ManualResetEventSlim(false);
            ManualResetEventSlim serverReadSlim = new ManualResetEventSlim(false);
            ManualResetEventSlim clientWriteSlim = new ManualResetEventSlim(true);
            ManualResetEventSlim serverWriteSlim = new ManualResetEventSlim(true);

            ClientNetworkStream = new MockNetworkStream("Client", clientStream, serverStream, clientReadSlim, serverReadSlim, clientWriteSlim, serverWriteSlim);
            ServerNetworkStream = new MockNetworkStream("Server", serverStream, clientStream, serverReadSlim, clientReadSlim, serverWriteSlim, clientWriteSlim);
        }
    }
}
