//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        TimeSeries
// Description: time series template class
// History:     2018ix10, FUB, created
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
using System;
using System.Collections.Generic;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Template class for time series data. A TimeSeries object will allow
    /// limited access to the historical values of the series, by default
    /// 256 values back. TuringTrader makes extensive use of time series
    /// objects for the implementation of data sources and indicators.
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    public class TimeSeries<T> : ITimeSeries<T>
    {
        #region internal data
        private T[] _cyclicData;
        private int _cyclicNewest;
        private List<T> _listData;
        #endregion

        #region public TimeSeries(int maxBarsBack)
        /// <summary>
        /// Create and initialize time series. Typically, a time series
        /// is a cyclic buffer, allowing access to a limited range of
        /// data. However, if a non-positive value is passed for maxBarsBack,
        /// a list is created, allowing access to all previous values.
        /// </summary>
        /// <param name="maxBarsBack">number of bars to hold</param>
        public TimeSeries(int maxBarsBack = 256)
        {
            MaxBarsBack = maxBarsBack;

            if (MaxBarsBack > 0)
                _cyclicData = new T[MaxBarsBack];
            else
                _listData = new List<T>();

            Clear();
        }
        #endregion
        #region public void Clear()
        /// <summary>
        /// Clear contents of time series.
        /// </summary>
        public void Clear()
        {
            if (_cyclicData != null)
                _cyclicNewest = -1;
            else
                _listData.Clear();

            BarsAvailable = 0;
        }
        #endregion

        #region public T Value
        /// <summary>
        /// Write-only property to set current value, and shift the time series.
        /// </summary>
        public T Value
        {
            set
            {
                if (_cyclicData != null)
                {
                    _cyclicNewest = (_cyclicNewest + 1) % MaxBarsBack;
                    BarsAvailable = Math.Min(BarsAvailable + 1, MaxBarsBack);
                    _cyclicData[_cyclicNewest] = value;
                }
                else
                {
                    _listData.Add(value);
                    BarsAvailable++;
                }
            }
        }
        #endregion
        #region public T this[int barsBack]
        /// <summary>
        /// Read only access to historical bars
        /// </summary>
        /// <param name="barsBack">number of bars back, 0 for most recent</param>
        /// <returns>historical value</returns>
        public T this[int barsBack]
        {
            get
            {
                if (BarsAvailable < 1)
                    throw new Exception("time series lookup past available bars");

                if (_cyclicData != null)
                {
                    // adjust daysBack, if exceeding # of available bars
                    // NOTE: we will *not* throw an exception when referencing bars
                    //       exceeding BarsAvailable
                    barsBack = Math.Max(Math.Min(barsBack, BarsAvailable - 1), 0);

                    int idx = (_cyclicNewest + MaxBarsBack - barsBack) % MaxBarsBack;
                    T value = _cyclicData[idx];

                    return value;
                }
                else
                {
                    return _listData[_listData.Count - 1 - barsBack];
                }
            }
        }
        #endregion
        #region public int BarsAvailable
        /// <summary>
        /// Number of valid bars available.
        /// </summary>
        public int BarsAvailable
        {
            get;
            private set;
        }
        #endregion
        #region public readonly int MaxBarsBack
        /// <summary>
        /// Maximum number of bars available.
        /// </summary>
        public readonly int MaxBarsBack;
        #endregion
    }
}

//==============================================================================
// end of file