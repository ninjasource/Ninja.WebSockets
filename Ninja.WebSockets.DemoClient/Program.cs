using Ninja.WebSockets.DemoClient.Complex;
using System;
using System.Threading.Tasks;
using WebSockets.DemoClient.Complex;
using WebSockets.DemoClient.Simple;

namespace WebSockets.DemoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                RunSimpleTest().Wait();
            }
            else if (args.Length == 5)
            {
                // TODO: allow buffer pool to grow its buffers
                // ws://localhost:27416/echo 5 1000 5000 40000
                RunComplexTest(args);
            }
            else
            {
                Console.WriteLine("Wrong number of arguments. 0 for simple test. 5 for complex test.");
                Console.WriteLine($"Complex Test: uri numThreads numItemsPerThread minNumBytesPerMessage maxNumBytesPerMessage");
                Console.WriteLine("e.g: ws://localhost:27416/chat/echo 5 100 4 4");
            }

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

        private static async Task RunLoadTest()
        {
            var client = new LoadTest();
            await client.Run();
        }

        private static void RunComplexTest(string[] args)
        {
            Uri uri = new Uri(args[0]);
            Int32.TryParse(args[1], out int numThreads);
            Int32.TryParse(args[2], out int numItemsPerThread);
            Int32.TryParse(args[3], out int minNumBytesPerMessage);
            Int32.TryParse(args[4], out int maxNumBytesPerMessage);

            Console.WriteLine($"Started DemoClient with Uri '{uri}' numThreads '{numThreads}' numItemsPerThread '{numItemsPerThread}' minNumBytesPerMessage '{minNumBytesPerMessage}' maxNumBytesPerMessage '{maxNumBytesPerMessage}'");

            TestRunner runner = new TestRunner(uri, numThreads, numItemsPerThread, minNumBytesPerMessage, maxNumBytesPerMessage);
            runner.Run();
        }

        private static async Task RunSimpleTest()
        {
            var client = new SimpleClient();
            await client.Run();
        }
    }
}
