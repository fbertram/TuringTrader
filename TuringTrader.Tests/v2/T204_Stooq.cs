//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T204_Stooq
// Description: Unit test for Stooq data source.
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
using System.IO;
using System.Linq;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T204_Stooq
    {
        private class DataRetrieval : Algorithm
        {
            public TimeSeriesAsset TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2019-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2019-01-12T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                TestResult = Asset("stooq:msft.us");
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var cachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TuringTrader", "Cache", "msft.us");
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);

            for (int i = 0; i < 2; i++)
            {
                var algo = new DataRetrieval();
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
        }
    }
}

//==============================================================================
// end of file