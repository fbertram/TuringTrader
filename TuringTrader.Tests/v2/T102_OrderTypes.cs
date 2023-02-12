//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T101_Cache
// Description: Unit test for cache.
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
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T102_OrderTypes
    {
        private class Testbed : Algorithm
        {
            public OrderType OrderType;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-12-06T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-16T16:00-05:00");

                SimLoop(() =>
                {
                    if (SimDate.Date == DateTime.Parse("2022-12-13"))
                    {
                        Asset("$SPX").Allocate(1.0, OrderType);
                    }
                    if (SimDate.Date == DateTime.Parse("2022-12-15"))
                    {
                        Asset("$SPX").Allocate(0.0, OrderType);
                    }
                });
            }
        }

        [TestMethod]
        public void Test_OrderOpenNextBar()
        {
            var algo = new Testbed();
            algo.OrderType = OrderType.openNextBar;
            algo.Run();

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 967.99456831900375) < 1e-5);
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
        }

        [TestMethod]
        public void Test_OrderCloseThisBar()
        {
            var algo = new Testbed();
            algo.OrderType = OrderType.closeThisBar;
            algo.Run();

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 968.20775228019545) < 1e-5);
            Assert.IsTrue(algo.Account.TradeLog.Count == 2);

            var order1 = algo.Account.TradeLog[0];
            Assert.IsTrue(order1.ExecDate.Date == DateTime.Parse("2022-12-13"));
            Assert.IsTrue(Math.Abs(order1.FillPrice - 4019.64990234375) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.OrderAmount - 999.50024987506254) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.FrictionAmount - 0.49975012493753129) < 1e-5);

            var order2 = algo.Account.TradeLog[1];
            Assert.IsTrue(order2.ExecDate.Date == DateTime.Parse("2022-12-15"));
            Assert.IsTrue(Math.Abs(order2.FillPrice - 3895.75) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.OrderAmount - -968.69209832936019) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.FrictionAmount - 0.4843460491646801) < 1e-5);
        }
    }
}

//==============================================================================
// end of file