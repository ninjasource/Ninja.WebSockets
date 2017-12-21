using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets.DemoClient.Complex
{
    class TestRunner
    {
        private readonly Uri _uri;
        private readonly int _numThreads;
        private readonly int _numItemsPerThread;
        private readonly int _minNumBytesPerMessage;
        private readonly int _maxNumBytesPerMessage;

        public TestRunner(Uri uri, int numThreads, int numItemsPerThread, int minNumBytesPerMessage, int maxNumBytesPerMessage)
        {
            _uri = uri;
            _numThreads = numThreads;
            _numItemsPerThread = numItemsPerThread;
            _minNumBytesPerMessage = minNumBytesPerMessage;
            _maxNumBytesPerMessage = maxNumBytesPerMessage;
        }

        public void Run()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Parallel.For(0, _numThreads, Run);
            Console.WriteLine($"Completed in {stopwatch.Elapsed.TotalMilliseconds:#,##0.00} ms");
        }

        public void Run(int index, ParallelLoopState state)
        {
            StressTest test = new StressTest(index, _uri, _numItemsPerThread, _minNumBytesPerMessage, _maxNumBytesPerMessage);
            test.Run().Wait();
        }
    }
}
