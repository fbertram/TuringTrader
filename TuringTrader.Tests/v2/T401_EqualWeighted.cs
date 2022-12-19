//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T401_EqualWeighted
// Description: Strategy test for equal-weighted index.
// History:     2022xii18, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC dba TuringTrader.
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
    public class T401_EqualWeighted
    {
        private class Testbed : Algorithm
        {
            public List<DateTime> TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("1990-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var constituents = Universe("$OEX");

                    var assets = constituents
                        .Concat(Positions.Keys)
                        .ToHashSet();

                    //if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                    {
                        foreach (var asset in assets)
                        {
                            Asset(asset).Allocate(
                                constituents.Contains(asset) ? 1.0 / constituents.Count : 0.0,
                                OrderType.closeThisBar);
                        }
                    }
                });
            }
        }

        [TestMethod]
        public void Test_Speed()
        {
            var algo = new Testbed();

            var start = DateTime.Now;
            algo.Run();
            var end = DateTime.Now;

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 38957.689261914849) < 1e-3);
            Assert.IsTrue((end - start).TotalSeconds < 41.0); // ~37s Surface Pro 8, i5-1135G7 @ 2.40GHz
        }
    }
}

//==============================================================================
// end of file