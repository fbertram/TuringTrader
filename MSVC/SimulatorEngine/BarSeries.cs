//==============================================================================
// Project:     Trading Simulator
// Name:        BarCollection
// Description: collection of bars
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public class BarSeries : TimeSeries<Bar>
    {
        private class BarSeriesAccessor : ITimeSeries<double>
        {
            private Func<int, double> _accessor;

            public BarSeriesAccessor(Func<int, double> accessor)
            {
                _accessor = accessor;
            }

            public double this[int daysBack]
            {
                get
                {
                    return _accessor(daysBack);
                }
            }
        }

        private BarSeriesAccessor _openSeries;
        private BarSeriesAccessor _highSeries;
        private BarSeriesAccessor _lowSeries;
        private BarSeriesAccessor _closeSeries;

        public BarSeries()
        {
            _openSeries  = new BarSeriesAccessor(t => this[t].Open);
            _highSeries  = new BarSeriesAccessor(t => this[t].High);
            _lowSeries   = new BarSeriesAccessor(t => this[t].Low);
            _closeSeries = new BarSeriesAccessor(t => this[t].Close);
        }

        public readonly InstrumentDataBase DataSource;

        public ITimeSeries<double> Open
        {
            get
            {
                return _openSeries;
            }
        }

        public ITimeSeries<double> High
        {
            get
            {
                return _highSeries;
            }
        }

        public ITimeSeries<double> Low
        {
            get
            {
                return _lowSeries;
            }
        }

        public ITimeSeries<double> Close
        {
            get
            {
                return _closeSeries;
            }
        }

    }
}
//==============================================================================
// end of file