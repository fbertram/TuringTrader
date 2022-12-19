//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T205_Csv
// Description: Unit test for CSV data source.
// History:     2022xi30, FUB, created
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T205_Csv
    {
        private class DataRetrieval : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2020-01-02T16:00-05:00");
                EndDate = DateTime.Parse("2020-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                TestResult = Asset("csv:backfills/$SPXTR.csv");
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var algo = new DataRetrieval();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Description;
            Assert.IsTrue(description.ToLower().Contains("$spxtr.csv"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2020-01-02T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2020-12-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 253);

            var firstOpen = result.Data.Result.First().Value.Open;
            var lastClose = result.Data.Result.Last().Value.Close;
            var highestHigh = result.Data.Result.Max(b => b.Value.High);
            var lowestLow = result.Data.Result.Min(b => b.Value.Low);
            Assert.IsTrue(Math.Abs(lastClose / firstOpen - 196865.1465 / 167008.1405) < 1e-3);
            Assert.IsTrue(Math.Abs(highestHigh / lowestLow - 197063.5459 / 113326.6985) < 1e-3);
        }
    }
}

//==============================================================================
// end of file