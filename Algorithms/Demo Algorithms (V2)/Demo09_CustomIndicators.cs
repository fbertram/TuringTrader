//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo09_CustomIndicators
// Description: demonstrate custom indicators
// History:     2019v21, FUB, created
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
using System.Threading.Tasks;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;
#endregion

namespace Demos
{
    public class Demo09_CustomIndicators : Algorithm
    {
        private TimeSeriesFloat CustomEMA(TimeSeriesFloat series, int period)
        {
            // all indicator instances must have a unique name
            var name = string.Format("{0}.CustomEMA({1})", series.Name, period);
            var alpha = 2.0 / (1.0 + period);

            // use a lambda to calculate the recursive EMA
            return Lambda(
                name,
                (ema) => ema + alpha * (series[0] - ema),
                0.0);
        }

        private TimeSeriesFloat CustomSMA(TimeSeriesFloat series, int period)
        {
            // all indicator instances must have a unique name
            var name = string.Format("{0}.CustomSMA({1})", series.Name, period);

            // use a lambda to calculate the non-recursive SMA
            return Lambda(name, 
                () => Enumerable.Range(0, period).Average(t => series[t]));
        }

        public override void Run()
        {
            //---------- initialization

            // set the simulation period
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

            //---------- simulation
                
            SimLoop(() =>
            {
                var input = Asset("$SPX").Close;
                var custom1 = CustomEMA(input, 200);
                var custom2 = CustomSMA(input, 200);

                Plotter.SelectChart("custom indicators", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot(input.Name, input[0]);
                Plotter.Plot(custom1.Name, custom1[0]);
                Plotter.Plot(custom2.Name, custom2[0]);
            });
        }

        // minimalistic chart
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file