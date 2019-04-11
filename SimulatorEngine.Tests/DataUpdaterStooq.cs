//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        DataUpdaterStooq
// Description: unit test for stooq.com data updater
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#if false

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class DataUpdaterStooq
    {
        #region public void Test_UpdateData()
        [TestMethod]
        public void Test_UpdateData()
        {
            Dictionary<DataSourceValue, string> info = new Dictionary<DataSourceValue, string>()
            {
                { DataSourceValue.ticker, "^SPX" },
                { DataSourceValue.symbolStooq, "^spx" },
                { DataSourceValue.dataUpdater, "stooq" }
            };

            DataUpdater updater = DataUpdater.New(
                null,
                info);

            List<Bar> bars = updater.UpdateData(
                    DateTime.Parse("01/16/1968, 9:30am"),
                    DateTime.Parse("01/16/1968, 4pm"))
                .ToList();

            Assert.IsTrue(bars.Count == 1);

            Bar bar = bars[0];

            Assert.IsTrue(bar.Time == DateTime.Parse("01/16/1968, 4pm"));
            Assert.IsTrue(Math.Abs(bar.Open - 96.42) < 1e-5);
            Assert.IsTrue(Math.Abs(bar.High - 96.91) < 1e-5);
            Assert.IsTrue(Math.Abs(bar.Low - 95.32) < 1e-5);
            Assert.IsTrue(Math.Abs(bar.Close - 95.82) < 1e-5);
            Assert.IsTrue(Math.Abs(bar.Volume - 13711111) < 1e-5);
        }
        #endregion
    }
}

#endif

//==============================================================================
// end of file