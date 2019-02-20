//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterStooq
// Description: unit test for stooq.com data updater
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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
        private List<Bar> _bars;
        private IEnumerator<Bar> _enum = null;

        public DataSourceFromBars(List<Bar> bars, Dictionary<DataSourceValue, string> infos) : base(infos)
        {
            _bars = bars;
        }

        public override IEnumerator<Bar> BarEnumerator
        {
            get
            {
                if (_enum == null)
                    _enum = _bars.GetEnumerator();
                return _enum;
            }
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