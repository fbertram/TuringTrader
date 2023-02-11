//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T208_Algorithm
// Description: Unit test for child algorithms.
// History:     2023ii08, FUB, created
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
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T208_Algorithm
    {
        #region v2 algorithms
        private class Testbed_v2 : Algorithm
        {
            private class SwitchHalfTime_v2 : Algorithm
            {
                public bool HoldFirst = true;
                public override void Run()
                {
                    StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
                    EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

                    SimLoop(() =>
                    {
                        var hold = SimDate.Date < DateTime.Parse("2022-07-01")
                            ? HoldFirst : !HoldFirst;

                        var asset = Asset("$SPX");

                        if (asset.Position < 0.5 && hold == true)
                            asset.Allocate(1.0, OrderType.closeThisBar);
                        else if (asset.Position > 0.5 && hold == false)
                            asset.Allocate(0.0, OrderType.closeThisBar);
                    });
                }
            }

            public int NumChildTrades = 0;

            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-31T16:00-05:00");
                ((Account_Default)Account).Friction = 0.0;

                var algo1 = new SwitchHalfTime_v2 { HoldFirst = true, };
                var algo2 = new SwitchHalfTime_v2 { HoldFirst = false, };

                SimLoop(() =>
                {
                    if (IsFirstBar)
                    {
                        Asset(algo1).Allocate(1.0, OrderType.closeThisBar);
                        Asset(algo2).Allocate(1.0, OrderType.closeThisBar);
                    }
                });

                NumChildTrades = algo1.Account.TradeLog.Count + algo2.Account.TradeLog.Count;

                Plotter.AddTargetAllocation();
                Plotter.AddTradeLog();
                Plotter.AddHistoricalAllocations();
            }
        }

        [TestMethod]
        public void Test_v2()
        {
            var algo = new Testbed_v2();
            algo.Run();

            Assert.AreEqual(algo.NetAssetValue, 800.31918528935819, 1e-5);
            Assert.AreEqual(algo.Account.TradeLog.Count, 2);
            Assert.AreEqual(algo.NumChildTrades, 3);

            var alloc = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS];
            Assert.AreEqual(alloc.Count, 1);
            Assert.AreEqual((string)alloc[0]["Symbol"], "$SPX");
            Assert.AreEqual(double.Parse(((string)alloc[0]["Allocation"]).TrimEnd('%')), 125.35, 1e-5);

            var last = algo.Plotter.AllData[Simulator.Plotter.SheetNames.LAST_REBALANCE];
            Assert.IsTrue(last.Count == 1);
            Assert.IsTrue((DateTime)last[0]["Value"] == DateTime.Parse("2022-07-01T16:00-04:00"));

            var history = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS_HISTORY];
            Assert.AreEqual(history.Count, 2);
            Assert.AreEqual((DateTime)history[0]["Date"], DateTime.Parse("2022-01-03T16:00-05:00"));
            Assert.AreEqual((string)history[0]["Allocation"], "$SPX=100.00%");
            Assert.AreEqual((DateTime)history[1]["Date"], DateTime.Parse("2022-07-01T16:00-04:00"));
            Assert.AreEqual((string)history[1]["Allocation"], "$SPX=100.00%");

            // TODO: add checks of trading log here
        }
        #endregion
        #region v1 algorithms
        private class Testbed_v1 : Algorithm
        {
            private class SwitchHalfTime_v1 : Simulator.Algorithm
            {
                public bool HoldFirst = true;

                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = startTime ?? DateTime.Parse("2022-01-01T16:00-05:00");
                    EndTime = endTime ?? DateTime.Parse("2022-12-31T16:00-05:00");
                    WarmupStartTime = StartTime;

                    Deposit(1e6);
                    CommissionPerShare = 0.0;

                    var ds = AddDataSource("$SPX");

                    foreach (var st in SimTimes)
                    {
                        var hold = SimTime[0] < DateTime.Parse("2022-07-01")
                            ? HoldFirst : !HoldFirst;

                        var shares = hold ? (int)Math.Floor(NetAssetValue[0] / ds.Instrument.Close[0]) : 0;
                        ds.Instrument.Trade(shares - ds.Instrument.Position, Simulator.OrderType.closeThisBar);

                        yield return Simulator.Bar.NewOHLC(
                            string.Format("{0}-HoldFirst={1}", Name, HoldFirst), SimTime[0],
                            NetAssetValue[0], NetAssetValue[0], NetAssetValue[0], NetAssetValue[0], 0);
                    }
                }
            }

            public int NumChildTrades = 0;

            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                ((Account_Default)Account).Friction = 0.0;

                var algo1 = new SwitchHalfTime_v1 { HoldFirst = true, };
                var algo2 = new SwitchHalfTime_v1 { HoldFirst = false, };

                // NOTE: we can access the v2 wrapper via the
                //       asset's meta information
                var algo1v2 = Asset(algo1).Meta.Generator;
                var algo2v2 = Asset(algo2).Meta.Generator;

                SimLoop(() =>
                {
                    if (IsFirstBar)
                    {
                        Asset(algo1).Allocate(1.0, OrderType.closeThisBar);
                        Asset(algo2).Allocate(1.0, OrderType.closeThisBar);
                    }
                });

                NumChildTrades = algo1v2.Account.TradeLog.Count + algo2v2.Account.TradeLog.Count;

                Plotter.AddTargetAllocation();
                Plotter.AddTradeLog();
                Plotter.AddHistoricalAllocations();
            }
        }

        [TestMethod]
        public void Test_v1()
        {
            var algo = new Testbed_v1();
            algo.Run();

            Assert.AreEqual(algo.NetAssetValue, 801.68251367187486, 1e-5); // v2 test: 800.31918528935819
            Assert.AreEqual(algo.Account.TradeLog.Count, 2);
            Assert.AreEqual(algo.NumChildTrades, 3);

            var alloc = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS];
            Assert.AreEqual(alloc.Count, 1);
            Assert.AreEqual((string)alloc[0]["Symbol"], "$SPX");
            Assert.AreEqual(double.Parse(((string)alloc[0]["Allocation"]).TrimEnd('%')), 125.00, 1e-5); // v2 test: 125.35

            var last = algo.Plotter.AllData[Simulator.Plotter.SheetNames.LAST_REBALANCE];
            Assert.IsTrue(last.Count == 1);
            //Assert.IsTrue((DateTime)last[0]["Value"] == DateTime.Parse("2022-07-01T16:00-04:00")); // TODO: convert time!

            var history = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS_HISTORY];
            Assert.AreEqual(history.Count, 2);
            //Assert.AreEqual((DateTime)history[0]["Date"], DateTime.Parse("2022-01-03T16:00-05:00"));
            Assert.AreEqual((string)history[0]["Allocation"], "$SPX=99.77%");
            Assert.AreEqual((DateTime)history[1]["Date"], DateTime.Parse("2022-07-01T16:00-04:00"));
            Assert.AreEqual((string)history[1]["Allocation"], "$SPX=99.84%");

            // TODO: add checks of trading log here
        }
        #endregion
    }
}

//==============================================================================
// end of file