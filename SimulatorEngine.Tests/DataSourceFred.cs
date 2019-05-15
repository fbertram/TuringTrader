//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        DataSourceFred
// Description: unit test for FRED data source
// History:     2019v15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class DataSourceFred
    {
        [TestMethod]
        public void Test_DataRetrieval()
        {
            var ds = DataSourceFromBars.New("fred:GDPC1");

            ds.LoadData(DateTime.Parse("09/30/2018"), DateTime.Parse("01/03/2019"));

            Assert.IsTrue(ds.Info[TuringTrader.Simulator.DataSourceValue.name].ToLower().Contains("real gross domestic product"));
            Assert.IsTrue(((DateTime)ds.FirstTime).Date == DateTime.Parse("01/01/1947"));
            //Assert.IsTrue(((DateTime)ds.LastTime).Date == DateTime.Parse("01/11/2019"));
            Assert.IsTrue(ds.Data.Count() == 64);
            Assert.IsTrue(Math.Abs(ds.Data.First().Open - 18765.256) < 1e-2);
            Assert.IsTrue(Math.Abs(ds.Data.Last().Close - 18912.326) < 1e-2);
        }
    }
}

//==============================================================================
// end of file