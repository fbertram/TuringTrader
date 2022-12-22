//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T200_Norgate
// Description: Unit test for Norgate data source.
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
            public string Description;
            public double FirstOpen;
            public double LastClose;
            public double HighestHigh = 0.0;
            public double LowestLow = 1e99;
            public double NumBars;
            public override void Run()
            {
                StartDate = DateTime.Parse("2019-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2019-01-12T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("norgate:msft");

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
        public void Test_DataRetrieval()
        {
            var algo = new Testbed();
            algo.Run();

            Assert.IsTrue(algo.Description.ToLower().Contains("microsoft"));
            Assert.IsTrue(algo.NumBars == 8);
            Assert.IsTrue(Math.Abs(algo.LastClose / algo.FirstOpen - 98.484176635742188 / 95.370620727539063) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.HighestHigh / algo.LowestLow - 100.47684478759766 / 93.119270324707031) < 1e-3);
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
            Assert.IsTrue(Math.Abs(avgTickers - 500.93551587301585) < 1e-3);

            var totTickers = result.Max(b => b.Value.High);
            Assert.IsTrue(Math.Abs(totTickers - 1232.0) < 1e-3);
        }
    }
}

//==============================================================================
// end of file