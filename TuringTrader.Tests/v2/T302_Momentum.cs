//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T302_Momentum
// Description: Unit test for basic indicators.
// History:     2023ii11, FUB, created
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
    }
}

//==============================================================================
// end of file
