using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemaphorePerfTests
{
    // TODO uncomment to perform tests against your SemaphoreSlim
    // using System.Threading2;

    public class SemaphoreSlimPerfTest
    {
        private static TimeSpan TestSemaphoreSlim(string name, int elemCount, int releasingThreadCount, int waitingThreadCount, int releasingSpin, int waitingSpin)
        {
            SemaphoreSlim sem = new SemaphoreSlim(0, int.MaxValue);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[releasingThreadCount];
            Thread[] takeThreads = new Thread[waitingThreadCount];

            int addedElemCount = 0;

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    sem.Release();
                    Thread.SpinWait(releasingSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        sem.Wait(myToken);
                        Thread.SpinWait(waitingSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                while (sem.Wait(0)) { }

                barierTakers.SignalAndWait();
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

            Console.WriteLine(name + ". SemaphoreSlim. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static TimeSpan RunAverage(int avg, Func<string, int, int, int, int, int, TimeSpan> func, int elemCount, int releasingThreadCount, int waitingThreadCount, int releasingSpin, int waitingSpin)
        {
            TimeSpan total = TimeSpan.Zero;

            for (int i = 0; i < avg; i++)
                total += func(string.Format("{0}, {1}", releasingThreadCount, waitingThreadCount), elemCount, releasingThreadCount, waitingThreadCount, releasingSpin, waitingSpin);

            TimeSpan averageTime = TimeSpan.FromTicks(total.Ticks / avg);
            Console.WriteLine("Average Time = " + ((long)averageTime.TotalMilliseconds).ToString() + "ms");
            Console.WriteLine();

            return averageTime;
        }



        public static void RunTest()
        {
            RunAverage(10, TestSemaphoreSlim, 10000000, 1, 1, 10, 10); // 1, 1
            RunAverage(10, TestSemaphoreSlim, 10000000, 8, 8, 10, 10); // 8, 8
            RunAverage(10, TestSemaphoreSlim, 10000000, 1, 8, 10, 10); // 1, 8
            RunAverage(10, TestSemaphoreSlim, 10000000, 1, 16, 10, 10);// 1, 16
            RunAverage(10, TestSemaphoreSlim, 10000000, 8, 1, 10, 10); // 8, 1
            RunAverage(10, TestSemaphoreSlim, 10000000, 16, 1, 10, 10);// 16, 1
        }
    }
}