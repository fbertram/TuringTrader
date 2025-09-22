//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T208_Algorithm
// Description: Unit test for child algorithms.
// History:     2023ii08, EFB, created
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
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(DateTime.Parse("2022-07-01T16:00-04:00"), (DateTime)last[0]["Value"]);

            var history = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS_HISTORY];
            Assert.AreEqual(2, history.Count);
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

                SimLoop(() =>
                {
                    if (IsFirstBar)
                    {
                        Asset(algo1).Allocate(1.0, OrderType.closeThisBar);
                        Asset(algo2).Allocate(1.0, OrderType.closeThisBar);
                    }
                });

                Plotter.AddTargetAllocation();
                Plotter.AddTradeLog();
                Plotter.AddHistoricalAllocations();

                // NOTE: we can access the v2 wrapper via the
                //       asset's meta information
                var algo1v2 = Asset(algo1).Meta.Generator;
                var algo2v2 = Asset(algo2).Meta.Generator;
                NumChildTrades = algo1v2.Account.TradeLog.Count + algo2v2.Account.TradeLog.Count;
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
            Assert.AreEqual(125.00, double.Parse(((string)alloc[0]["Allocation"]).TrimEnd('%')), 1e-5); // v2 test: 125.35
            Assert.AreEqual(3839.50, double.Parse(((string)alloc[0]["Price"]).TrimStart('$')), 1e-03); // value from 12/29/2021

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
        #region nested v1 algorithms
        private class Testbed_nested_v1 : Algorithm
        {
            private class Child_1 : Simulator.Algorithm
            {
                //public override bool CanRunAsChild => true;
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = startTime ?? DateTime.Parse("2023-01-01T16:00");
                    EndTime = endTime ?? DateTime.Parse("2023-12-31T16:00");
                    WarmupStartTime = StartTime;

                    var deposit = 1e6;
                    Deposit(deposit);
                    CommissionPerShare = 0.0;

                    var stocks = AddDataSource("SPY");
                    var bonds = AddDataSource(new Child_2());

                    foreach (var st in SimTimes)
                    {
                        if (Positions.Count == 0 || SimTime[0].Month != NextSimTime.Month)
                        {
                            // NOTE: make sure to never have an order size of zero
                            var spyOrder = (int)Math.Floor(0.60 * NetAssetValue[0] / stocks.Instrument.Close[0])
                                - stocks.Instrument.Position;
                            stocks.Instrument.Trade(spyOrder != 0 ? spyOrder : -1, Simulator.OrderType.openNextBar);

                            var bndOrder = (int)Math.Floor(0.40 * NetAssetValue[0] / bonds.Instrument.Close[0])
                                - bonds.Instrument.Position;
                            bonds.Instrument.Trade(bndOrder, Simulator.OrderType.openNextBar);
                        }

                        var v = NetAssetValue[0] / deposit;
                        yield return Simulator.Bar.NewOHLC(
                            Name,
                            SimTime[0],
                            v, v, v, v, 0);
                    }
                }
            }
            private class Child_2 : Simulator.Algorithm
            {
                public override bool CanRunAsChild => true;
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = startTime ?? DateTime.Parse("2023-01-01T16:00-05:00");
                    EndTime = endTime ?? DateTime.Parse("2023-12-31T16:00-05:00");
                    WarmupStartTime = StartTime;

                    var deposit = 1e6;
                    Deposit(deposit);
                    CommissionPerShare = 0.0;

                    var bonds = AddDataSource(new Child_3());

                    foreach (var st in SimTimes)
                    {
                        if (Positions.Count == 0 || SimTime[0].Month != NextSimTime.Month)
                        {
                            var bndOrder = (int)Math.Floor(1.00 * NetAssetValue[0] / bonds.Instrument.Close[0])
                                - bonds.Instrument.Position;
                            bonds.Instrument.Trade(bndOrder, Simulator.OrderType.openNextBar);
                        }

                        var v = NetAssetValue[0] / deposit;
                        yield return Simulator.Bar.NewOHLC(
                            Name,
                            SimTime[0],
                            v, v, v, v, 0);
                    }
                }
            }
            private class Child_3 : Simulator.Algorithm
            {
                public override bool CanRunAsChild => true;
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = startTime ?? DateTime.Parse("2023-01-01T16:00-05:00");
                    EndTime = endTime ?? DateTime.Parse("2023-12-31T16:00-05:00");
                    WarmupStartTime = StartTime;

                    var deposit = 1e6;
                    Deposit(deposit);
                    CommissionPerShare = 0.0;

                    var bonds = AddDataSource("IEF");

                    foreach (var st in SimTimes)
                    {
                        if (Positions.Count == 0 || SimTime[0].Month != NextSimTime.Month)
                        {
                            // NOTE: make sure to never have an order size of zero
                            var iefOrder = (int)Math.Floor(1.00 * NetAssetValue[0] / bonds.Instrument.Close[0])
                                - bonds.Instrument.Position;
                            bonds.Instrument.Trade(iefOrder != 0 ? iefOrder : -1, Simulator.OrderType.openNextBar);
                        }

                        var v = NetAssetValue[0] / deposit;
                        yield return Simulator.Bar.NewOHLC(
                            Name,
                            SimTime[0],
                            v, v, v, v, 0);
                    }
                }
            }

            public override void Run()
            {
                StartDate = StartDate ?? DateTime.Parse("2023-01-01T16:00-05:00");
                EndDate = EndDate ?? DateTime.Parse("2023-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                ((Account_Default)Account).Friction = 0.0;

                var child1 = Asset(new Child_1());

                SimLoop(() =>
                {
                    if (IsFirstBar)
                    {
                        child1.Allocate(1.0, OrderType.closeThisBar);
                    }
                });

                Plotter.AddTargetAllocation();
                Plotter.AddTradeLog();
                Plotter.AddHistoricalAllocations();
            }
        }
        [TestMethod]
        public void Test_nested_v1()
        {
            var algo = new Testbed_nested_v1();
            //algo.EndDate = DateTime.Parse("2023-01-05T16:00-05:00");
            algo.Run();

            var alloc = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS];
            Assert.AreEqual(2, alloc.Count);
            Assert.AreEqual("SPY", alloc[0]["Symbol"]);
            Assert.AreEqual(60.00, double.Parse(alloc[0]["Allocation"].ToString().Replace("%", "")), 1.50);
            // NOTE: prices change due to back-adjusting
            //Assert.AreEqual(472.31, double.Parse(alloc[0]["Price"].ToString().Replace("$", "")), 0.10);
            Assert.AreEqual("IEF", alloc[1]["Symbol"]);
            Assert.AreEqual(40.00, double.Parse(alloc[1]["Allocation"].ToString().Replace("%", "")), 1.50);
            //Assert.AreEqual(94.20, double.Parse(alloc[1]["Price"].ToString().Replace("$", "")), 0.10);

            var last = algo.Plotter.AllData[Simulator.Plotter.SheetNames.LAST_REBALANCE];
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(DateTime.Parse("2023-12-29T16:00-05:00"), (DateTime)last[0]["Value"]);

            var log = algo.Plotter.AllData["Trade Log"];
            Assert.AreEqual(1, log.Count);

            var history = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS_HISTORY];
            Assert.AreEqual(13, history.Count);
            // TODO: check all entries here
            Assert.AreEqual(DateTime.Parse("2023-05-31T16:00-04:00"), (DateTime)history[5]["Date"]);
            Assert.AreEqual(60.00, double.Parse(history[5]["Allocation"].ToString().Split(',')[0].Replace("SPY=", "").Replace("%", "")), 0.10);
            Assert.AreEqual(40.00, double.Parse(history[5]["Allocation"].ToString().Split(',')[1].Replace("IEF=", "").Replace("%", "")), 0.10);
        }
        #endregion
