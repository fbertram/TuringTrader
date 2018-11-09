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

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Interface for time series data. This interface provides access to
    /// a limited number of historical values.
    /// </summary>
    /// <typeparam name="T">type of time series</typeparam>
    public interface ITimeSeries<T>
    {
        #region T this[int barsBack]
        /// <summary>
        /// Retrieve historical value from time series.
        /// </summary>
        /// <param name="barsBack">number of bars back, 0 for current bar</param>
        /// <returns>historical value</returns>
        T this[int barsBack]
        {
            get;
        }
        #endregion
    }
}

//==============================================================================
// end of file