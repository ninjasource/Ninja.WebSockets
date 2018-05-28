# Ninja WebSockets

A concrete implementation of the .Net Standard 2.0 System.Net.WebSockets.WebSocket abstract class

A WebSocket library that allows you to make WebSocket connections as a client or to respond to WebSocket requests as a server.
You can safely pass around a general purpose WebSocket instance throughout your codebase without tying yourself strongly to this library. This is the same WebSocket abstract class used by .net core 2.0 and it allows for asynchronous Websocket communication for improved performance and scalability.

### Dependencies

No dependencies. 

## Getting Started

As a client, use the WebSocketClientFactory

```csharp
var factory = new WebSocketClientFactory();
WebSocket webSocket = await factory.ConnectAsync(new Uri("wss://example.com"));
```

As a server, use the WebSocketServerFactory

```csharp
Stream stream = tcpClient.GetStream();
var factory = new WebSocketServerFactory();
WebSocketHttpContext context = await factory.ReadHttpHeaderFromStreamAsync(stream);

if (context.IsWebSocketRequest)
{
    WebSocket webSocket = await factory.AcceptWebSocketAsync(context);
}
```
## Using the WebSocket class

Client and Server send and receive data the same way.

Receiving Data:

```csharp
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
```

Receive data in an infinite loop until we receive a close frame from the server.

Sending Data:
```csharp
private async Task Send(WebSocket webSocket)
{
    var array = Encoding.UTF8.GetBytes("Hello World");
    var buffer = new ArraySegment<byte>(array);
    await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
} 
```

Simple client Request / Response:
The best approach to communicating using a web socket is to send and receive data on different worker threads as shown below. 

```csharp
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
```

## WebSocket Extensions

Websocket extensions like compression (per message deflate) is currently work in progress.

## Running the tests

Tests are written for xUnit

## Authors

David Haig

## License

This project is licensed under the MIT License - see the LICENSE.md file for details

## Acknowledgments

* Step by step guide:
  https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers

* The official WebSocket spec:
  http://tools.ietf.org/html/rfc6455

## Further Reading

This library is based on all the amazing feedback I got after writing this article (thanks all):
https://www.codeproject.com/articles/1063910/websocket-server-in-csharp

The code in the article above was written before Microsoft made System.Net.WebSockets.WebSocket generally available with .NetStandard 2.0 but the concepts remain the same. Take a look if you are interested in the inner workings of the websocket protocol.