#if false
        #region v1/ v2 meta portfolio
        // NOTE: this is a complicated issue that arose during testing
        //       with TuringTrader.com algorithms. It was removed from
        //       the standard test cases, as it requires a number of
        //       proprietary algorithms, which we can't disclose here.
        private class Testbed_v1_v2_Meta : BooksAndPubsV2.LazyPortfolio
        {
            private class RoundRobinBullPortfolioMockUp : BooksAndPubs.LazyPortfolio
            {
                public int FILTER_LENGTH => 20;
                public int FILTER_STAGES => 5;
                public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
                {
                    new Tuple<object, double>(
                        new TuringTrader.com.Algorithms.TTcom_RoundRobin.BullMarketStrategy
                        {
                            FILTER_LENGTH = FILTER_LENGTH,
                            FILTER_STAGES = FILTER_STAGES,
                            ASSETS = TuringTrader.com.Algorithms.TTcom_RoundRobin_Blocks.SPDR_Sectors,
                        },
                        0.50),
                    new Tuple<object, double>(
                        new TuringTrader.com.Algorithms.TTcom_RoundRobin.BullMarketStrategy
                        {
                            FILTER_LENGTH = FILTER_LENGTH,
                            FILTER_STAGES = FILTER_STAGES,
                            ASSETS = TuringTrader.com.Algorithms.TTcom_RoundRobin_Blocks.USA_Diversified,
                        },
                        0.50),
                };
            }
            public override HashSet<Tuple<object, double>> ALLOCATION { get; set; } = new HashSet<Tuple<object, double>>
            {
#if true
                new Tuple<object, double>(new RoundRobinBullPortfolioMockUp(), 1.00),
#else
                // initial capital ~300k
                //new Tuple<object, double>(new TTcom_RoundRobin_v3(),           0.18), // https://www.turingtrader.com/portfolios/tt-round-robin/
                //new Tuple<object, double>(new TTcom_StocksOnTheLoose_v3(),     0.18), // https://www.turingtrader.com/portfolios/tt-stocks-on-the-loose/
                //new Tuple<object, double>(new TTcom_MeanKitty_v2(),            0.18), // https://www.turingtrader.com/portfolios/tt-mean-kitty/
                //new Tuple<object, double>(new TTcom_QuickChange(),             0.18), // https://www.turingtrader.com/portfolios/tt-quick-change/
                //new Tuple<object, double>(new TTcom_VixSpritz_v2_Aggressive(), 0.18), // https://www.turingtrader.com/portfolios/tt-vix-spritz-aggro/
                //--- 5x 18% = 90%
                //new Tuple<object, double>(new TTcom_Mach2_v3(),                0.10), // https://www.turingtrader.com/portfolios/tt-mach-2/
#endif
            };
        }

        [TestMethod]
        public void Test_v1_v2_Meta()
        {
            var algo = new Testbed_v1_v2_Meta();
            algo.EndDate = DateTime.Parse("2024-08-30T16:00-05:00");
            algo.Run();

            var alloc = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS];
            Assert.AreEqual(2, alloc.Count);
            Assert.AreEqual("SPYG", alloc[0]["Symbol"]);
            Assert.AreEqual(50.00, double.Parse(alloc[0]["Allocation"].ToString().Replace("%", "")), 0.25);
            Assert.AreEqual(80.75, double.Parse(alloc[0]["Price"].ToString().Replace("$", "")), 0.10);
            Assert.AreEqual("XHB", alloc[1]["Symbol"]);
            Assert.AreEqual(50.00, double.Parse(alloc[1]["Allocation"].ToString().Replace("%", "")), 0.25);
            Assert.AreEqual(117.39, double.Parse(alloc[1]["Price"].ToString().Replace("$", "")), 0.10);

            var last = algo.Plotter.AllData[Simulator.Plotter.SheetNames.LAST_REBALANCE];
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(DateTime.Parse("2024-08-30T16:00-04:00"), (DateTime)last[0]["Value"]);

            var log = algo.Plotter.AllData["Trade Log"];
            Assert.AreEqual(213, log.Count);

            var history = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS_HISTORY];
            Assert.AreEqual(213, history.Count);
            Assert.AreEqual(DateTime.Parse("2007-05-31T16:00-04:00"), (DateTime)history[5]["Date"]);
            Assert.AreEqual(50.00, double.Parse(history[5]["Allocation"].ToString().Split(',')[0].Replace("XLE=", "").Replace("%", "")), 0.10);
            Assert.AreEqual(50.00, double.Parse(history[5]["Allocation"].ToString().Split(',')[1].Replace("DIA=", "").Replace("%", "")), 0.10);
        }
        #endregion
