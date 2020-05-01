//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Instrument
// Description: instrument, a time series of bars
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class representing a tradeable instrument. In essence, an instrument
    /// is a time series of bars, enriched by general information about
    /// the data source which feeds this instrument. Instruments are created
    /// automatically by the simulator engine as required, in many cases
    /// leading to instruments being added over the course of a simulation.
    /// As indicators typically run on time series of double, instruments
    /// break down the time series of bars into multiple series of doubles.
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
        /// <summary>
        /// Create and initialize instrument. Algorithms should not create
        /// instrument objects directly. Instead, the simulator engine will
        /// create these objects as required, and while processing bars from
        /// DataSource objects.
        /// </summary>
        /// <param name="simulator">parent simulator object</param>
        /// <param name="source">associated data source</param>
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
        #region public readonly SimulatorCore Simulator
        /// <summary>
        /// Parent Simulator object.
        /// </summary>
        public readonly SimulatorCore Simulator;
        #endregion
        #region public readonly DataSource DataSource
        /// <summary>
        /// Associated DataSource object.
        /// </summary>
        public readonly DataSource DataSource;
        #endregion
        #region public string Nickname
        /// <summary>
        /// DataSource's nickname.
        /// </summary>
        public string Nickname
        {
            get
            {
                return DataSource.Info[DataSourceParam.nickName];
            }
        }
        #endregion
        #region public string Name
        /// <summary>
        /// Instrument's full name, e.g. Microsoft Corporation.
        /// </summary>
        public string Name
        {
            get
            {
                return DataSource.Info[DataSourceParam.name];
            }
        }
        #endregion
        #region public string Symbol
        /// <summary>
        /// Instrument's fully qualified symbol. For stocks, this is identical
        /// to the ticker. For options, this will include the expiry date,
        /// direction, and strike price.
        /// </summary>
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
        /// <summary>
        /// Flag indicating if this is an option contract.
        /// </summary>
        public bool IsOption
        {
            get
            {
                return DataSource.IsOption;
            }
        }
        #endregion
        #region public string OptionUnderlying
        /// <summary>
        /// Options only: Underlying symbol.
        /// </summary>
        public string OptionUnderlying
        {
            get
            {
                return DataSource.OptionUnderlying;
            }
        }
        #endregion
        #region public DateTime OptionExpiry
        /// <summary>
        /// Options only: expiry date.
        /// </summary>
        public DateTime OptionExpiry
        {
            get
            {
                return this[0].OptionExpiry;
            }
        }
        #endregion
        #region public bool OptionIsPut
        /// <summary>
        /// Options only: flag indicating put (true), or call (false).
        /// </summary>
        public bool OptionIsPut
        {
            get
            {
                return this[0].OptionIsPut;
            }
        }
        #endregion
        #region public double OptionStrike
        /// <summary>
        /// Options only: strike price.
        /// </summary>
        public double OptionStrike
        {
            get
            {
                return this[0].OptionStrike;
            }
        }
        #endregion

        #region public bool HasOHLC
        /// <summary>
        /// Flag indicating if this instrument has open/ high/ low/ close prices.
        /// </summary>
        public bool HasOHLC
        {
            get
            {
                return this[0].HasOHLC;
            }
        }
        #endregion
        #region public bool HasBidAsk
        /// <summary>
        /// Flag indicating if this instrument has bid/ ask prices.
        /// </summary>
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
        /// <summary>
        /// Time series with bar time stamps.
        /// </summary>
        public ITimeSeries<DateTime> Time
        {
            get
            {
                return _timeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Open
        /// <summary>
        /// Time series of opening prices.
        /// </summary>
        public ITimeSeries<double> Open
        {
            get
            {
                return _openSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> High
        /// <summary>
        /// Time series of high prices.
        /// </summary>
        public ITimeSeries<double> High
        {
            get
            {
                return _highSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Low
        /// <summary>
        /// Time series of low prices.
        /// </summary>
        public ITimeSeries<double> Low
        {
            get
            {
                return _lowSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Close
        /// <summary>
        /// Time series of closing prices.
        /// </summary>
        public ITimeSeries<double> Close
        {
            get
            {
                return _closeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<long> Volume
        /// <summary>
        /// Time series of trading volumes.
        /// </summary>
        public ITimeSeries<long> Volume
        {
            get
            {
                return _volumeSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Bid
        /// <summary>
        /// Time series of bid prices.
        /// </summary>
        public ITimeSeries<double> Bid
        {
            get
            {
                return _bidSeries;
            }
        }
        #endregion
        #region public ITimeSeries<double> Ask
        /// <summary>
        /// Time series of ask prices.
        /// </summary>
        public ITimeSeries<double> Ask
        {
            get
            {
                return _askSeries;
            }
        }
        #endregion
        #region public ITimeSeries<long> BidVolume
        /// <summary>
        /// Time series of bid volumes.
        /// </summary>
        public ITimeSeries<long> BidVolume
        {
            get
            {
                return _bidVolume;
            }
        }
        #endregion
        #region public ITimeSeries<long> AskVolume
        /// <summary>
        /// Time series of ask volumes.
        /// </summary>
        public ITimeSeries<long> AskVolume
        {
            get
            {
                return _askVolume;
            }
        }
        #endregion
        #region public ITimeSeries<bool> IsBidAskValid
        /// <summary>
        /// Time series of flags indicating bid/ ask price validity.
        /// </summary>
        public ITimeSeries<bool> IsBidAskValid
        {
            get
            {
                return _bidAskValid;
            }
        }
        #endregion

        //----- trading
        #region public Order Trade(int quantity, OrderType tradeExecution, double price, Func<Instrument, bool> condition)
        /// <summary>
        /// Submit trade for this instrument.
        /// </summary>
        /// <param name="quantity">number of contracts to trade</param>
        /// <param name="tradeExecution">type of trade execution</param>
        /// <param name="price">optional price specifier</param>
        /// <param name="condition">lambda, specifying exec condition</param>
        /// <returns>Order object</returns>
        public Order Trade(int quantity, OrderType tradeExecution = OrderType.openNextBar, double price = 0.00, Func<Instrument, bool> condition = null)
        {
            if (quantity == 0)
                return null;

            if (IsOption && OptionExpiry.Date < Simulator.SimTime[0].Date)
                return null;

            Order order = new Order()
            {
                Instrument = this,
                Quantity = quantity,
                Type = tradeExecution,
                Price = price,
                Condition = condition,
            };

            Simulator.QueueOrder(order);
            return order;
        }
        #endregion
        #region public int Position
        /// <summary>
        /// Return current open position size.
        /// </summary>
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