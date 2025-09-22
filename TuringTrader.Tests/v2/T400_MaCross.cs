//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T400_MaCross
// Description: Unit test for simple MA-cross strategy.
// History:     2022xi30, EFB, created
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
using System.Linq;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T400_MaCross
    {
        private class Testbed : Algorithm
        {
            //[OptimizerParam(1, 1, 1)]
            //public int PARAM { get; set; } = 1;

            public override void Run()
            {
                StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2021-12-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(365);

                SimLoop(() =>
                {
                    var asset = Asset("$SPX");
                    var weight = asset.Close.EMA(50)[0] > asset.Close.EMA(200)[0] ? 1.0 : 0.0;

                    if (Math.Abs(asset.Position - weight) > 0.05)
                        asset.Allocate(weight, OrderType.openNextBar);

                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                });
            }
        }

        [TestMethod]
        public void Test_Strategy()
        {
#if false
            // NOTE: this code is to reproduce a bug where the fitness value
            //       returned by the optimizer is calculated incorrectly,
            //       when strategies where nested.
            var algo = new TTcom_VixSpritz_v3();
            algo.Run();
            var fitnessReturn = algo.FitnessReturn;
            var fitnessRisk = algo.FitnessRisk;
            var fitnessValue = algo.FitnessValue;
#else
            var algo = new Testbed();
            algo.Run();

            //----- check algo result
            var result = algo.EquityCurve;

            var barCount = result.Count();
            Assert.IsTrue(barCount == 3777);

            var firstDate = result.First().Date;
            Assert.IsTrue(firstDate == DateTime.Parse("2007-01-03T16:00-5:00"));

            var lastDate = result.Last().Date;
            Assert.IsTrue(lastDate == DateTime.Parse("2021-12-31T16:00-5:00"));

            var firstValue = result.First().Value.Close;
            Assert.AreEqual(1000.0, firstValue, 1e-5);

            var lastValue = result.Last().Value.Close;
            Assert.AreEqual(2498.4266608406929, lastValue, 1e-5);

            //----- check algo account
            var account = algo.Account;

            var cagr = ((Account_Default)account).AnnualizedReturn;
            //Assert.IsTrue(Math.Abs(cagr - 0.062978353632788586) < 1e-5);
            Assert.AreEqual(Math.Pow(lastValue / firstValue, 365.25 / (lastDate - firstDate).TotalDays) - 1.0, cagr, 1e-5);

            var mdd = ((Account_Default)account).MaxDrawdown;
            Assert.IsTrue(Math.Abs(mdd - 0.29532656508770572) < 1e-5);

            var trades = account.TradeLog.Count;
            Assert.IsTrue(trades == 19);

            //----- check plotter
            var equityCurve = algo.Plotter.AllData["Testbed"];
            Assert.AreEqual(3777, equityCurve.Count);

            var plotterFirstDate = equityCurve.First()["Date"];
            Assert.AreEqual(DateTime.Parse("2007-01-03T16:00-5:00"), plotterFirstDate);

            var plotterLastDate = equityCurve.Last()["Date"];
            Assert.AreEqual(DateTime.Parse("2021-12-31T16:00-5:00"), plotterLastDate);

            var plotterFirstValue = (double)equityCurve.First()["Testbed"];
            Assert.AreEqual(1000.0, plotterFirstValue, 1e-5);

            var plotterLastValue = (double)equityCurve.Last()["Testbed"];
            Assert.AreEqual(2498.4266608406929, plotterLastValue, 1e-5);

            //----- check fitness values
            var fitnessReturn = algo.FitnessReturn;
            Assert.AreEqual(lastValue, lastValue, 1e-5);

            var fitnessRisk = algo.FitnessRisk;
            Assert.AreEqual(mdd, mdd, 1e-5);

            var fitnessValue = algo.FitnessValue;
            Assert.AreEqual(0.66408920742165534, fitnessValue, 1e-5);
#endif

            //----- check optimizer
            var optimizerParams = algo.OptimizerParams;
            //optimizerParams["PARAM"].IsEnabled = true;

            var optimizer = new OptimizerGrid(algo);
            optimizer.Run();
            var optResults = optimizer.Results;

            var numResults = optResults.Count;
            Assert.AreEqual(1, numResults);

            var optNav = (double)optResults[0].NetAssetValue;
            Assert.AreEqual(optNav, fitnessReturn, 1e-5);

            var optMdd = (double)optResults[0].MaxDrawdown;
            Assert.AreEqual(optMdd, fitnessRisk, 1e-5);

            var optFitness = (double)optResults[0].Fitness;
            Assert.AreEqual(optFitness, fitnessValue, 1e-5);
        }
    }
}

//==============================================================================
// end of file