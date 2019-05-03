using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ninja.WebSockets.Internal;
using Xunit;

namespace Ninja.WebSockets.UnitTests
{
    public class WebSocketClientTests
    {
        [Fact]
        public async Task CanCancelReceive()
        {
            Func<MemoryStream> memoryStreamFactory = () => new MemoryStream();
            var theInternet = new TheInternet();
            var webSocketClient = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, theInternet.ClientNetworkStream, TimeSpan.Zero, null, false, true, null);
            var webSocketServer = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, theInternet.ServerNetworkStream, TimeSpan.Zero, null, false, false, null);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[10]);

            tokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => webSocketClient.ReceiveAsync(buffer, tokenSource.Token));
        }

        [Fact]
        public async Task CanCancelSend()
        {
            Func<MemoryStream> memoryStreamFactory = () => new MemoryStream();
            var theInternet = new TheInternet();
            var webSocketClient = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, theInternet.ClientNetworkStream, TimeSpan.Zero, null, false, true, null);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[10]);

            tokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => webSocketClient.SendAsync(buffer, WebSocketMessageType.Binary, true, tokenSource.Token));
        }

        [Fact]
        public async Task SimpleSend()
        {
            Func<MemoryStream> memoryStreamFactory = () => new MemoryStream();
            var theInternet = new TheInternet();
            var webSocketClient = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, theInternet.ClientNetworkStream, TimeSpan.Zero, null, false, true, null);
            var webSocketServer = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, theInternet.ServerNetworkStream, TimeSpan.Zero, null, false, false, null);
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var clientReceiveTask = Task.Run<string[]>(() => ReceiveClient(webSocketClient, tokenSource.Token));
            var serverReceiveTask = Task.Run(() => ReceiveServer(webSocketServer, tokenSource.Token));

            ArraySegment<byte> message1 = GetBuffer("Hi");
            ArraySegment<byte> message2 = GetBuffer("There");

            await webSocketClient.SendAsync(message1, WebSocketMessageType.Binary, true, tokenSource.Token);
            await webSocketClient.SendAsync(message2, WebSocketMessageType.Binary, true, tokenSource.Token);
            await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, tokenSource.Token);

            string[] replies = await clientReceiveTask;
            foreach (string reply in replies)
            {
                Console.WriteLine(reply);
            }
        }
    
        [Fact]
        public async Task SimpleNamedPipes()
        {
            Func<MemoryStream> memoryStreamFactory = () => new MemoryStream();
            string pipeName = Guid.NewGuid().ToString();
            using (var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            using (var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {                
                Task clientConnectTask = clientPipe.ConnectAsync();
                Task serverConnectTask = serverPipe.WaitForConnectionAsync();
                Task.WaitAll(clientConnectTask, serverConnectTask);

                var webSocketClient = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, clientPipe, TimeSpan.Zero, null, false, true, null);
                var webSocketServer = new WebSocketImplementation(Guid.NewGuid(), memoryStreamFactory, serverPipe, TimeSpan.Zero, null, false, false, null);
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var clientReceiveTask = Task.Run<string[]>(() => ReceiveClient(webSocketClient, tokenSource.Token));
                var serverReceiveTask = Task.Run(() => ReceiveServer(webSocketServer, tokenSource.Token));

                ArraySegment<byte> message1 = GetBuffer("Hi");
                ArraySegment<byte> message2 = GetBuffer("There");

                await webSocketClient.SendAsync(message1, WebSocketMessageType.Binary, true, tokenSource.Token);
                await webSocketClient.SendAsync(message2, WebSocketMessageType.Binary, true, tokenSource.Token);
                await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, tokenSource.Token);

                string[] replies = await clientReceiveTask;
                foreach (string reply in replies)
                {
                    Console.WriteLine(reply);
                }
            }
        }

        private ArraySegment<byte> GetBuffer(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return new ArraySegment<byte>(buffer, 0, buffer.Length);
        }

        public async Task<string[]> ReceiveClient(WebSocket webSocket, CancellationToken cancellationToken)
        {
            List<string> values = new List<string>();

            byte[] array = new byte[256];
            var buffer = new ArraySegment<byte>(array);
            
            while(true)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                string value = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                values.Add(value);
            }

            return values.ToArray();
        }

        public async Task ReceiveServer(WebSocket webSocket, CancellationToken cancellationToken)
        {
            byte[] array = new byte[256];
            var buffer = new ArraySegment<byte>(array);

            while (true)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                string value = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                string reply = "Server: " + value;
                byte[] toSend = Encoding.UTF8.GetBytes(reply);
                await webSocket.SendAsync(new ArraySegment<byte>(toSend, 0, toSend.Length), WebSocketMessageType.Binary, true, cancellationToken);
            }
        }
    }
}
