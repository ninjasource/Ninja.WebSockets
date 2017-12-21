using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ninja.WebSockets.UnitTests
{
    public class TheInternetTests
    {
        [Fact]
        public async Task ClientToServerTest()
        {
            TheInternet theInternet = new TheInternet();
            string expected = "hello world";
            byte[] buffer = Encoding.UTF8.GetBytes(expected);
            byte[] readBuffer = new byte[256];

            await theInternet.ClientNetworkStream.WriteAsync(buffer, 0, buffer.Length);
            int count = await theInternet.ServerNetworkStream.ReadAsync(readBuffer, 0, readBuffer.Length);

            string actual = Encoding.UTF8.GetString(readBuffer, 0, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ServerToClientTest()
        {
            TheInternet theInternet = new TheInternet();
            string expected = "hello world";
            byte[] buffer = Encoding.UTF8.GetBytes(expected);
            byte[] readBuffer = new byte[256];

            await theInternet.ServerNetworkStream.WriteAsync(buffer, 0, buffer.Length);
            int count = await theInternet.ClientNetworkStream.ReadAsync(readBuffer, 0, readBuffer.Length);

            string actual = Encoding.UTF8.GetString(readBuffer, 0, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ClientToServerToClientTest()
        {
            TheInternet theInternet = new TheInternet();
            string expected = "hello world";
            byte[] buffer = Encoding.UTF8.GetBytes(expected);
            byte[] readBuffer = new byte[256];

            await theInternet.ClientNetworkStream.WriteAsync(buffer, 0, buffer.Length);
            int count = await theInternet.ServerNetworkStream.ReadAsync(readBuffer, 0, readBuffer.Length);
            await theInternet.ServerNetworkStream.WriteAsync(readBuffer, 0, count);
            count = await theInternet.ClientNetworkStream.ReadAsync(readBuffer, 0, readBuffer.Length);

            string actual = Encoding.UTF8.GetString(readBuffer, 0, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiTaskEchoTest()
        {
            // This test sends the following 5 messages to the server:
            // "hello world 0"
            // "hello world 1"
            // "hello world 2"
            // "hello world 3"
            // "hello world 4"
            // And expects the following response
            // "Server : hello world 0"
            // "Server : hello world 1"
            // "Server : hello world 2"
            // "Server : hello world 3"
            // "Server : hello world 4"

            TheInternet theInternet = new TheInternet();
            string expected = "hello world";
            const int NumMessagesToSend = 5;
            CancellationTokenSource source = new CancellationTokenSource();

            Task clientSend = Task.Run(async () =>
            {
                for (int i = 0; i < NumMessagesToSend; i++)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes($"{expected} {i}");
                    await theInternet.ClientNetworkStream.WriteAsync(buffer, 0, buffer.Length, source.Token);
                }
            });

            Task<string[]> clientReceive = Task.Run(async () =>
            {
                List<string> replies = new List<string>();
                byte[] buffer = new byte[256];
                int count;
                while ((count = await theInternet.ClientNetworkStream.ReadAsync(buffer, 0, buffer.Length, source.Token)) > 0)
                {
                    string reply = Encoding.UTF8.GetString(buffer, 0, count);
                    replies.Add(reply);
                    if (replies.Count >= NumMessagesToSend)
                    {
                        source.Cancel();
                        break;
                    }
                }

                return replies.ToArray();
            });

            Task serverTask = Task.Run(async () =>
            {
                byte[] buffer = new byte[256];
                while (!source.Token.IsCancellationRequested)
                {
                    int count = await theInternet.ServerNetworkStream.ReadAsync(buffer, 0, buffer.Length, source.Token);
                    string message = Encoding.UTF8.GetString(buffer, 0, count);
                    message = "Server: " + message;
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(message);
                    await theInternet.ServerNetworkStream.WriteAsync(sendBuffer, 0, sendBuffer.Length, source.Token);
                }
            });

            Task.WaitAll(clientReceive, clientSend);

            string[] results = clientReceive.Result;
            Assert.Equal(NumMessagesToSend, results.Length);
            for(int i=0; i<NumMessagesToSend; i++)
            {
                Assert.Equal($"Server: {expected} {i}", results[i]);                
            }
        }
    }
}
