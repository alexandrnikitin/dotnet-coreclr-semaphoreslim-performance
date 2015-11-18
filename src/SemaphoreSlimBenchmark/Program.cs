using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SemaphorePerfTests;

namespace SemaphoreSlimBenchmark
{
    public class Program
    {
        public void Main(string[] args)
        {
            SemaphoreSlimPerfTest.RunTest();
            BlockingCollectionPerfTest.RunTest();
        }
    }
}
