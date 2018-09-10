using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class Program
    {
        static void Main(string[] args)
        {
            var algo = new MyAlgo2();
            algo.Run();
            double fitness = (double)algo.Report(ReportType.FitnessValue);
        }
    }
}
