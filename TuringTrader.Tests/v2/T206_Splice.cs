//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T206_Splice
// Description: Unit test for data source splice.
// History:     2022xi30, FUB, created
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
using System.IO;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T206_Splice
    {
        private class TestbedSplice : Algorithm
        {
            public string Description;
            public double FirstOpen;
            public double LastClose;
            public double HighestHigh = 0.0;
            public double LowestLow = 1e99;
            public double NumBars;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-01-29T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("splice:csv:test/splice-b.csv,csv:test/splice-a.csv");

                    if (IsFirstBar)
                    {
                        Description = a.Description;
                        FirstOpen = a.Open[0];
                        NumBars = 0;
                    }
                    if (IsLastBar)
                    {
                        LastClose = a.Close[0];
                    }
                    HighestHigh = Math.Max(HighestHigh, a.High[0]);
                    LowestLow = Math.Min(LowestLow, a.Low[0]);
                    NumBars++;
                });
            }
        }

        [TestMethod]
        public void Test_Splice()
        {
            var algo = new TestbedSplice();
            algo.Run();

            Assert.IsTrue(algo.NumBars == 19);
            Assert.IsTrue(Math.Abs(algo.FirstOpen - 10000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.LastClose - 28000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.HighestHigh - 28000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.LowestLow - 10000) < 1e-3);
        }

        private class TestbedSplice2 : Algorithm
        {
            public string Description;
            public double FirstOpen;
            public double LastClose;
            public double HighestHigh = 0.0;
            public double LowestLow = 1e99;
            public double NumBars;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-01-29T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("splice:csv:test/splice-b.csv,csv:does-not-exist");

                    if (IsFirstBar)
                    {
                        Description = a.Description;
                        FirstOpen = a.Open[0];
                        NumBars = 0;
                    }
                    if (IsLastBar)
                    {
                        LastClose = a.Close[0];
                    }
                    HighestHigh = Math.Max(HighestHigh, a.High[0]);
                    LowestLow = Math.Min(LowestLow, a.Low[0]);
                    NumBars++;
                });
            }
        }

        [TestMethod]
        public void Test_Splice2()
        {
            // this test makes sure we
            // - return the data we have
            // - don't throw for a missing backfill
            // - don't create a new file or folder for the missing data
            var algo = new TestbedSplice2();
            algo.Run();

            Assert.AreEqual(algo.NumBars, 19);
            Assert.AreEqual(algo.FirstOpen, 20000, 1e-3);
            Assert.AreEqual(algo.LastClose, 28000, 1e-3);
            Assert.AreEqual(algo.HighestHigh, 28000, 1e-3);
            Assert.AreEqual(algo.LowestLow, 20000, 1e-3);
            Assert.IsFalse(File.Exists(Path.Combine(GlobalSettings.DataPath, "does-not-exist")));
            Assert.IsFalse(Directory.Exists(Path.Combine(GlobalSettings.DataPath, "does-not-exist")));
        }

        private class TestbedJoin : Algorithm
        {
            public string Description;
            public double FirstOpen;
            public double LastClose;
            public double HighestHigh = 0.0;
            public double LowestLow = 1e99;
            public double NumBars;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-01-29T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("join:csv:test/splice-b.csv,csv:test/splice-a.csv");

                    if (IsFirstBar)
                    {
                        Description = a.Description;
                        FirstOpen = a.Open[0];
                        NumBars = 0;
                    }
                    if (IsLastBar)
                    {
                        LastClose = a.Close[0];
                    }
                    HighestHigh = Math.Max(HighestHigh, a.High[0]);
                    LowestLow = Math.Min(LowestLow, a.Low[0]);
                    NumBars++;
                });
            }
        }

        [TestMethod]
        public void Test_Join()
        {
            var algo = new TestbedJoin();
            algo.Run();

            Assert.IsTrue(algo.NumBars == 19);
            Assert.IsTrue(Math.Abs(algo.FirstOpen - 1000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.LastClose - 28000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.HighestHigh - 28000) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.LowestLow - 1000) < 1e-3);
        }
    }
}

//==============================================================================
// end of file