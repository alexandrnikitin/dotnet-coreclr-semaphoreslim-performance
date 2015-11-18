using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemaphorePerfTests
{
    public static class BlockingCollectionPerfTest
    {
        private static TimeSpan TestBlockingCollection(string name, int elemCount, int addThreadCount, int takeThreadCount, int addSpin, int takeSpin)
        {
            BlockingCollection<int> col = new BlockingCollection<int>(10000);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThreadCount];
            Thread[] takeThreads = new Thread[takeThreadCount];

            int addedElemCount = 0;
            List<int> globalList = new List<int>();

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    col.Add(index - 1);
                    Thread.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;
                List<int> valList = new List<int>(elemCount / takeThreadCount + 100);

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        int val = 0;
                        val = col.Take(myToken);

                        valList.Add(val);
                        Thread.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                int val2 = 0;
                while (col.TryTake(out val2))
                    valList.Add(val2);

                barierTakers.SignalAndWait();

                lock (globalList)
                {
                    globalList.AddRange(valList);
                }
            };

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i] = new Thread(new ThreadStart(addAction));
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i] = new Thread(new ThreadStart(takeAction));


            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Start();
            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Start();

            barierStart.SignalAndWait();

            Stopwatch sw = Stopwatch.StartNew();

            barierAdders.SignalAndWait();
            srcCancel.Cancel();
            barierTakers.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Join();
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Join();


            if (globalList.Count != elemCount)
                Console.WriteLine("Bad count");

            Console.WriteLine(name + ". BlockingCollection. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }



        private static TimeSpan RunAverage(int avg, Func<string, int, int, int, int, int, TimeSpan> func, int elemCount, int addThreadCount, int takeThreadCount, int addSpin, int takeSpin)
        {
            TimeSpan total = TimeSpan.Zero;

            for (int i = 0; i < avg; i++)
                total += func(string.Format("{0}, {1}", addThreadCount, takeThreadCount), elemCount, addThreadCount, takeThreadCount, addSpin, takeSpin);

            TimeSpan averageTime = TimeSpan.FromTicks(total.Ticks / avg);
            Console.WriteLine("Average Time = " + ((long)averageTime.TotalMilliseconds).ToString() + "ms");
            Console.WriteLine();

            return averageTime;
        }


        public static void RunTest()
        {
            RunAverage(10, TestBlockingCollection, 5000000, 1, 1, 10, 10);   // 1, 1
            RunAverage(10, TestBlockingCollection, 5000000, 4, 4, 10, 10);   // 4, 4
            RunAverage(10, TestBlockingCollection, 5000000, 8, 8, 10, 10);   // 8, 8
            RunAverage(10, TestBlockingCollection, 5000000, 16, 1, 10, 10);  // 16, 1
            RunAverage(10, TestBlockingCollection, 5000000, 1, 16, 10, 10);  // 1, 16
            RunAverage(10, TestBlockingCollection, 5000000, 16, 16, 10, 10); // 16, 16
        }
    }
}