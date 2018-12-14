//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterStooq
// Description: unit test for finance.yahoo.com data updater
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#if true

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class DataUpdaterYahoo
    {
        #region public void Test_UpdateData()
        [TestMethod]
        public void Test_UpdateData()
        {
            Dictionary<DataSourceValue, string> info = new Dictionary<DataSourceValue, string>()
            {
                { DataSourceValue.ticker, "^SPX" },
                { DataSourceValue.symbolYahoo, "^GSPC" },
                { DataSourceValue.dataUpdater, "yahoo" }
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
            Assert.IsTrue(Math.Abs(bar.Volume - 12340000) < 1e-5);
        }
        #endregion
    }
}

#endif

//==============================================================================
// end of file