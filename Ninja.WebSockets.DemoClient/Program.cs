using System;
using System.Diagnostics;
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
                // ws://localhost:27416/echo 5 1000 5000 40000
                RunComplexTest(args).Wait();
            }
            else
            {
                Console.WriteLine("Wrong number of arguments. 0 for simple test. 5 for complex test.");
                Console.WriteLine($"Complex Test: uri numThreads numItemsPerThread minNumBytesPerMessage maxNumBytesPerMessage");
                Console.WriteLine("e.g: ws://localhost:27416/chat/echo 5 100 4 4");
            }

            Console.WriteLine("Press Enter to quit");
            Console.ReadLine();
        }

        private static async Task RunComplexTest(string[] args)
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
            SimpleClient client = new SimpleClient();
            await client.Run();
        }
    }
}
