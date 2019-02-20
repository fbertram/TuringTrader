//==============================================================================
// Project:     Trading Simulator
// Name:        Demo05_Optimizer
// Description: demonstrate algorithm optimization
// History:     2018ix20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Demos
{
    public class Demo05_Optimizer : Algorithm
    {
        // these are the parameters to optimize. note that
        // we can optimize fields and properties alike
        [OptimizerParam(0, 90, 10)]
        public int X { get; set; }

        [OptimizerParam(0, 9, 1)]
        public int Y;

        // this is just a dummy for the algorithm's internal functionality.
        // the algorithm should set the Fitness value of this iteration
        override public void Run()
        {
            Thread.Sleep(250);
            FitnessValue = X + Y;

            // while optimizing, we should avoid printing to the log
            if (!IsOptimizing)
                Output.WriteLine("Run: {0}", OptimizerParamsAsString);
        }

        // create a report. typically, we would create a pretty plot here
        public override void Report()
        {
            Output.WriteLine("Report: Fitness={0}", FitnessValue);
        }

        // we should make sure that the constructor sets
        // our parameters to reasonable default values
        public Demo05_Optimizer()
        {
            X = 40;
            Y = 2;
        }
    }
}

//==============================================================================
// end of file