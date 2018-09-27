//==============================================================================
// Project:     Trading Simulator
// Name:        Demo05_Optimizer
// Description: demonstrate algorithm optimization
// History:     2018ix20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class Demo05_Optimizer : Algorithm
    {
        #region internal data
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\..\Excel\SimpleTable.xlsm";
        #endregion

        // these are the parameters to optimize. note that
        // we can optimize fields and properties alike
        [OptimizerParam(1, 10, 1)]
        public int X;

        [OptimizerParam(10, 100, 10)]
        public int Y { get; set; }

        // this is just a dummy for the algorithm's internal functionality.
        // the algorithm must set the Fitness value of this iteration
        override public void Run()
        {
            Thread.Sleep(250);
            FitnessValue = X + Y;
        }

        // create a report. typically, we would create a pretty plot here
        public void Report()
        {
            Debug.WriteLine("X = {0}, Y = {1}", X, Y);
        }

        // this is the simplest form of optimization:
        // a brute-force iteration through all parameter combinations
        public void OptimizeSimple()
        {
            OptimizerGrid optimizer = new OptimizerGrid(this);
            optimizer.Run();

            // we can present the result in Excel
            optimizer.ResultsToExcel(_excelPath);

            // we can walk through the results
            OptimizerResult bestResult = optimizer.Results
                    .OrderByDescending(r => r.Fitness)
                    .First();

            // and re-run any of the results for a detailed report
            Demo05_Optimizer algo = (Demo05_Optimizer)optimizer.ReRun(bestResult);
            algo.Report();
        }

        // we should make sure that the constructor sets
        // our parameters to reasonable default values
        public Demo05_Optimizer()
        {
            X = 2;
            Y = 20;
        }

        static void Main(string[] args)
        {
            var algo = new Demo05_Optimizer();
            algo.OptimizeSimple();
        }
    }
}

//==============================================================================
// end of file