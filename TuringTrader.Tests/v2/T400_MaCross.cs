//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T400_MaCross
// Description: Unit test for simple MA-cross strategy.
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
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T400_MaCross
    {
        private class Testbed : Algorithm
        {
            public override void Run()
            {
                StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(365);

                SimLoop(() =>
                {
                    var asset = Asset("$SPX");
                    var weight = asset.Close.EMA(50)[0] > asset.Close.EMA(200)[0] ? 1.0 : 0.0;

                    if (Math.Abs(asset.Position - weight) > 0.05)
                        asset.Allocate(weight, OrderType.openNextBar);
                });

            }
        }

        [TestMethod]
        public void Test_StepResponse()
        {
            var algo = new Testbed();
            algo.Run();
            var result = algo.Result;
            var account = algo.Account;

            var firstDate = result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2007-01-03T16:00-5:00"));

            var lastDate = result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2021-12-31T16:00-5:00"));

            var barCount = result.Count();
            Assert.IsTrue(barCount == 3777);

            var cagr = account.AnnualizedReturn;
            Assert.IsTrue(Math.Abs(cagr - 0.062978353632788586) < 1e-5);

            var mdd = account.MaxDrawdown;
            Assert.IsTrue(Math.Abs(mdd - 0.29532656508770572) < 1e-5);

            var trades = account.TradeLog.Count;
            Assert.IsTrue(trades == 19);
        }
    }
}

//==============================================================================
// end of file