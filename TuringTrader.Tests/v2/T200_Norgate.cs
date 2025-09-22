//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T200_Norgate
// Description: Unit test for Norgate data source.
// History:     2022xi30, EFB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2025, Bertram Enterprises LLC dba TuringTrader.
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
using System.Threading.Tasks;
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
            private string _universe;

            public Testbed2(string universe)
            {
                _universe = universe;
            }

            public override void Run()
            {
                //StartDate
                //EndDate
                WarmupPeriod = TimeSpan.FromDays(0);

                var allTickers = new HashSet<string>();

                SimLoop(() =>
                {
                    var constituents = Universe(_universe);

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
            var universes = new List<Tuple<string, DateTime, DateTime, double, int>>
            {
                //Tuple.Create("$OEX", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 100.38482194979568, 232),
                Tuple.Create("$SPX", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2021-12-31T16:00-05:00"), 500.99925595238096, 1229),
                //Tuple.Create("$MID", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 382.63304144775248, 1683),
                //Tuple.Create("$SML", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 514.48021015761822, 2453),
                //Tuple.Create("$SP1500", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 1287.242148277875, 4053),
                //Tuple.Create("$SPDAUDP", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 51.483946293053123, 171),
                //Tuple.Create("$SPESG", DateTime.Parse("2010-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 126.519023282226, 469),
                
                Tuple.Create("$RUI", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 969.85043782837124, 3461),
                //Tuple.Create("$RUT", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 1925.0939871570345, 10773),
                //Tuple.Create("$RUA", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 2895.231873905429, 11997),
                //Tuple.Create("$RMC", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 659.13683596030353, 3003),
                //Tuple.Create("$RUMIC", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 1170.5858727378868, 7580),

                Tuple.Create("$NDX", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 89.826736719206068, 455),
                //Tuple.Create("$NGX", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 9.9985989492119085, 201),
                //Tuple.Create("$NXTQ", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 23.904845300642148, 362),

                Tuple.Create("$DJI", DateTime.Parse("1990-01-01T16:00-05:00"), DateTime.Parse("2023-12-31T16:00-05:00"), 29.996964389959135, 58),
            };

            foreach(var tuple in universes)
            {
                var algo = new Testbed2(tuple.Item1);
                algo.StartDate = tuple.Item2;
                algo.EndDate = tuple.Item3;
                algo.Run();
                var result = algo.EquityCurve;

                var avgTickers = result.Average(b => b.Value.Open);
                Assert.AreEqual(tuple.Item4, avgTickers, 1e-3);

                var totTickers = result.Max(b => b.Value.High);
                Assert.AreEqual(tuple.Item5, totTickers);
            }

        }

        private class TestbedMultithreading : Algorithm
        {
            public override void Run()
            {
                StartDate = DateTime.Parse("1990-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2023-12-31T16:00-05:00");

                var sumSpx = 0;
                SimLoop(() =>
                {
                    var spx = Universe("norgate:$SPX");
                    sumSpx += spx.Count;
                });
            }
        }
        [TestMethod]
        public void Test_Multithreading()
        {
            var taskList = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                void runTest()
                {
                    var algo = new TestbedMultithreading();
                    algo.Run();
                }
#if true
                taskList.Add(Task.Run(runTest));
#else
                runTest();
#endif
            }
            if (taskList.Count > 0)
                Task.WaitAll(taskList.ToArray());
        }
    }
}

//==============================================================================
// end of file