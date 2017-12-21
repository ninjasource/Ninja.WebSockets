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
            using (WebSocket webSocket = await factory.ConnectAsync(new Uri("ws://localhost:17823")))
            {
                var readTask = new Task(async () =>
                {
                    var readBuffer = new ArraySegment<byte>(new byte[1024]);
                    while (true)
                    {
                        WebSocketReceiveResult readResult = await webSocket.ReceiveAsync(readBuffer, CancellationToken.None);
                        switch (readResult.MessageType)
                        {
                            case WebSocketMessageType.Close:
                                return;
                            case WebSocketMessageType.Text:
                            case WebSocketMessageType.Binary:
                                string value = Encoding.UTF8.GetString(readBuffer.Array, 0, readResult.Count);
                                Console.WriteLine(value);
                                break;
                        }
                    }
                });
                
                for (int i=0; i<10; i++)
                {
                    var writeBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes($"Test{i}"));
                    await webSocket.SendAsync(writeBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                await readTask;
            }           
        }
    }
}
