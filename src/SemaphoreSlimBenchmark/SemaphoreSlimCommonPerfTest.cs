using System;
using System.Diagnostics;
using System.Threading;

namespace SemaphoreSlimBenchmark
{
    // TODO uncomment to perform tests against your SemaphoreSlim
    //using System.Threading2;

    public class SemaphoreSlimCommonPerfTest
    {
        private static TimeSpan TestSemaphoreSlim(string name, int semaphoreConcurrency, int threadCount, int threadIterationCount)
        {
            var sem = new SemaphoreSlim(semaphoreConcurrency, semaphoreConcurrency);
            var threads = new Thread[threadCount];

            var barierStart = new Barrier(1 + threads.Length);
            var barierStop = new Barrier(1 + threads.Length);

            Action action = () =>
            {
                var addedElemCount = 0;
                barierStart.SignalAndWait();

                while (addedElemCount < threadIterationCount)
                {
                    sem.Wait();
                    Thread.SpinWait(10);
                    sem.Release();
                    addedElemCount++;
                }

                barierStop.SignalAndWait();
            };



            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(action));
            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            var sw = new Stopwatch();
            barierStart.SignalAndWait();
            sw.Start();
            barierStop.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Console.WriteLine(name + ". SemaphoreSlim. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static TimeSpan RunAverage(int avg, Func<string, int, int, int, TimeSpan> func, int semaphoreConcurrency, int threadCount, int threadIterationCount)
        {
            var total = TimeSpan.Zero;

            for (var i = 0; i < avg; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                total += func(
                    string.Format("{0}, {1}", semaphoreConcurrency, threadCount),
                    semaphoreConcurrency,
                    threadCount,
                    threadIterationCount);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var averageTime = TimeSpan.FromTicks(total.Ticks / avg);
            Console.WriteLine("Average Time = " + ((long)averageTime.TotalMilliseconds).ToString() + "ms");
            Console.WriteLine();

            return averageTime;
        }



        public static void RunTest()
        {
            RunAverage(10, TestSemaphoreSlim, 1, 1, 100000);
            RunAverage(10, TestSemaphoreSlim, 8, 8, 100000);
            RunAverage(10, TestSemaphoreSlim, 1, 8, 100000);
            RunAverage(10, TestSemaphoreSlim, 1, 16, 100000);
            RunAverage(10, TestSemaphoreSlim, 8, 1, 100000);
            RunAverage(10, TestSemaphoreSlim, 16, 1, 100000);
        }
    }

}