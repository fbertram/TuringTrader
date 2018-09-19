//==============================================================================
// Project:     Trading Simulator
// Name:        TimeSeries
// Description: time series template class
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
    /// time series implementation
    /// </summary>
    public class TimeSeries<T> : ITimeSeries<T>
    {
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
    }
}

//==============================================================================
// end of file