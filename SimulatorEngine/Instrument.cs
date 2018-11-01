//==============================================================================
// Project:     Trading Simulator
// Name:        Instrument
// Description: instrument, a time series of bars
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    /// <summary>
    /// class representing a tradeable instrument, holding time series data
    /// </summary>
    public class Instrument : TimeSeries<Bar>
    {
        #region internal data
        private class BarSeriesAccessor<T> : ITimeSeries<T>
        {
            private Func<int, T> _accessor;

            public BarSeriesAccessor(Func<int, T> accessor)
            {
                _accessor = accessor;
            }

            public T this[int daysBack]
            {
                get
                {
                    return _accessor(daysBack);
                }
            }
        }
        private readonly BarSeriesAccessor<DateTime> _timeSeries;
        private readonly BarSeriesAccessor<double> _openSeries;
        private readonly BarSeriesAccessor<double> _highSeries;
        private readonly BarSeriesAccessor<double> _lowSeries;
        private readonly BarSeriesAccessor<double> _closeSeries;
        private readonly BarSeriesAccessor<long> _volumeSeries;
        private readonly BarSeriesAccessor<double> _bidSeries;
        private readonly BarSeriesAccessor<double> _askSeries;
        private readonly BarSeriesAccessor<long> _bidVolume;
        private readonly BarSeriesAccessor<long> _askVolume;
        private readonly BarSeriesAccessor<bool> _bidAskValid;
        #endregion

        #region public Instrument(...)
        public Instrument(SimulatorCore simulator, DataSource source)
        {
            Simulator = simulator;
            DataSource = source;

            _timeSeries = new BarSeriesAccessor<DateTime>(t => this[t].Time);
            _openSeries = new BarSeriesAccessor<double>(t => this[t].Open);
            _highSeries = new BarSeriesAccessor<double>(t => this[t].High);
            _lowSeries = new BarSeriesAccessor<double>(t => this[t].Low);
            _closeSeries = new BarSeriesAccessor<double>(t => this[t].Close);
            _volumeSeries = new BarSeriesAccessor<long>(t => this[t].Volume);
            _bidSeries = new BarSeriesAccessor<double>(t => this[t].Bid);
            _askSeries = new BarSeriesAccessor<double>(t => this[t].Ask);
            _bidVolume = new BarSeriesAccessor<long>(t => this[t].BidVolume);
            _askVolume = new BarSeriesAccessor<long>(t => this[t].AskVolume);
            _bidAskValid = new BarSeriesAccessor<bool>(t => this[t].IsBidAskValid);
        }
        #endregion

        //----- general info
        public readonly SimulatorCore Simulator;
        public readonly DataSource DataSource;
        #region public string Nickname
        public string Nickname
        {
            get
            {
                return DataSource.Info[DataSourceValue.nickName];
            }
        }
        #endregion
        #region public string Name
        public string Name
        {
            get
            {
                return DataSource.Info[DataSourceValue.name];
            }
        }
        #endregion
        #region public string Symbol
        public string Symbol
        {
            get
            {
                return this[0].Symbol;
            }
        }
        #endregion

        //----- option-specific info
        #region public bool IsOption
        public bool IsOption
        {
            get
            {
                return DataSource.IsOption;
            }
        }
        #endregion
        #region public string OptionUnderlying
        public string OptionUnderlying
        {
            get
            {
                return DataSource.OptionUnderlying;
            }
        }
        #endregion
        #region public DateTime OptionExpiry
        public DateTime OptionExpiry
        {
            get
            {
                return this[0].OptionExpiry;
            }
        }
        #endregion
        #region public bool OptionIsPut
        public bool OptionIsPut
        {
            get
            {
                return this[0].OptionIsPut;
            }
        }
        #endregion
        #region public double OptionStrike
        public double OptionStrike
        {
            get
            {
                return this[0].OptionStrike;
            }
        }
        #endregion
        #region public bool HasOHLC
        public bool HasOHLC
        {
            get
            {
                return this[0].HasOHLC;
            }
        }
        #endregion
        #region public bool HasBidAsk
        public bool HasBidAsk
        {
            get
            {
                return this[0].HasBidAsk;
            }
        }
        #endregion

        //----- time series
        #region public ITimeSeries<DateTime> Time
        public ITimeSeries<DateTime> Time
        {
            get
            {
                return _timeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Open
        public ITimeSeries<double> Open
        {
            get
            {
                return _openSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> High
        public ITimeSeries<double> High
        {
            get
            {
                return _highSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Low
        public ITimeSeries<double> Low
        {
            get
            {
                return _lowSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Close
        public ITimeSeries<double> Close
        {
            get
            {
                return _closeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<long> Volume
        public ITimeSeries<long> Volume
        {
            get
            {
                return _volumeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Bid
        public ITimeSeries<double> Bid
        {
            get
            {
                return _bidSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Ask
        public ITimeSeries<double> Ask
        {
            get
            {
                return _askSeries;
            }
        }
        #endregion
        #region public ITimeSeries<long> BidVolume
        public ITimeSeries<long> BidVolume
        {
            get
            {
                return _bidVolume;
            }
        }
        #endregion
        #region public ITimeSeries<long> AskVolume
        public ITimeSeries<long> AskVolume
        {
            get
            {
                return _askVolume;
            }
        }
        #endregion
        #region public ITimeSeries<bool> IsBidAskValid
        public ITimeSeries<bool> IsBidAskValid
        {
            get
            {
                return _bidAskValid;
            }
        }
        #endregion

        //----- trading
        #region public void Trade(int quantity, OrderExecution tradeExecution, double price)
        public void Trade(int quantity, OrderType tradeExecution = OrderType.openNextBar, double price = 0.00)
        {
            if (quantity != 0)
            {
                Simulator.PendingOrders.Add(
                    new Order()
                    {
                        Instrument = this,
                        Quantity = quantity,
                        Type = tradeExecution,
                        Price = price,
                    });
            }
        }
        #endregion
        #region public int Position
        public int Position
        {
            get
            {
                // TODO: does this crash, when there is no position?
                return Simulator.Positions
                        .Where(p => p.Key == this)
                        .Sum(x => x.Value);
            }
        }
        #endregion
    }
}
//==============================================================================
// end of file