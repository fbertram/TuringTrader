//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdater
// Description: Data updater base class
// History:     2018ix27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    abstract public class DataUpdater
    {
        //----- object factory
        #region static public DataUpdate New(Dictionary<DataSourceValue, string> info)
        static public DataUpdater New(Dictionary<DataSourceValue, string> info)
        {
            if (info.ContainsKey(DataSourceValue.updateWeb))
                return new DataUpdaterWeb(info);
            else
                return null;
        }
        #endregion
        #region public DataUpdate(Dictionary<DataSourceValue, string> info)
        public DataUpdater(Dictionary<DataSourceValue, string> info)
        {
            Info = info;
        }
        #endregion

        public readonly Dictionary<DataSourceValue, string> Info;

        abstract public void UpdateData(DateTime startTime, DateTime endTime);
    }
}

//==============================================================================
// end of file