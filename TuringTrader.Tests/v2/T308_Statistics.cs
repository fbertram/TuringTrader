//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T308_Statistics
// Description: Unit test for statistical indicators.
// History:     2023iii31, FUB, created
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
    public class T308_Statistics
    {
        #region StandardDeviation
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
        #region Correlation
        private class Testbed_Correlation_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Result;
            public List<BarType<double>> v2Result;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    var spx = AddDataSource("$SPX");
                    var dji = AddDataSource("$DJI");

                    foreach (var st in SimTimes)
                        yield return Simulator.Bar.NewValue(
                            GetType().Name,
                            SimTime[0],
                            spx.Instrument.Close.Correlation(dji.Instrument.Close, 10)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.Correlation(Asset("$DJI").Close, 10).Data;
            }
        }

        [TestMethod]
        public void Test_Correlation_V2vsV1()
        {
            var algo = new Testbed_Correlation_V2vsV1();
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
        #region Covariance
        private class Testbed_Covariance_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Result;
            public List<BarType<double>> v2Result;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    var spx = AddDataSource("$SPX");
                    var dji = AddDataSource("$DJI");

                    foreach (var st in SimTimes)
                        yield return Simulator.Bar.NewValue(
                            GetType().Name,
                            SimTime[0],
                            spx.Instrument.Close.Covariance(dji.Instrument.Close, 10)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.Covariance(Asset("$DJI").Close, 10).Data;
            }
        }

        [TestMethod]
        public void Test_Covariance_V2vsV1()
        {
            var algo = new Testbed_Covariance_V2vsV1();
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
        #region ZScore
        [TestMethod]
        public void Test_ZScore()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2021-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);

            var asset = algo.Asset("$SPXTR");
            var z = asset.Close.ZScore(21).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(252, z.Count);
            Assert.AreEqual(0.7279812127562796, z.Average(b => b.Value), 1e-5);
            Assert.AreEqual(2.6775366551310253, z.Max(b => b.Value), 1e-5);
            Assert.AreEqual(-2.975802196796978, z.Min(b => b.Value), 1e-5);
        }
        #endregion
    }
}

//==============================================================================
// end of file