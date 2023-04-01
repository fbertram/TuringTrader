//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T309_Ehlers
// Description: Unit test for Ehlers's indicators.
// History:     2023iv01, FUB, created
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
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T309_Ehlers
    {
        #region DominantCyclePeriod
        [TestMethod]
        public void Test_DominantCyclePeriod()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2021-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);

            var series = algo.Asset("synth", () =>
            {
                var bars = new List<BarType<OHLCV>>();

                double accumulatedPhase = 0.0;
                foreach (var t in algo.TradingCalendar.TradingDays)
                {
                    var cyclePeriod = t < DateTime.Parse("2021-07-01T16:00-05:00")
                        ? 15 : 45;
                    var price = 50.0 + 5.0 * Math.Sin(accumulatedPhase);
                    accumulatedPhase += 2.0 * Math.PI / cyclePeriod;

                    bars.Add(new BarType<OHLCV>(
                        t,
                        new OHLCV(price, price, price, price, 0.0)));
                }
                return bars;
            }).Close;

            var dcperiod1 = series.DominantCyclePeriod().Data
                .Where(b => b.Date >= DateTime.Parse("2021-02-01T16:00-05:00")
                    && b.Date < DateTime.Parse("2021-06-01T16:00-05:00"))
                .ToList();

            var dcperiod2 = series.DominantCyclePeriod().Data
                .Where(b => b.Date >= DateTime.Parse("2021-09-01T16:00-05:00")
                    && b.Date < DateTime.Parse("2021-11-01T16:00-05:00"))
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(15.023741049818078, dcperiod1.Average(b => b.Value), 1e-5);
            Assert.AreEqual(15.051485682831206, dcperiod1.Max(b => b.Value), 1e-5);
            Assert.AreEqual(14.996305446479827, dcperiod1.Min(b => b.Value), 1e-5);

            Assert.AreEqual(43.522484932507346, dcperiod2.Average(b => b.Value), 1e-5);
            Assert.AreEqual(45.82704491489082, dcperiod2.Max(b => b.Value), 1e-5);
            Assert.AreEqual(40.13115260537523, dcperiod2.Min(b => b.Value), 1e-5);
        }
        #endregion
    }
}

//==============================================================================
// end of file
