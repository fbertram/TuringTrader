//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T303_Resampling
// Description: Unit test for Resampling indicator.
// History:     2022xi30, FUB, created
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
    public class T303_Resampling
    {
        #region weekly
        private class Testbed_Weekly : Algorithm
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

                TestResult = Asset(Generator).Close.Weekly().EMA(4);
            }
        }

        [TestMethod]
        public void Test_Weekly_StepResponse()
        {
            var algo = new Testbed_Weekly();
            algo.Generator = new T000_Helpers.StepResponse();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.weekly(0).ema(4)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-07T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 5);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 1) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 1) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 5) < 1e-5);
        }

        [TestMethod]
        public void Test_Weekly_NyquistResponse()
        {
            var algo = new Testbed_Weekly();
            algo.Generator = new T000_Helpers.NyquistFrequency();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().EndsWith("close.weekly(0).ema(4)"));

            var firstDate = result.Data.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-07T16:00-5:00"));

            var lastDate = result.Data.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Count();
            Assert.IsTrue(barCount == 5);

            var min = result.Data.Min(b => b.Value);
            var max = result.Data.Max(b => b.Value);
            var sum = result.Data.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.64) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 2.0544000000000002) < 1e-5);
        }
        #endregion
    }
}

//==============================================================================
// end of file