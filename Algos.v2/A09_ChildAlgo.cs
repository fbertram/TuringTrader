//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A09_ChildAlgo
// Description: Develop & test child algorithms.
// History:     2022xi04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
//              https://www.bertram.solutions
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
using System.Globalization;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.DemoV2
{
    public class A09_ChildAlgo : Algorithm
    {
        public override string Name => "A09_ChildAlgo";


        private class ChildAlgo : Algorithm
        {
            public override void Run()
            {
                StartDate = StartDate ?? DateTime.Parse("01/01/2015", CultureInfo.InvariantCulture);
                EndDate = EndDate ?? DateTime.Now;

                SimLoop(() =>
                {
                    Asset("SPY").Allocate(1.0, OrderType.openNextBar);
                });
            }
        }

        public override void Run()
        {
            StartDate = DateTime.Parse("10/31/2022", CultureInfo.InvariantCulture);
            EndDate = DateTime.Now;

            var childAlgo = new ChildAlgo();

            var spy0 = (double?)null;

            SimLoop(() =>
            {
                spy0 = spy0 ?? Asset("SPY").Open[0] / 1000.0;

                Plotter.SelectChart("Price Tracking", "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot("Open", Asset(childAlgo).Open[0] / Asset("SPY").Open[0] / spy0);
                Plotter.Plot("Close", Asset(childAlgo).Close[0] / Asset("SPY").Close[0] / spy0);

                Plotter.SelectChart("Open Prices", "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot("Child", Asset(childAlgo).Open[0]);
                Plotter.Plot("Local", Asset("SPY").Open[0] / spy0);

                Plotter.SelectChart("Close Prices", "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot("Child", Asset(childAlgo).Close[0]);
                Plotter.Plot("Local", Asset("SPY").Close[0] / spy0);
            });
        }

        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file
