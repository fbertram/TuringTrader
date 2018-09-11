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
    public class Instrument : TimeSeries<Bar>
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
        private Algorithm _algorithm;

        public Instrument(Algorithm algorithm)
        {
            _algorithm = algorithm;

            _openSeries  = new BarSeriesAccessor(t => this[t].Values[DataSourceValue.open]);
            _highSeries  = new BarSeriesAccessor(t => this[t].Values[DataSourceValue.high]);
            _lowSeries   = new BarSeriesAccessor(t => this[t].Values[DataSourceValue.low]);
            _closeSeries = new BarSeriesAccessor(t => this[t].Values[DataSourceValue.close]);
        }

        public readonly DataSource DataSource;

        public string Symbol
        {
            get
            {
                return this[0].Symbol;
            }
        }
        public DateTime LastTime
        {
            get
            {
                return this[0].TimeStamp;
            }
        }

        public void Trade(int quantity, OrderExecution tradeExecution = OrderExecution.openNextBar)
        {
            _algorithm.PendingOrders.Add(
                new Order() {
                    Instrument = this,
                    Quantity = quantity,
                    Execution = tradeExecution,
                    PriceSpec = OrderPriceSpec.market,
                });
        }
        public int Position
        {
            get
            {
                return _algorithm.Positions
                        .Where(p => p.Key == this)
                        .Sum(x => x.Value);
            }
        }

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