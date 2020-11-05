//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        ITimeSeries
// Description: time series interface
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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