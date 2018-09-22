//==============================================================================
// Project:     Trading Simulator
// Name:        ITimeSeries
// Description: time series interface
// History:     2018ix10, FUB, created
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
    /// <summary>
    /// time series interface
    /// </summary>
    public interface ITimeSeries<T>
    {
        T this[int daysBack]
        {
            get;
        }
    }
}

//==============================================================================
// end of file