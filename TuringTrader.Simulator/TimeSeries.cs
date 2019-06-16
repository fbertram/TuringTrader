//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        TimeSeries
// Description: time series template class
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
    /// Template class for time series data. A TimeSeries object will allow
    /// limited access to the historical values of the series, by default
    /// 256 values back. TuringTrader makes extensive use of time series
    /// objects for the implementation of data sources and indicators.
    /// </summary>
    /// <typeparam name="T">value type</typeparam>
    public class TimeSeries<T> : ITimeSeries<T>
    {
#if false
        // safe: implementation with C# List<T>
        #region internal data
        private int _maxBarsBack;
        private List<T> _data = new List<T>();
        #endregion

        #region public TimeSeries(int maxBarsBack)
        public TimeSeries(int maxBarsBack = 256)
        {
            _maxBarsBack = maxBarsBack;

        }
        #endregion

        #region public T Value
        public T Value
        {
            set
            {
                _data.Insert(0, value);

                if (_data.Count > _maxBarsBack)
                    _data.RemoveRange(_maxBarsBack, _data.Count - _maxBarsBack);
            }
        }
        #endregion
        #region public T this[int daysBack]
        public T this[int daysBack]
        {
            get
            {
#if false
                // throw, if exceeding # of available bars
                if (daysBack < 0 || daysBack >= BarsAvailable)
                    throw new Exception(string.Format("{0} exceed max bars back of {1}", daysBack, _maxBarsBack));
#else
                // adjust daysBack, if exceeding # of available bars
                daysBack = Math.Max(Math.Min(daysBack, BarsAvailable - 1), 0);
#endif

                return _data[daysBack];
            }
        }
        #endregion
        #region public int BarsAvailable
        public int BarsAvailable
        {
            get
            {
                return _data.Count();
            }
        }
        #endregion
#else
        // faster: implementation with diy cyclic buffer
        #region internal data
        private T[] _barData;
        private int _newestBar;
        #endregion

        #region public TimeSeries(int maxBarsBack)
        /// <summary>
        /// Create and initialize time series.
        /// </summary>
        /// <param name="maxBarsBack">number of bars to hold</param>
        public TimeSeries(int maxBarsBack = 256)
        {
            MaxBarsBack = maxBarsBack;
            _barData = new T[MaxBarsBack];

            Clear();
        }
        #endregion
        #region public void Clear()
        /// <summary>
        /// Clear contents of time series.
        /// </summary>
        public void Clear()
        {
            _newestBar = -1;
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
                _newestBar = (_newestBar + 1) % MaxBarsBack;
                BarsAvailable = Math.Min(BarsAvailable + 1, MaxBarsBack);
                _barData[_newestBar] = value;
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

                // adjust daysBack, if exceeding # of available bars
                // NOTE: we will *not* throw an exception when referencing bars
                //       exceeding BarsAvailable
                barsBack = Math.Max(Math.Min(barsBack, BarsAvailable - 1), 0);

                int idx = (_newestBar + MaxBarsBack - barsBack) % MaxBarsBack;
                T value = _barData[idx];

                return value;
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
#endif
    }
}

//==============================================================================
// end of file