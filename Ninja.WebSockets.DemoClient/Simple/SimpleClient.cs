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
            var uri = new Uri("ws://localhost:27416/chat");
            using (WebSocket webSocket = await factory.ConnectAsync(uri))
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
