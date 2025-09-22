//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T304_Trend
// Description: Unit test for volume indicators.
// History:     2023ii27, EFB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2025, Bertram Enterprises LLC dba TuringTrader.
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
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T306_Volume
    {
        #region AccumulationDistributionIndex
        private class Testbed_AccumulationDistributionIndex_V2vsV1 : Algorithm
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
                            Instruments.First().AccumulationDistributionIndex()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").AccumulationDistributionIndex().Data;
            }
        }

        [TestMethod]
        public void Test_AccumulationDistributionIndex_V2vsV1()
        {
            var algo = new Testbed_AccumulationDistributionIndex_V2vsV1();
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
        #region ChaikinOscillator
        private class Testbed_ChaikinOscillator_V2vsV1 : Algorithm
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
                            Instruments.First().ChaikinOscillator()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").ChaikinOscillator().Data;
            }
        }

        [TestMethod]
        public void Test_ChaikinOscillator_V2vsV1()
        {
            var algo = new Testbed_ChaikinOscillator_V2vsV1();
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
        #region OnBalanceVolume
        private class Testbed_OnBalanceVolume_V2vsV1 : Algorithm
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
                            Instruments.First().OnBalanceVolume()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").OnBalanceVolume().Data;
            }
        }

        [TestMethod]
        public void Test_OnBalanceVolume_V2vsV1()
        {
            var algo = new Testbed_OnBalanceVolume_V2vsV1();
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
        #region MoneyFlowIndex
        private class Testbed_MoneyFlowIndex_V2vsV1 : Algorithm
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
                            Instruments.First().MoneyFlowIndex(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").MoneyFlowIndex(5).Data;
            }
        }

        [TestMethod]
        public void Test_MoneyFlowIndex_V2vsV1()
        {
            var algo = new Testbed_MoneyFlowIndex_V2vsV1();
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
        #region VWAP
        [TestMethod]
        public void Test_VWAP()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2023-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2023-12-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);

            var asset = algo.Asset("$SPX");
            var vwap = asset.VWAP(21).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(250, vwap.Count);
            Assert.AreEqual(4247.374150863023, vwap.Average(b => b.Value), 1e-5);
            Assert.AreEqual(4669.002894940431, vwap.Max(b => b.Value), 1e-5);
            Assert.AreEqual(3875.20634657336, vwap.Min(b => b.Value), 1e-5);
        }
        #endregion
        #region VWEMA
        [TestMethod]
        public void Test_VWEMA()
        {
            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("2023-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2023-12-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(90);
            algo.CooldownPeriod = TimeSpan.FromDays(0);

            var asset = algo.Asset("$SPX");
            var vwema = asset.VWEMA(21).Data
                .Where(b => b.Date >= algo.StartDate)
                .ToList();

            Assert.AreEqual(250, vwema.Count);
            Assert.AreEqual(4249.001588211432, vwema.Average(b => b.Value), 1e-5);
            Assert.AreEqual(4673.872252452, vwema.Max(b => b.Value), 1e-5);
            Assert.AreEqual(3871.849182989182, vwema.Min(b => b.Value), 1e-5);
        }
        #endregion
    }
}


//==============================================================================
// end of file