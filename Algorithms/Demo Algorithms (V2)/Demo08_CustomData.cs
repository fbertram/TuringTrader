//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo08_CustomData
// Description: demonstrate custom data sources
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
    public class Demo08_CustomData : Algorithm
    {
        // we overload the asset method, so that our custom data
        // source is localized in a short blob of code
        public override TimeSeriesAsset Asset(string name)
        {
            switch(name)
            {
                case "ernie":
                case "bert":
                    return Asset(name, () =>
                    {
                        // the retrieval function returns a list of bars
                        // note that this function is parameterless
                        var bars = new List<BarType<OHLCV>>();
                        foreach (var timestamp in TradingCalendar.TradingDays)
                        {
                            var p = name == "ernie" ? 360.0 : 180.0;
                            var t = (timestamp - DateTime.Parse("1970-01-01")).TotalDays;
                            var v = Math.Sin(2.0 * Math.PI * t / p);

                            bars.Add(new BarType<OHLCV>(
                                timestamp,
                                new OHLCV(v, v, v, v, 0.0)));
                        }

                        return bars;
                    });

                // optionally, we can keep the built-in data sources alive
                default:
                    return base.Asset(name);
            }
        }

        public override void Run()
        {
            StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

            SimLoop(() =>
            {
                Plotter.SelectChart("custom data", "date");
                Plotter.SetX(SimDate);

                // custom data behave just like built-in data
                // they have OHLCV bars, and they are cached
                Plotter.Plot("custom data #1", Asset("ernie").Close[0]);
                Plotter.Plot("custom data #2", Asset("bert").Close[0]);
                Plotter.Plot("regular data", Asset("SPY").Close[0] / 350.0 - 1.0);
            });
        }

        // minimalistic report
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file