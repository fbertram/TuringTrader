//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T209_CustomData
// Description: Unit test for custom data.
// History:     2023iii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T209_CustomData
    {
        private class Testbed : Algorithm
        {
            private TimeSeriesAsset CustomData(string name) => Asset(name, () =>
            {
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

            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

                SimLoop(() =>
                {
                    Plotter.SelectChart("custom data", "date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("custom data #1", CustomData("ernie").Close[0]);
                    Plotter.Plot("custom data #2", CustomData("bert").Close[0]);
                });
            }
        }

        [TestMethod]
        public void Test_CustomData()
        {
            var algo = new Testbed();
            algo.Run();

            // TODO: perform tests here
        }
    }
}

//==============================================================================
// end of file