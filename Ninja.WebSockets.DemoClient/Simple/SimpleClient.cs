using Ninja.WebSockets;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSockets.DemoClient.Simple
{
    class SimpleClient
    {
        public async Task Run()
        {
            var factory = new WebSocketClientFactory();
            using (WebSocket webSocket = await factory.ConnectAsync(new Uri("ws://localhost:27416/chat")))
            {
                Task readTask = Receive(webSocket);
                await Send(webSocket);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                await readTask;
            }           
        }

        private async Task Send(WebSocket webSocket)
        {
            var array = Encoding.UTF8.GetBytes("Hello World");
            var buffer = new ArraySegment<byte>(array);
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task Receive(WebSocket webSocket)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        return;
                    case WebSocketMessageType.Text:
                    case WebSocketMessageType.Binary:
                        string value = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        Console.WriteLine(value);
                        break;
                }
            }
        }
    }
}
