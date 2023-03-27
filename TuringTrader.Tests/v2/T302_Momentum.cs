//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T302_Momentum
// Description: Unit test for momentum indicators.
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
    public class T302_Momentum
    {
        #region LinRegression
        private class Testbed_LinRegression_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Slope;
            public List<BarType<double>> v1Intercept;
            public List<BarType<double>> v1R2;
            public List<BarType<double>> v2Slope;
            public List<BarType<double>> v2Intercept;
            public List<BarType<double>> v2R2;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    AddDataSource("$SPX");

                    foreach (var st in SimTimes)
                    {
                        var r = Instruments.First().Close.LinRegression(5);
                        yield return Simulator.Bar.NewOHLC(
                            GetType().Name,
                            SimTime[0],
                            r.Slope[0], r.Intercept[0], r.R2[0], 0.0, 0);
                    }
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var v1 = Asset(new Testbed_v1());
                v1Slope = v1.Open.Data;
                v1Intercept = v1.High.Data;
                v1R2 = v1.Low.Data;

                var v2 = Asset("$SPX").Close.LinRegression(5);
                v2Slope = v2.Slope.Data;
                v2Intercept = v2.Intercept.Data;
                v2R2 = v2.R2.Data;
            }
        }

        [TestMethod]
        public void Test_LinRegression_V2vsV1()
        {
            var algo = new Testbed_LinRegression_V2vsV1();
            algo.Run();

            Assert.AreEqual(algo.v1Slope.Count, algo.v2Slope.Count);
            for (var i = 0; i < algo.v2Slope.Count; i++)
            {
                Assert.AreEqual(algo.v1Slope[i].Date, algo.v2Slope[i].Date);
                Assert.AreEqual(algo.v1Slope[i].Value, algo.v2Slope[i].Value, 1e-5);
            }

            Assert.AreEqual(algo.v1Intercept.Count, algo.v2Intercept.Count);
            for (var i = 0; i < algo.v2Intercept.Count; i++)
            {
                Assert.AreEqual(algo.v1Intercept[i].Date, algo.v2Intercept[i].Date);
                Assert.AreEqual(algo.v1Intercept[i].Value, algo.v2Intercept[i].Value, 1e-5);
            }

            Assert.AreEqual(algo.v1R2.Count, algo.v2R2.Count);
            for (var i = 0; i < algo.v2R2.Count; i++)
            {
                Assert.AreEqual(algo.v1R2[i].Date, algo.v2R2[i].Date);
                Assert.AreEqual(algo.v1R2[i].Value, algo.v2R2[i].Value, 1e-5);
            }
        }
        #endregion
        #region LogRegression
        private class Testbed_LogRegression_V2vsV1 : Algorithm
        {
            public List<BarType<double>> v1Slope;
            public List<BarType<double>> v1Intercept;
            public List<BarType<double>> v1R2;
            public List<BarType<double>> v2Slope;
            public List<BarType<double>> v2Intercept;
            public List<BarType<double>> v2R2;
            private class Testbed_v1 : Simulator.Algorithm
            {
                public override IEnumerable<Simulator.Bar> Run(DateTime? startTime, DateTime? endTime)
                {
                    StartTime = (DateTime)startTime;
                    EndTime = (DateTime)endTime;
                    AddDataSource("$SPX");

                    foreach (var st in SimTimes)
                    {
                        var r = Instruments.First().Close.LogRegression(5);
                        yield return Simulator.Bar.NewOHLC(
                            GetType().Name,
                            SimTime[0],
                            r.Slope[0], r.Intercept[0], r.R2[0], 0.0, 0);
                    }
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var v1 = Asset(new Testbed_v1());
                v1Slope = v1.Open.Data;
                v1Intercept = v1.High.Data;
                v1R2 = v1.Low.Data;

                var v2 = Asset("$SPX").Close.LogRegression(5);
                v2Slope = v2.Slope.Data;
                v2Intercept = v2.Intercept.Data;
                v2R2 = v2.R2.Data;
            }
        }

        [TestMethod]
        public void Test_LogRegression_V2vsV1()
        {
            var algo = new Testbed_LogRegression_V2vsV1();
            algo.Run();

            Assert.AreEqual(algo.v1Slope.Count, algo.v2Slope.Count);
            for (var i = 0; i < algo.v2Slope.Count; i++)
            {
                Assert.AreEqual(algo.v1Slope[i].Date, algo.v2Slope[i].Date);
                Assert.AreEqual(algo.v1Slope[i].Value, algo.v2Slope[i].Value, 1e-5);
            }

            Assert.AreEqual(algo.v1Intercept.Count, algo.v2Intercept.Count);
            for (var i = 0; i < algo.v2Intercept.Count; i++)
            {
                Assert.AreEqual(algo.v1Intercept[i].Date, algo.v2Intercept[i].Date);
                Assert.AreEqual(algo.v1Intercept[i].Value, algo.v2Intercept[i].Value, 1e-5);
            }

            Assert.AreEqual(algo.v1R2.Count, algo.v2R2.Count);
            for (var i = 0; i < algo.v2R2.Count; i++)
            {
                Assert.AreEqual(algo.v1R2[i].Date, algo.v2R2[i].Date);
                Assert.AreEqual(algo.v1R2[i].Value, algo.v2R2[i].Value, 1e-5);
            }
        }
        #endregion

        #region CCI
        private class Testbed_CCI_V2vsV1 : Algorithm
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
                            Instruments.First().CCI(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").CCI(5).Data;
            }
        }

        [TestMethod]
        public void Test_CCI_V2vsV1()
        {
            var algo = new Testbed_CCI_V2vsV1();
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
        #region TSI
        private class Testbed_TSI_V2vsV1 : Algorithm
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
                            Instruments.First().Close.TSI(10, 5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.TSI(10, 5).Data;
            }
        }

        [TestMethod]
        public void Test_TSI_V2vsV1()
        {
            var algo = new Testbed_TSI_V2vsV1();
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
        #region RSI
        private class Testbed_RSI_V2vsV1 : Algorithm
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
                            Instruments.First().Close.RSI(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").Close.RSI(5).Data;
            }
        }

        [TestMethod]
        public void Test_RSI_V2vsV1()
        {
            var algo = new Testbed_RSI_V2vsV1();
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
        #region WilliamsPercentR
        private class Testbed_WilliamsPercentR_V2vsV1 : Algorithm
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
                            Instruments.First().WilliamsPercentR(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").WilliamsPercentR(5).Data;
            }
        }

        [TestMethod]
        public void Test_WilliamsPercentR_V2vsV1()
        {
            var algo = new Testbed_WilliamsPercentR_V2vsV1();
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
        #region StochasticOscillator
        private class Testbed_StochasticOscillator_V2vsV1 : Algorithm
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
                            Instruments.First().StochasticOscillator(5).PercentD[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").StochasticOscillator(5).PercentD.Data;
            }
        }

        [TestMethod]
        public void Test_StochasticOscillator_V2vsV1()
        {
            var algo = new Testbed_StochasticOscillator_V2vsV1();
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
        #region ADX
        private class Testbed_ADX_V2vsV1 : Algorithm
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
                            Instruments.First().ADX(5)[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").ADX(5).Data;
            }
        }

        [TestMethod]
        public void Test_ADX_V2vsV1()
        {
            var algo = new Testbed_ADX_V2vsV1();
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
