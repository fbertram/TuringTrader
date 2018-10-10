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
    public class Demo05_Optimizer : Algorithm
    {
        // these are the parameters to optimize. note that
        // we can optimize fields and properties alike
        [OptimizerParam(0, 9, 1)]
        public int X;

        [OptimizerParam(0, 90, 10)]
        public int Y { get; set; }

        // this is just a dummy for the algorithm's internal functionality.
        // the algorithm should set the Fitness value of this iteration
        override public void Run()
        {
            Thread.Sleep(250);
            FitnessValue = X + Y;

            // while optimizing, we should avoid printing to the log
            if (!IsOptimizing)
                Output.WriteLine("Run: X={0}, Y={1}", X, Y);
        }

        // create a report. typically, we would create a pretty plot here
        override public void Report()
        {
            Output.WriteLine("Report: Fitness={0}", FitnessValue);
        }

        // we should make sure that the constructor sets
        // our parameters to reasonable default values
        public Demo05_Optimizer()
        {
            X = 2;
            Y = 40;
        }
    }
}

//==============================================================================
// end of file