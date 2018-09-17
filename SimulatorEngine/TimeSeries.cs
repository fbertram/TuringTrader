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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public class TimeSeries<T> : ITimeSeries<T>
    {
        private int _maxBarsBack;
        private List<T> _data = new List<T>();

        public TimeSeries(int maxBarsBack = 256)
        {
            _maxBarsBack = maxBarsBack;

        }

        public T Value
        {
            set
            {
                _data.Insert(0, value);

                if (_data.Count > _maxBarsBack)
                    _data.RemoveRange(_maxBarsBack, _data.Count - _maxBarsBack);
            }
        }

        public T this[int daysBack]
        {
            get
            {
                if (daysBack < 0 || daysBack >= _data.Count)
                    throw new Exception(string.Format("{0} exceed max bars back of {1}", daysBack, _maxBarsBack));

                return _data[daysBack];
            }
        }
    }
}

//==============================================================================
// end of file