# Ninja WebSockets

A concrete implementation of the .Net Standard 2.0 System.Net.WebSockets.WebSocket abstract class

A WebSocket library that allows you to make WebSocket connections as a client or to respond to WebSocket requests as a server.
You can safely pass around a general purpose WebSocket instance throughout your codebase without tying yourself strongly to this library. This is the same WebSocket abstract class used by .net core 2.0 and it allows for asynchronous Websocket communication for improved performance and scalability.

### Dependencies

No dependencies. 

## Getting Started

As a Client, use the WebSocketClientFactory

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

## Running the tests

Tests are written for xUnit

## Authors

David Haig

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Step by step guide:
  https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers

* The official WebSocket spec:
  http://tools.ietf.org/html/rfc6455
