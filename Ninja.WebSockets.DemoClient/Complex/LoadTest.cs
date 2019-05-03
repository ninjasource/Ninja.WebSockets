using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ninja.WebSockets.DemoClient.Complex
{
    // This test sends a large buffer
    // NOTE: you would never normally do this. In order to send a large amount of data use a small buffer and make multiple calls
    // to SendAsync with endOfMessage false and the last SendAsync function call with endOfMessage set to true.
    class LoadTest
    {
        const int BUFFER_SIZE = 1 * 1024 * 1024 * 1024; // 1GB

        public async Task Run()
        {
            var factory = new WebSocketClientFactory();
            var uri = new Uri("ws://localhost:27416/chat");
            var options = new WebSocketClientOptions() { KeepAliveInterval = TimeSpan.FromMilliseconds(500) };
            using (WebSocket webSocket = await factory.ConnectAsync(uri, options))
            {
                // receive loop
                Task readTask = Receive(webSocket);

                // send a message
                await Send(webSocket);

                // initiate the close handshake
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

                // wait for server to respond with a close frame
                await readTask;
            }
        }

        private async Task Send(WebSocket webSocket)
        {            
            var array = new byte[BUFFER_SIZE];
            var buffer = new ArraySegment<byte>(array);
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<long> ReadAll(WebSocket webSocket)
        {
            var buffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
            long len = 0;
            while (true)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        return len;
                    case WebSocketMessageType.Text:
                    case WebSocketMessageType.Binary:
                        len += result.Count;
                        break;
                }
            }
        }

        private async Task Receive(WebSocket webSocket)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var len = await ReadAll(webSocket);
            Console.WriteLine($"Read {len:#,##0} bytes in {stopwatch.Elapsed.TotalMilliseconds:#,##0} ms");
        }
    }
}
