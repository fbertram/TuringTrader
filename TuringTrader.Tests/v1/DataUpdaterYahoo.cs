//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        DataUpdaterStooq
// Description: unit test for finance.yahoo.com data updater
// History:     2018xi27, FUB, created
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

#if false

// doesn't work as of 2023iv17. Most likely, the download URL has changed

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
            Dictionary<DataSourceParam, string> info = new Dictionary<DataSourceParam, string>()
            {
                { DataSourceParam.ticker, "^SPX" },
                { DataSourceParam.symbolYahoo, "^GSPC" },
                { DataSourceParam.dataUpdater, "yahoo" }
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