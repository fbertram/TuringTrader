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
using System.Text.RegularExpressions;
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
        private readonly BarSeriesAccessor _openSeries;
        private readonly BarSeriesAccessor _highSeries;
        private readonly BarSeriesAccessor _lowSeries;
        private readonly BarSeriesAccessor _closeSeries;
        private readonly BarSeriesAccessor _bidSeries;
        private readonly BarSeriesAccessor _askSeries;
        private readonly Algorithm _algorithm;
        private readonly DataSource _dataSource;

        public Instrument(Algorithm algorithm, DataSource source)
        {
            _algorithm = algorithm;
            _dataSource = source;

            _openSeries  = new BarSeriesAccessor(t => this[t].Open);
            _highSeries  = new BarSeriesAccessor(t => this[t].High);
            _lowSeries   = new BarSeriesAccessor(t => this[t].Low);
            _closeSeries = new BarSeriesAccessor(t => this[t].Close);

            _bidSeries = new BarSeriesAccessor(t => this[t].Bid);
            _askSeries = new BarSeriesAccessor(t => this[t].Ask);
        }

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
                return this[0].Time;
            }
        }

        public bool IsOption
        {
            get
            {
                return _dataSource.IsOption;
            }
        }
        public string OptionUnderlying
        {
            get
            {
                return _dataSource.OptionUnderlying;
            }
        }
        public DateTime OptionExpiry
        {
            get
            {
                return this[0].OptionExpiry;
            }
        }
        public bool OptionIsPut
        {
            get
            {
                return this[0].OptionIsPut;
            }
        }
        public double OptionStrike
        {
            get
            {
                return this[0].OptionStrike;
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

        public ITimeSeries<double> Bid
        {
            get
            {
                return _bidSeries;
            }
        }
        public ITimeSeries<double> Ask
        {
            get
            {
                return _askSeries;
            }
        }
    }
}
//==============================================================================
// end of file