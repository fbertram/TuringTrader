//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo05_Optimizer
// Description: demonstrate algorithm optimization
// History:     2018ix20, FUB, created
//              2023iii02, FUB, updated for v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;
#endregion

namespace TuringTrader.Demos
{
    public class Demo05_Optimizer : Algorithm
    {
        // these are the parameters to optimize. note that
        // we can optimize fields and properties alike
        [OptimizerParam(0, 90, 10)]
        public int X { get; set; } = 40;

        [OptimizerParam(0, 9, 1)]
        public int Y = 2;

        override public void Run()
        {
            // this is just a dummy for the algorithm's internal functionality.
            Thread.Sleep(250);

            // we can set the FitnessValue manually
            // as a default, TuringTrader will use the
            // return on maximum drawdown
            FitnessValue = X + Y;

            // avoid printing to the log while optimizing
            if (!IsOptimizing)
                Output.WriteLine("Run: {0}", OptimizerParamsAsString);
        }

        // dummy report - typically, we would create a pretty chart here
        public override void Report()
        {
            Output.WriteLine("Report: Fitness={0}", FitnessValue);
        }
    }
}

//==============================================================================
// end of file