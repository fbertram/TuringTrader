//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        DataSourceNorgate
// Description: unit test for Norgate data source
// History:     2019x08, FUB, created
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
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class DataSourceNorgate
    {
        [TestMethod]
        public void Test_DataRetrieval()
        {
            var ds = DataSource.New("norgate:msft");

            var d = ds.LoadData(DateTime.Parse("01/01/2019"), DateTime.Parse("01/12/2019"));

            Assert.IsTrue(ds.Info[TuringTrader.Simulator.DataSourceParam.name].ToLower().Contains("microsoft"));
            Assert.IsTrue(d.First().Time.Date == DateTime.Parse("01/02/2019"));
            Assert.IsTrue(d.Last().Time.Date == DateTime.Parse("01/11/2019"));
            Assert.IsTrue(d.Count() == 8);
            Assert.IsTrue(Math.Abs(d.Last().Close / d.First().Open - 102.36056 / 99.12445) < 1e-3);
        }

        [TestMethod]
        public void Test_Universe()
        {
            var universe = Universe.New("$SPX");
            var constituents = universe.Constituents;
            var isConstituent1 = universe.IsConstituent("META", DateTime.Parse("01/01/2013"));
            var isConstituent2 = universe.IsConstituent("META", DateTime.Parse("01/01/2014"));

            Assert.IsTrue(constituents.Count() > 1100);
            Assert.IsTrue(isConstituent1 == false);
            Assert.IsTrue(isConstituent2 == true);
        }
    }
}

//==============================================================================
// end of file