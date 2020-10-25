using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ninja.WebSockets.UnitTests
{
    // Thanks Esbjörn for adding this unit test!!
    public class LargeMessageTests
    {
        private class Server : IDisposable
        {
            private HttpListener _listener;
            private Task _connectionPointTask;
            public Uri Address { get; private set; }
            public readonly List<byte[]> ReceivedMessages = new List<byte[]>();
            private WebSocket _webSocket;
            public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

            public Server()
            {
                var os = Environment.OSVersion.Version;
                if (os.Major < 6 || os.Major == 6 && os.Minor < 2)
                {
                    throw new InvalidOperationException(
                        "Cannot create server - running on operating system that doesn't support native web sockets...");
                }
            }

            public void StartListener()
            {
                if (_listener != null)
                {
                    throw new InvalidOperationException("Listener already started.");
                }

                // Create new listener
                var usedPorts = new HashSet<int>(IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(a => a.Port));

                for (int i = 49152; i <= 65535; i++)
                {
                    if (usedPorts.Contains(i))
                    {
                        continue;
                    }

                    var listener = new HttpListener();

                    listener.Prefixes.Add($"http://localhost:{i}/");
                    try
                    {
                        listener.Start();
                        Address = new Uri($"ws://localhost:{i}/");
                        _listener = listener;
                        _connectionPointTask = Task.Run(ConnectionPoint);
                        break;
                    }
                    catch (HttpListenerException)
                    {
                        // right - for some reason we couldn't connect. Try the next port
                    }
                }

                if (_listener == null)
                {
                    throw new InvalidOperationException("Could not find free port to bind to.");
                }
            }

            private async Task ConnectionPoint()
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = webSocketContext.WebSocket;
                        _webSocket = webSocket;
                        byte[] receiveBuffer = new byte[4096];

                        MemoryStream stream = new MemoryStream();


                        while (webSocket.State == WebSocketState.Open)
                        {
                            var arraySegment = new ArraySegment<byte>(receiveBuffer);
                            var received = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);

                            switch (received.MessageType)
                            {
                                case WebSocketMessageType.Close:
                                    {
                                        if (webSocket.State == WebSocketState.CloseReceived)
                                        {
                                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ok",
                                                CancellationToken.None);
                                        }

                                        break;
                                    }
                                
                                case WebSocketMessageType.Binary:
                                    {
                                        stream.Write(arraySegment.Array, arraySegment.Offset, received.Count);
                                        if (received.EndOfMessage)
                                        {
                                            ReceivedMessages.Add(stream.ToArray());
                                            stream = new MemoryStream();
                                        }

                                        break;
                                    }
                            }
                        }
                    }
                }
                catch (HttpListenerException)
                {
                    // This would happen when the server was stopped for instance.
                }
            }

            public void Dispose()
            {
                _listener.Stop();
                _connectionPointTask.Wait();
            }
        }

        private async Task SendBinaryMessage(WebSocket client, byte[] message, int sendBufferLength)
        {
            if (message.Length > 0)
            {
                // copy data so that masking doesn't affect the original message
                var data = message.ToArray();

                for (int i = 0; i <= (data.Length - 1) / sendBufferLength; i++)
                {
                    int start = i * sendBufferLength;
                    int nextStart = Math.Min(start + sendBufferLength, data.Length);
                    ArraySegment<byte> seg = new ArraySegment<byte>(data, start, nextStart - start);
                    await client.SendAsync(seg, WebSocketMessageType.Binary, nextStart == data.Length,
                        CancellationToken.None);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendLargeBinaryMessage(bool useNinja)
        {
            using (var server = new Server())
            {
                server.StartListener();

                // Create client
                WebSocket webSocket;
                if (useNinja)
                {
                    var factory = new WebSocketClientFactory();
                    webSocket = await factory.ConnectAsync(server.Address, new WebSocketClientOptions(), CancellationToken.None);
                }
                else
                {
                    var clientWebSocket = new ClientWebSocket();
                    await clientWebSocket.ConnectAsync(server.Address, CancellationToken.None);
                    webSocket = clientWebSocket;
                }

                // Send large message
                byte[] message = new byte[10000];
                new Random().NextBytes(message);
                await SendBinaryMessage(webSocket, message, 1024);

                // Close
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);

                // Wait for the server to receive our close message
                var stopwatch = Stopwatch.StartNew();
                while (server.State == WebSocketState.Open)
                {
                    await Task.Delay(5);
                    if (stopwatch.Elapsed.TotalSeconds > 10)
                    {
                        throw new TimeoutException("Timeout expired after waiting for close handshake to complete");
                    }
                }
                
                Assert.Single(server.ReceivedMessages);
                Assert.Equal(message, server.ReceivedMessages[0]);
            }
        }
    }
}
