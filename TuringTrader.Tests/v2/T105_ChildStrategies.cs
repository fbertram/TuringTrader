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

            public int NumTrades = 0;

            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-31T16:00-05:00");
                Account.Friction = 0.0;

                var algo1 = new SwitchHalfTime { HoldFirst = true, };
                var algo2 = new SwitchHalfTime { HoldFirst = false, };

                SimLoop(() =>
                {
                    Asset(algo1).Allocate(1.0, OrderType.closeThisBar);
                    Asset(algo2).Allocate(1.0, OrderType.closeThisBar);
                });

                NumTrades = algo1.Account.TradeLog.Count + algo2.Account.TradeLog.Count;

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

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 799.673401689968) < 1e-5);
            Assert.IsTrue(algo.NumTrades == 3);

            var alloc = algo.Plotter.AllData[Simulator.Plotter.SheetNames.HOLDINGS];
            Assert.IsTrue(alloc.Count == 1);
            Assert.IsTrue((string)alloc[0]["Symbol"] == "$SPX");
            Assert.IsTrue(Math.Abs(double.Parse(((string)alloc[0]["Allocation"]).TrimEnd('%')) - 100.0) < 1e-5);

            // TODO: add checks of historical allocation here
            // TODO: add checks of trading log here

            /*
            Assert.IsTrue(algo.Account.TradeLog.Count == 2);

            var order1 = algo.Account.TradeLog[0];
            Assert.IsTrue(order1.ExecDate.Date == DateTime.Parse("2022-12-14"));
            Assert.IsTrue(Math.Abs(order1.FillPrice - 4015.5400390625) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.OrderAmount - 999.50024987506242) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.FrictionAmount - 0.49975012493753124) < 1e-5);

            var order2 = algo.Account.TradeLog[1];
            Assert.IsTrue(order2.ExecDate.Date == DateTime.Parse("2022-12-16"));
            Assert.IsTrue(Math.Abs(order2.FillPrice - 3890.909912109375) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.OrderAmount - -968.47880772286521) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.FrictionAmount - 0.4842394038614326) < 1e-5);
            */
        }

        // TODO: add test for including child algorithm via "algo:xxx" nickname
        // TODO: add test for including child algorithm based on v1 engine
    }
}

//==============================================================================
// end of file