﻿//==============================================================================
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
        #region test Sum
        private class Testbed_Sum_V2vsV1 : Algorithm
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
                            Instruments.First().Close.Sum(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.Sum(5).Data;
            }
        }

        [TestMethod]
        public void Test_Sum_V2vsV1()
        {
            var algo = new Testbed_Sum_V2vsV1();
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
        #region test WMA
        private class Testbed_WMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.WMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.WMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_WMA_V2vsV1()
        {
            var algo = new Testbed_WMA_V2vsV1();
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

        #region test DEMA
        private class Testbed_DEMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.DEMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.DEMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_DEMA_V2vsV1()
        {
            var algo = new Testbed_DEMA_V2vsV1();
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
        #region test HMA
        private class Testbed_HMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.HMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.HMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_HMA_V2vsV1()
        {
            var algo = new Testbed_HMA_V2vsV1();
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
        #region test TEMA
        private class Testbed_TEMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.TEMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.TEMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_TEMA_V2vsV1()
        {
            var algo = new Testbed_TEMA_V2vsV1();
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
        #region test ZLEMA
        private class Testbed_ZLEMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.ZLEMA(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.ZLEMA(5).Data;
            }
        }

        [TestMethod]
        public void Test_ZLEMA_V2vsV1()
        {
            var algo = new Testbed_ZLEMA_V2vsV1();
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
        #region test KAMA
        private class Testbed_KAMA_V2vsV1 : Algorithm
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
                            Instruments.First().Close.KAMA()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.KAMA().Data;
            }
        }

        [TestMethod]
        public void Test_KAMA_V2vsV1()
        {
            var algo = new Testbed_KAMA_V2vsV1();
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

        #region test MACD
        private class Testbed_MACD_V2vsV1 : Algorithm
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
                            Instruments.First().Close.MACD().MACD[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.MACD().MACD.Data;
            }
        }
        private class Testbed_MACD_Signal_V2vsV1 : Algorithm
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
                            Instruments.First().Close.MACD().Signal[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.MACD().Signal.Data;
            }
        }

        [TestMethod]
        public void Test_MACD_V2vsV1()
        {
            var algo = new Testbed_MACD_V2vsV1();
            algo.Run();
            var v1Result = algo.v1Result;
            var v2Result = algo.v2Result;

            Assert.AreEqual(v1Result.Count, v2Result.Count);

            for (var i = 0; i < v2Result.Count; i++)
            {
                Assert.AreEqual(v1Result[i].Date, v2Result[i].Date);
                Assert.AreEqual(v1Result[i].Value, v2Result[i].Value, 1e-5);
            }

            var algo2 = new Testbed_MACD_Signal_V2vsV1();
            algo2.Run();
            var v1Result2 = algo.v1Result;
            var v2Result2 = algo.v2Result;

            Assert.AreEqual(v1Result2.Count, v2Result2.Count);

            for (var i = 0; i < v2Result2.Count; i++)
            {
                Assert.AreEqual(v1Result2[i].Date, v2Result2[i].Date);
                Assert.AreEqual(v1Result2[i].Value, v2Result2[i].Value, 1e-5);
            }
        }
        #endregion
        #region test Supertrend
        [TestMethod]
        public void Test_Supertrend()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var asset = algo.Asset("$SPX");
            var supertrend = asset.Supertrend(10, 10.0);

            var upperBasic = supertrend.BasicUpperBand.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(upperBasic.Count, 20);
            Assert.AreEqual(5302.502868652344, upperBasic.Average(b => b.Value), 1e-3);
            Assert.AreEqual(5693.72509765625, upperBasic.Max(b => b.Value), 1e-3);
            Assert.AreEqual(5110.100341796875, upperBasic.Min(b => b.Value), 1e-3);

            var lowerBasic = supertrend.BasicLowerBand.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(lowerBasic.Count, 20);
            Assert.AreEqual(3845.1151733398438, lowerBasic.Average(b => b.Value), 1e-3);
            Assert.AreEqual(4360.843994140625, lowerBasic.Max(b => b.Value), 1e-3);
            Assert.AreEqual(3186.810302734375, lowerBasic.Min(b => b.Value), 1e-3);

            var signal = supertrend.SignalLine.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(signal.Count, 20);
            Assert.AreEqual(4695.85205078125, signal.Average(b => b.Value), 1e-3);
            Assert.AreEqual(5245.968505859375, signal.Max(b => b.Value), 1e-3);
            Assert.AreEqual(4399.635498046875, signal.Min(b => b.Value), 1e-3);

            var direction = supertrend.Direction.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(20, direction.Count);
            Assert.AreEqual(0.3, direction.Average(b => b.Value), 1e-3);
        }
        #endregion
    }
}

//==============================================================================
// end of file