//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T201_Fred
// Description: Unit test for FRED data source.
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
using System.Linq;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T201_Fred
    {
        private class DataRetrieval : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-11-25T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                TestResult = Asset("fred:EFFR"); // Effective Federal Funds Rate
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var algo = new DataRetrieval();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Description;
            Assert.IsTrue(description.ToLower().Contains("effective federal funds rate"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-11-25T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 227);

            var firstOpen = result.Data.Result.First().Value.Open;
            var lastClose = result.Data.Result.Last().Value.Close;
            var highestHigh = result.Data.Result.Max(b => b.Value.High);
            var lowestLow = result.Data.Result.Min(b => b.Value.Low);
            Assert.IsTrue(Math.Abs(lastClose / firstOpen - 3.83 / 0.08) < 1e-3);
            Assert.IsTrue(Math.Abs(highestHigh / lowestLow - 3.83 / 0.08) < 1e-3);
        }
    }
}

//==============================================================================
// end of file