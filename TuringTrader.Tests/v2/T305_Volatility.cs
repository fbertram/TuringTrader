//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T305_Volatility
// Description: Unit test for volatility indicators.
// History:     2023ii11, FUB, created
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
    public class T305_Volatility
    {
        #region test StandardDeviation
        [TestMethod]
        public void Test_StandardDeviation()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var asset = algo.Asset("$SPX");
            var std = asset.Close.StandardDeviation(21).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(std.Count, 20);
            Assert.AreEqual(95.5827468751985, std.Average(b => b.Value), 1e-5);
            Assert.AreEqual(162.52062970246257, std.Max(b => b.Value), 1e-5);
            Assert.AreEqual(63.713300800076084, std.Min(b => b.Value), 1e-5);
        }
        private class Testbed_StandardDeviation_V2vsV1 : Algorithm
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
                            Instruments.First().Close.StandardDeviation(10)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(20);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.StandardDeviation(10).Data;
            }
        }

        [TestMethod]
        public void Test_StandardDeviation_V2vsV1()
        {
            var algo = new Testbed_StandardDeviation_V2vsV1();
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
        #region test Volatility
        [TestMethod]
        public void Test_Volatility()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var asset = algo.Asset("$SPX");
            var vol = asset.Close.Volatility(21).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(vol.Count, 20);
            Assert.AreEqual(0.009779318771068187, vol.Average(b => b.Value), 1e-5);
            Assert.AreEqual(0.011489691450402354, vol.Max(b => b.Value), 1e-5);
            Assert.AreEqual(0.008334928184057918, vol.Min(b => b.Value), 1e-5);
        }
        #endregion
        #region test TrueRange
        private class Testbed_Volatility_V2vsV1 : Algorithm
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
                            Instruments.First().Close.Volatility(10)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(20);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.Volatility(10).Data;
            }
        }

        [TestMethod]
        public void Test_Volatility_V2vsV1()
        {
            var algo = new Testbed_Volatility_V2vsV1();
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
        #region test AverageTrueRange
        private class Testbed_AverageTrueRange_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Result;
            public List<BarType<double>> v2Result;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    AddDataSource("$SPXTR");

                    foreach (var st in SimTimes)
                        yield return Simulator.Bar.NewValue(
                            GetType().Name,
                            SimTime[0],
                            Instruments.First().AverageTrueRange(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPXTR").AverageTrueRange(5).Data;
            }
        }

        [TestMethod]
        public void Test_AverageTrueRange_V2vsV1()
        {
            var algo = new Testbed_AverageTrueRange_V2vsV1();
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
        #region test BollingerBands
        [TestMethod]
        public void Test_BollingerBands()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var asset = algo.Asset("$SPX");
            var bollinger = asset.Close.BollingerBands(20, 1.0);

            var upper = bollinger.Upper.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(upper.Count, 20);
            Assert.AreEqual(4776.819515292047, upper.Average(b => b.Value), 1e-5);
            Assert.AreEqual(4798.020536843549, upper.Max(b => b.Value), 1e-5);
            Assert.AreEqual(4728.821722741259, upper.Min(b => b.Value), 1e-5);

            var lower = bollinger.Lower.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(lower.Count, 20);
            Assert.AreEqual(4586.152679532175, lower.Average(b => b.Value), 1e-5);
            Assert.AreEqual(4652.292302958482, lower.Max(b => b.Value), 1e-5);
            Assert.AreEqual(4418.809185461867, lower.Min(b => b.Value), 1e-5);

            var middle = bollinger.Middle.Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(middle.Count, 20);
            Assert.AreEqual(4681.486097412109, middle.Average(b => b.Value), 1e-5);
            Assert.AreEqual(4716.387011718751, middle.Max(b => b.Value), 1e-5);
            Assert.AreEqual(4573.815454101563, middle.Min(b => b.Value), 1e-5);
        }
        private class Testbed_BollingerBands_Upper_V2vsV1 : Algorithm
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
                            Instruments.First().Close.BollingerBands(10, 2.0).Upper[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.BollingerBands(10, 2.0).Upper.Data;
            }
        }
        private class Testbed_BollingerBands_Lower_V2vsV1 : Algorithm
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
                            Instruments.First().Close.BollingerBands(10, 2.0).Lower[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.BollingerBands(10, 2.0).Lower.Data;
            }
        }

        [TestMethod]
        public void Test_BollingerBands_V2vsV1()
        {
            {
                var algo = new Testbed_BollingerBands_Upper_V2vsV1();
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
            {
                var algo = new Testbed_BollingerBands_Lower_V2vsV1();
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
        }
        #endregion
        #region test ValueAtRisk
        [TestMethod]
        public void Test_ValueAtRisk()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2022-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var asset = algo.Asset("$SPX");
            var var = asset.ValueAtRisk(21, 0.95).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();
            Assert.AreEqual(var.Count, 20);
            Assert.AreEqual(0.010204094182301643, var.Average(b => b.Value), 1e-5);
            Assert.AreEqual(0.012947836531692403, var.Max(b => b.Value), 1e-5);
            Assert.AreEqual(0.00650605717196906, var.Min(b => b.Value), 1e-5);
        }
        #endregion
    }
}

//==============================================================================
// end of file