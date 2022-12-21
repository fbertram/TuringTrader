//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T104_Lambda
// Description: Unit test for lambda functions.
// History:     2022xii21, FUB, created
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
using System.Linq;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T104_Lambda
    {
        private class TestbedEMA : Algorithm
        {
            public bool ResultsMatch = true;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var emaA = Asset("$SPX").Close.EMA(10)[0];

                    var emaB = Lambda("ema-b", (ema) =>
                    {
                        var alpha = 2.0 / (1.0 + 10);
                        return ema + alpha * (Asset("$SPX").Close[0] - ema);
                    }, 3700.64990234375)[0];

                    if (Math.Abs(emaA - emaB) > 1e-3)
                        ResultsMatch = false;
                });
            }
        }

        [TestMethod]
        public void Test_LambdaEMA()
        {
            var algo = new TestbedEMA();
            algo.Run();

            Assert.IsTrue(algo.ResultsMatch == true);
        }

        private class TestbedSMA : Algorithm
        {
            public bool ResultsMatch = true;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var smaA = Asset("$SPX").Close.SMA(10)[0];

                    var smaB = Lambda("sma-b", () =>
                    {
                        return Enumerable.Range(0, 10)
                            .Average(t => Asset("$SPX").Close[t]);
                    })[0];

                    if (Math.Abs(smaA - smaB) > 1e-3)
                        ResultsMatch = false;
                });
            }
        }

        [TestMethod]
        public void Test_LambdaSMA()
        {
            var algo = new TestbedSMA();
            algo.Run();

            Assert.IsTrue(algo.ResultsMatch == true);
        }
    }
}

//==============================================================================
// end of file