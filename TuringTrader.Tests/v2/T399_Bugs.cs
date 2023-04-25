//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T399_Bugs
// Description: Unit test for bugs and fixes.
// History:     2023iv18, FUB, created
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
using System.Linq;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T399_Bugs
    {
        #region ZScore
        [TestMethod]
        public void Test_ZScore_00()
        {
            // reproduce bug in 16.0.57 w/ ZLEMA returning NaN
            // this bug turned out to be caused by Variance
            // returning a negative value and Sqrt for
            // StandardDeviation failing
            // fixed this issue by introducing a Max(0.0, ...)
            // in Variance calculation

            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("1993-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("1993-12-31T16:00-05:00");
            algo.WarmupPeriod = TimeSpan.FromDays(3 * 252);
            algo.CooldownPeriod = TimeSpan.FromDays(0);
            var vix = algo.Asset("$VIX");
            var zlema = vix.Close.ZScore(3 * 252);

            var first = zlema.Data.First();
            Assert.AreEqual(DateTime.Parse("1990-12-07T16:00-05:00"), first.Date);
            Assert.AreEqual(0.0, first.Value, 1e-5);

            var last = zlema.Data.Last();
            Assert.AreEqual(DateTime.Parse("1993-12-31T16:00-05:00"), last.Date);
            Assert.AreEqual(-1.1270145110378169, last.Value, 1e-5);
        }
        #endregion
        #region ValueAtRisk
        [TestMethod]
        public void Test_ValueAtRisk_00()
        {
            // reproduce bug w/ 16.0.59: ValueAtRisk throws exception,
            // seemingly because the asset is not loaded prior to 
            // calculating ValueAtRisk

            var algo = new T000_Helpers.DoNothing();
            algo.StartDate = DateTime.Parse("1970-01-01T16:00-05:00");
            algo.EndDate = DateTime.Parse("2023-03-01");
            ((Account_Default)algo.Account).Friction = 0.0;

            var var = algo.Asset(MarketIndex.SPXTR).ValueAtRisk(21, 0.99);
            var varAvg = var.Data.Average(b => b.Value);
            var varStd = Math.Sqrt(var.Data.Average(b => Math.Pow(b.Value - varAvg, 2.0)));

            Assert.AreEqual(13413, var.Data.Count);
            Assert.AreEqual(0.01163752927334598, varAvg, 1e-5);
            Assert.AreEqual(0.00852457502803818, varStd, 1e-5);
        }
        #endregion
    }
}

//==============================================================================
// end of file