//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T303_Arithmetic
// Description: Unit test for indicator arithmetic.
// History:     2022xii01, FUB, created
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
    public class T303_Arithmetic
    {
        private class Testbed_Add : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Add(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Add()
        {
            var algo = new Testbed_Add();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.add"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.5) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 2.0) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 34.0) < 1e-5);
        }

        private class Testbed_Sub : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Sub(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Sub()
        {
            var algo = new Testbed_Sub();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.sub"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min + 0.5) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 0.5) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 4.0) < 1e-5);
        }

        private class Testbed_Mul : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Mul(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Mul()
        {
            var algo = new Testbed_Mul();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.mul"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 1.0) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 14.5) < 1e-5);
        }

        private class Testbed_Div : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Div(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Div()
        {
            var algo = new Testbed_Div();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.div"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 2.0) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 28.0) < 1e-5);
        }

        private class Testbed_Min : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Min(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Min()
        {
            var algo = new Testbed_Min();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.min"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.0) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 1.0) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 14.5) < 1e-5);
        }

        private class Testbed_Max : Algorithm
        {
            public TimeSeriesFloat TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);
                CooldownPeriod = TimeSpan.FromDays(0);

                var one = new T000_Helpers.StepResponse();
                one.StartDate = StartDate;
                one.EndDate = EndDate;
                one.Amplitude = 1.0;

                var two = new T000_Helpers.NyquistFrequency();
                two.StartDate = StartDate;
                two.EndDate = EndDate;
                two.Amplitude = 0.5;

                TestResult = Asset(one).Close.Max(Asset(two).Close.Add(0.5));
            }
        }

        [TestMethod]
        public void Test_Max()
        {
            var algo = new Testbed_Max();
            algo.Run();
            var result = algo.TestResult;

            var description = result.Name;
            Assert.IsTrue(description.ToLower().Contains("close.max"));

            var firstDate = result.Data.Result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2022-01-03T16:00-5:00"));

            var lastDate = result.Data.Result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2022-01-31T16:00-5:00"));

            var barCount = result.Data.Result.Count();
            Assert.IsTrue(barCount == 20);

            var min = result.Data.Result.Min(b => b.Value);
            var max = result.Data.Result.Max(b => b.Value);
            var sum = result.Data.Result.Sum(b => b.Value);
            Assert.IsTrue(Math.Abs(min - 0.5) < 1e-5);
            Assert.IsTrue(Math.Abs(max - 1.0) < 1e-5);
            Assert.IsTrue(Math.Abs(sum - 19.5) < 1e-5);
        }
    }
}

//==============================================================================
// end of file