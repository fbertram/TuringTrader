//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T304_Trend
// Description: Unit test for trend indicators.
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
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T304_Trend
    {
        #region test EMA
        private class Testbed_EMA : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public Algorithm Generator;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                Generator.StartDate = StartDate;
                Generator.EndDate = EndDate;

                TestResult = Asset(Generator).Close.EMA(20);
            }
        }

        [TestMethod]
        public void Test_EMA_StepResponse()
        {
            var algo = new Testbed_EMA();
            algo.Generator = new T000_Helpers.StepResponse();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.ema(20)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.85066836567421422) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 10.918650526094964) < 1e-5);
        }

        [TestMethod]
        public void Test_EMA_NyquistResponse()
        {
            var algo = new Testbed_EMA();
            algo.Generator = new T000_Helpers.NyquistFrequency();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.ema(20)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.4540674736952518) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 5.6863589998951092) < 1e-5);
        }

        private class Testbed_EMA_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Result;
            public List<BarType<double>> v2Result;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    AddDataSource("$SPX");

                    foreach (var st in SimTimes)
                        yield return Simulator.Bar.NewValue(
                            GetType().Name,
                            SimTime[0],
                            Instruments.First().Close.EMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.EMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_EMA_V2vsV1()
        {
            var algo = new Testbed_EMA_V2vsV1();
            algo.Run();
            var v1Result = algo.v1Result;
            var v2Result = algo.v2Result;

            Assert.AreEqual(v1Result.Count, v2Result.Count);

            for (var i = 0; i < v2Result.Count; i++)
            {
                Assert.AreEqual(v1Result[i].Date, v2Result[i].Date);
                Assert.AreEqual(v1Result[i].Value, v2Result[i].Value, 1e-5);
            }
        }
        #endregion
        #region test SMA
        private class Testbed_SMA : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public Algorithm Generator;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                Generator.StartDate = StartDate;
                Generator.EndDate = EndDate;

                TestResult = Asset(Generator).Close.SMA(20);
            }
        }

        [TestMethod]
        public void Test_SMA_StepResponse()
        {
            var algo = new Testbed_SMA();
            algo.Generator = new T000_Helpers.StepResponse();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.sma(20)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.95) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 9.4999999999999982) < 1e-5);
        }

        [TestMethod]
        public void Test_SMA_NyquistResponse()
        {
            var algo = new Testbed_SMA();
            algo.Generator = new T000_Helpers.NyquistFrequency();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.sma(20)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.5) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 5.0) < 1e-5);
        }

        private class Testbed_SMA_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Result;
            public List<BarType<double>> v2Result;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    AddDataSource("$SPX");

                    foreach (var st in SimTimes)
                        yield return Simulator.Bar.NewValue(
                            GetType().Name,
                            SimTime[0],
                            Instruments.First().Close.SMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.SMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_SMA_V2vsV1()
        {
            var algo = new Testbed_SMA_V2vsV1();
            algo.Run();
            var v1Result = algo.v1Result;
            var v2Result = algo.v2Result;

            Assert.AreEqual(v1Result.Count, v2Result.Count);

            for (var i = 0; i < v2Result.Count; i++)
            {
                Assert.AreEqual(v1Result[i].Date, v2Result[i].Date);
                Assert.AreEqual(v1Result[i].Value, v2Result[i].Value, 1e-5);
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file