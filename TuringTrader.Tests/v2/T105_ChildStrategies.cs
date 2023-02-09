//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T105_ChildStrategies
// Description: Unit test for child strategies.
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
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T105_ChildStrategies
    {
        private class Testbed_Instance : Algorithm
        {
            private class SwitchHalfTime : Algorithm
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
                Account.Friction = 0.0;

                var algo1 = new SwitchHalfTime { HoldFirst = true, };
                var algo2 = new SwitchHalfTime { HoldFirst = false, };

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
        public void Test_Instance()
        {
            var algo = new Testbed_Instance();
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

        // TODO: add test for including child algorithm via "algo:xxx" nickname
        // TODO: add test for including child algorithm based on v1 engine
    }
}

//==============================================================================
// end of file