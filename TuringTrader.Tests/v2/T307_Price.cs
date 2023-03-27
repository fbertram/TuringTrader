//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T307_Price
// Description: Unit test for price indicators.
// History:     2023iii27, FUB, created
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
    public class T307_Price
    {
        #region TypicalPrice
        private class Testbed_TypicalPrice_V2vsV1 : Algorithm
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
                            Instruments.First().TypicalPrice()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").TypicalPrice().Data;
            }
        }

        [TestMethod]
        public void Test_TypicalPrice_V2vsV1()
        {
            var algo = new Testbed_TypicalPrice_V2vsV1();
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
        #region CLV
        private class Testbed_CLV_V2vsV1 : Algorithm
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
                            Instruments.First().CLV()[0]);
                }
            }
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-03-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                v1Result = Asset(new Testbed_v1()).Close.Data;
                v2Result = Asset("$SPX").CLV().Data;
            }
        }

        [TestMethod]
        public void Test_CLV_V2vsV1()
        {
            var algo = new Testbed_CLV_V2vsV1();
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