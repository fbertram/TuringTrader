//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T200_Norgate
// Description: Unit test for Norgate data source.
// History:     2022xi30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T200_Norgate
    {
        private class Testbed : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2019-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2019-01-12T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                TestResult = Asset("norgate:msft");
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var algo = new Testbed();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Description;
            Assert.IsTrue(description.ToLower().Contains("microsoft"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2019-01-02T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2019-01-11T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 8);

            var firstOpen = result.Data.Result.First().Value.Open;
            var lastClose = result.Data.Result.Last().Value.Close;
            var highestHigh = result.Data.Result.Max(b => b.Value.High);
            var lowestLow = result.Data.Result.Min(b => b.Value.Low);
            Assert.IsTrue(Math.Abs(lastClose / firstOpen - 98.48418 / 95.37062) < 1e-3);
            Assert.IsTrue(Math.Abs(highestHigh / lowestLow - 100.47684 / 93.11927) < 1e-3);
        }

        private class Testbed2 : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("1990-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                var allTickers = new HashSet<string>();

                SimLoop(() =>
                {
                    var constituents = Universe("$SPX");

                    foreach (var constituent in constituents)
                        if (!allTickers.Contains(constituent))
                            allTickers.Add(constituent);

                    return new OHLCV(constituents.Count, allTickers.Count, 0.0, 0.0, 0.0);
                });
            }
        }

        [TestMethod]
        public void Test_Universe()
        {
            var algo = new Testbed2();
            algo.Run();
            var result = algo.Result;

            var avgTickers = result.Average(b => b.Value.Open);
            Assert.IsTrue(Math.Abs(avgTickers - 507.95126488095241) < 1e-3);

            var totTickers = result.Max(b => b.Value.High);
            Assert.IsTrue(Math.Abs(totTickers - 1231) < 1e-3);
        }
    }
}

//==============================================================================
// end of file