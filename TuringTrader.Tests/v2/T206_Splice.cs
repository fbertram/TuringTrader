//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T206_Splice
// Description: Unit test for data source splice.
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
    public class T206_Splice
    {
        private class DataRetrieval : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-01-29T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                TestResult = Asset("splice:csv:../TestData/splice-b.csv,csv:../TestData/splice-a.csv");
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var algo = new DataRetrieval();
            algo.Run();
            var result = algo.TestResult;

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2021-01-04T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2021-01-29T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 19);

            var firstOpen = result.Data.Result.First().Value.Open;
            Assert.IsTrue(Math.Abs(firstOpen - 10000) < 1e-3);

            var lastClose = result.Data.Result.Last().Value.Close;
            Assert.IsTrue(Math.Abs(lastClose - 28000) < 1e-3);

            var sum = result.Data.Result.Sum(b => b.Value.Close);
            Assert.IsTrue(Math.Abs(sum - 361000) < 1e-3);
        }
    }
}

//==============================================================================
// end of file