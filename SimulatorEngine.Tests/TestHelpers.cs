//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        TestHelpers
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    class TestHelpers
    {
    }

    #region class DataSourceFromBars
    class DataSourceFromBars : DataSource
    {
        public DataSourceFromBars(List<Bar> bars, Dictionary<DataSourceValue, string> infos) : base(infos)
        {
            Data = bars;
        }

        public override void LoadData(DateTime startTime, DateTime endTime)
        {
            // nothing to do here
        }
    }
    #endregion
}

//==============================================================================
// end of file