#endif
        #region v1/ v2 sim range
        private class Testbed_simrange : Algorithm
        {
            public class Child_v1 : Simulator.Algorithm
            {
                public DateTime FirstTime = DateTime.MaxValue;
                public DateTime LastTime = DateTime.MinValue;
                public override bool CanRunAsChild => true;
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = startTime ?? DateTime.Parse("2023-01-01T16:00-05:00");
                    EndTime = endTime ?? DateTime.Parse("2023-12-31T16:00-05:00");
                    Deposit(1e6);

                    var spx = AddDataSource("$OEXTR");

                    foreach (var st in SimTimes)
                    {
                        if (SimTime[0] < FirstTime) FirstTime = SimTime[0];
                        if (SimTime[0] > LastTime) LastTime = SimTime[0];

                        spx.Instrument.Trade((int)Math.Round(NetAssetValue[0] / spx.Instrument.Close[0] - spx.Instrument.Position), Simulator.OrderType.openNextBar);

                        var v = NetAssetValue[0];
                        yield return Simulator.Bar.NewOHLC(
                            Name,
                            SimTime[0],
                            v, v, v, v, 0);
                    }
                }
            }
            private class Child_v2 : Algorithm
            {
                public DateTime FirstDate = DateTime.MaxValue;
                public DateTime LastDate = DateTime.MinValue;
                public override void Run()
                {
                    StartDate = StartDate ?? DateTime.Parse("2023-01-01T16:00-05:00");
                    EndDate = EndDate ?? DateTime.Parse("2023-12-31T16:00-05:00");

                    SimLoop(() =>
                    {
                        if (SimDate < FirstDate) FirstDate = SimDate;
                        if (SimDate > LastDate) LastDate = SimDate;

                        Asset("$NDXTR").Allocate(1.0, OrderType.openNextBar);
                    });
                }
            }

            public DateTime v1FirstTime = DateTime.MaxValue;
            public DateTime v1LastTime = DateTime.MinValue;
            public DateTime v2FirstDate = DateTime.MaxValue;
            public DateTime v2LastDate = DateTime.MinValue;
            public DateTime FirstDate = DateTime.MaxValue;
            public DateTime LastDate = DateTime.MinValue;
            public override void Run()
            {
                StartDate = StartDate ?? DateTime.Parse("2023-01-01T16:00-05:00");
                EndDate = EndDate ?? DateTime.Parse("2023-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(90);

                var v1Child = new Child_v1();
                var v2Child = new Child_v2();

                SimLoop(() =>
                {
                    if (SimDate < FirstDate) FirstDate = SimDate;
                    if (SimDate > LastDate) LastDate = SimDate;

                    Asset(v1Child).Allocate(0.5, OrderType.openNextBar);
                    Asset(v2Child).Allocate(0.5, OrderType.openNextBar);
                });

                v1FirstTime = v1Child.FirstTime;
                v1LastTime = v1Child.LastTime;
                v2FirstDate = v2Child.FirstDate;
                v2LastDate = v2Child.LastDate;

                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
        [TestMethod]
        public void Test_simrange()
        {
            var algo = new Testbed_simrange();
            algo.StartDate = DateTime.Parse("2023-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2023-12-31T16:00-05:00");
            algo.Run();

            Assert.AreEqual(DateTime.Parse("2023-01-03T16:00-05:00"), algo.FirstDate); // no padding
            Assert.AreEqual(DateTime.Parse("2023-12-29T16:00-05:00"), algo.LastDate); // no padding

            Assert.AreEqual(DateTime.Parse("2022-10-03T16:00"), algo.v1FirstTime); // includes warmup
            Assert.AreEqual(DateTime.Parse("2023-12-29T16:00"), algo.v1LastTime); // no padding

            Assert.AreEqual(DateTime.Parse("2022-10-03T16:00-04:00"), algo.v2FirstDate); // includes warmup
            Assert.AreEqual(DateTime.Parse("2023-12-29T16:00-05:00"), algo.v2LastDate); // no padding
        }
        #endregion
    }
}

//==============================================================================
// end of file