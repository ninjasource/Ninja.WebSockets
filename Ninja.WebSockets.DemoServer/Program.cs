using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Ninja.WebSockets;

namespace WebSockets.DemoServer
{
    class Program
    {
        static ILogger _logger;
        static ILoggerFactory _loggerFactory;
        static IWebSocketServerFactory _webSocketServerFactory;
        static RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        static void Main(string[] args)
        {
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole(LogLevel.Trace);
            _logger = _loggerFactory.CreateLogger<Program>();
            const int DefaultBlockSize = 16 * 1024;
            const int MaxBufferSize = 128 * 1024;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager(DefaultBlockSize, 4, MaxBufferSize);
            _webSocketServerFactory = new WebSocketServerFactory(_recyclableMemoryStreamManager.GetStream);
            Task task = StartWebServer();
            task.Wait();
        }

        static async Task StartWebServer()
        {
            try
            {
                int port = 27416;

                using (WebServer server = new WebServer(_webSocketServerFactory, _loggerFactory))
                {
                    await server.Listen(port);
                    _logger.LogInformation($"Listening on port {port}");
                    _logger.LogInformation("Press any key to quit");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                Console.ReadKey();
            }
        }
    }
}
