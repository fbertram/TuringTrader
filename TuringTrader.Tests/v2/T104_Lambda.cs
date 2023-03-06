//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T104_Lambda
// Description: Unit test for lambda functions.
// History:     2022xii21, FUB, created
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

        private class TestbedUniverse : Algorithm
        {
            public double Result;
            public override void Run()
            {
                StartDate = DateTime.Parse("2021-01-04T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var equalWeightedIndex = Lambda("ew-idx", (prev) =>
                    {
                        var universe = Universe("$DJI");
                        return prev
                            * (1.0 + universe
                                .Average(asset => Asset(asset).Close.RelReturn()[0]));
                    }, 1.0)[0];

                    if (IsLastBar)
                        Result = equalWeightedIndex;
                });
            }
        }

        [TestMethod]
        public void Test_LambdaUniverse()
        {
            var algo = new TestbedUniverse();
            algo.Run();

            Assert.IsTrue(Math.Abs(algo.Result - 1.2130976664261734) < 1e-5);
        }

        private class Testbed_Bugfix01 : Algorithm
        {
            // taken from Demo09_CustomIndicators.cs
            private TimeSeriesFloat CustomEMA(TimeSeriesFloat series, int period)
            {
                var name = string.Format("{0}.CustomEMA({1})", series.Name, period);
                var alpha = 2.0 / (1.0 + period);

                return Lambda(
                    name,
                    (prevEMA) => IsFirstBar
                        ? series[0]
                        : prevEMA + alpha * (series[0] - prevEMA),
                    -999.99);
            }

            private TimeSeriesFloat CustomSMA(TimeSeriesFloat series, int period)
            {
                var name = string.Format("{0}.CustomSMA({1})", series.Name, period);

                return Lambda(
                    name,
                    () => Enumerable.Range(0, period).Average(t => series[t]));
            }

            public override void Run()
            {
                StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

                SimLoop(() =>
                {
                    var input = Asset("$SPX").Close;

                    return new OHLCV(
                        CustomEMA(input, 200)[0] - input[0],
                        CustomSMA(input, 200)[0] - input[0],
                        0.0, 0.0, 0.0);
                });
            }
        }

        [TestMethod]
        public void Test_Bugfix01()
        {
            var algo = new Testbed_Bugfix01();
            algo.Run();

            var sumEMA = algo.EquityCurve.Sum(b => b.Value.Open);
            Assert.AreEqual(sumEMA, -257008.86804729505, 1e-5);

            var sumSMA = algo.EquityCurve.Sum(b => b.Value.High);
            Assert.AreEqual(sumSMA, -248093.10192443867, 1e-5);
        }
    }
}

//==============================================================================
// end of file