﻿//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        TestHelpers
// Description: unit test for stooq.com data updater
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
        private List<Bar> _bars = null;

        public DataSourceFromBars(List<Bar> bars, Dictionary<DataSourceParam, string> infos) : base(infos)
        {
            _bars = bars;
        }

        public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
        {
            return _bars;
        }
    }
    #endregion
}

//==============================================================================
// end of file