//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        TimeSeries
// Description: Time series class.
// History:     2022x26, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2
{
    #region class BarType
    /// <summary>
    /// Template class for simulator bars.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BarType<T>
    {
        /// <summary>
        /// Bar date.
        /// </summary>
        public readonly DateTime Date;
        /// <summary>
        /// Bar value.
        /// </summary>
        public readonly T Value;

        /// <summary>
        /// Create new bar object.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="value"></param>
        public BarType(DateTime date, T value)
        {
            Date = date;
            Value = value;
        }
    }
    #endregion
    #region class TimeSeries<T>
    /// <summary>
    /// Class to encapsulate time-series data. The class is designed
    /// to receive its data from an asynchronous task.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TimeSeries<T>
    {
        #region internal stuff
        /// <summary>
        /// Task to retrieve time series' data. Note that these data are untyped,
        /// and that an extract function is required to wait for the result, and unpack them.
        /// </summary>
        protected readonly Task<object> _retrieveUntyped;
        private readonly Task<List<BarType<T>>> _retrieveTyped;
        private readonly Func<object, List<BarType<T>>> _extract;
        private long _lastTicks = default;
        private int _CurrentIndex = 0;
        private int GetIndex(DateTime date = default)
        {
            // NOTE: this routine is time critical as it is called
            //       many thousand times thoughout a typical backtest.
            //       there are a few cases to consider:
            //       - repeated calls. This should be the typical
            //         case and will, for most strategies, require
            //         only one (or very few) increments forward.
            //       - first call for a series. This may happen deep
            //         into a backtest, requiring an optimized
            //         jump forward.
            //       - call by a Lambda. Here we will nest a new simloop
            //         inside the existing one, leading to restart
            //         from the beginning.
            //       - lookup by date. This case is different, as it is
            //         independent from the simulator's state. Consequently,
            //         we should preserve the current index position.

            var data = Data;
            if (data.Count == 0)
                Output.ThrowError("No data for time series {0}", Name);

            var isSimloop = date == default;
            var lookupDate = isSimloop ? Owner.SimDate : date;

            int index;
            if (
                _lastTicks == default            // first reference to this series
                || lookupDate.Ticks < _lastTicks // nested simloop
                || !isSimloop)                   // lookup by date
            {
                // coarse jump
                // this is needed when we either didn't look up this series before,
                // we encounter a nested simloop, or we look up by date
                // here, we make a coarse jump first, aiming to save time
                // searching through the time stamps
                var daysPerBar = (data.Last().Date - data.First().Date).TotalDays / data.Count;
                index = (int)Math.Floor(
                    Math.Max(0, Math.Min(data.Count - 1,
                        (date - data.First().Date).TotalDays * daysPerBar)));

                // coarse jump might have been too far
                while (index > 0 && data[index].Date > lookupDate)
                    index--;
            }
            else
            {
                // regular advance
                // this happens when we make continued references to a series
                // while the simulator advances. in this case, the previous
                // index is a time-saving starting point
                index = _CurrentIndex;
            }

            // advance time forward
            // typically, this should only require very few iterations,
            // as we either go from the previous index, or the coarse jump
            while (index < data.Count - 1 && data[index + 1].Date <= lookupDate)
                index++;

            // save the current index so our next lookup will start there
            if (isSimloop)
            {
                _CurrentIndex = index;
                _lastTicks = lookupDate.Ticks;
            }

            return index;
        }

        private DateTime GetDate(int offset)
        {
            var data = Data;
            var baseIdx = GetIndex();
            var idx = Math.Max(0, Math.Min(data.Count - 1, baseIdx - offset));

            return data[idx].Date;
        }
        #endregion

        /// <summary>
        /// Create new time series. The retrieval function is untyped, requiring
        /// an extraction function to unpack the data.
        /// </summary>
        /// <param name="owner">parent algorithm</param>
        /// <param name="name">time series name</param>
        /// <param name="retrieve">data retrieval task</param>
        /// <param name="extract">data extraction function</param>
        public TimeSeries(Algorithm owner, string name, Task<object> retrieve, Func<object, List<BarType<T>>> extract)
        {
            Owner = owner;
            Name = name;
            Time = new TimeIndexer<T>(this);
            _retrieveUntyped = retrieve;
            _extract = extract;
        }

        /// <summary>
        /// Create new time series.
        /// </summary>
        /// <param name="owner">parent/ owning algorithm</param>
        /// <param name="name">time series name</param>
        /// <param name="retrieve">data retrieval task</param>
        public TimeSeries(Algorithm owner, string name, Task<List<BarType<T>>> retrieve)
        {
            Owner = owner;
            Name = name;
            Time = new TimeIndexer<T>(this);
            _retrieveTyped = retrieve;
        }

        /// <summary>
        /// Create new time series
        /// </summary>
        /// <param name="owner">parent/ owning algorithm</param>
        /// <param name="name">time series name</param>
        /// <param name="data">time series data</param>
        public TimeSeries(Algorithm owner, string name, List<BarType<T>> data)
        {
            Owner = owner;
            Name = name;
            Time = new TimeIndexer<T>(this);
            _retrieveTyped = Task.FromResult(data);
        }

        /// <summary>
        /// Algorithm instance owning this time series. This is the algorithm
        /// instance that holds this time series in its cache. Note that for
        /// child algorithms, this is not the same instance that generated
        /// the data.
        /// </summary>
        public readonly Algorithm Owner;

        /// <summary>
        /// Name of the time series. For assets, this is the nickname used
        /// to initially load the data. For indicators, this name is 
        /// typically derived from any input series, the indicator's name, 
        /// and its parameters.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The time series data. Note that accessing this field will
        /// wait for the retrieval task to finish and then extract the data.
        /// </summary>
        public List<BarType<T>> Data => _retrieveTyped != null ? _retrieveTyped.Result : _extract(_retrieveUntyped.Result);

        /// <summary>
        /// Indexer to return time series value at offset.
        /// </summary>
        /// <param name="offset">number of bars to offset, positive indices are in the past</param>
        /// <returns>value at offset</returns>
        public T this[int offset]
        {
            get
            {
                var data = Data;
                var baseIdx = GetIndex();
                var idx = Math.Max(0, Math.Min(data.Count - 1, baseIdx - offset));

                return data[idx].Value;
            }
        }

        /// <summary>
        /// Indexer to return time series value at a specific date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns>value at date</returns>
        public T this[DateTime date]
        {
            get
            {
                var data = Data;
                var idx = GetIndex(date);
                return data[idx].Value;
            }
        }

        /// <summary>
        /// Helper class to allow retrieval of the series' timestamp at an
        /// offset relative to the parent algorithm's simulator timestamp.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        public class TimeIndexer<T2>
        {
            private readonly TimeSeries<T2> TimeSeries;

            /// <summary>
            /// Create time indexer object.
            /// </summary>
            /// <param name="timeSeries"></param>
            public TimeIndexer(TimeSeries<T2> timeSeries)
            {
                TimeSeries = timeSeries;
            }

            /// <summary>
            /// Indexer to access time series's timestamp at an
            /// offset relative to the parent algorithm's simulator time.
            /// </summary>
            /// <param name="offset"></param>
            /// <returns></returns>
            public DateTime this[int offset] { get => TimeSeries.GetDate(offset); }
        }

        /// <summary>
        /// Time series indexer to return timestamp at offset.
        /// </summary>
        public TimeIndexer<T> Time = null; // instantiated in constructor
    }
    #endregion
    #region class OHLCV
    /// <summary>
    /// Simple container for open/ high/ low/ close prices and volume.
    /// </summary>
    public class OHLCV
    {
        /// <summary>
        /// Opening price.
        /// </summary>
        public readonly double Open;
        /// <summary>
        /// Highest price.
        /// </summary>
        public readonly double High;
        /// <summary>
        /// Lowest price.
        /// </summary>
        public readonly double Low;
        /// <summary>
        /// Closing price.
        /// </summary>
        public readonly double Close;
        /// <summary>
        /// Bar volume.
        /// </summary>
        public readonly double Volume;

        /// <summary>
        /// Construct new OHLCV object
        /// </summary>
        /// <param name="o">opening price</param>
        /// <param name="h">highest price</param>
        /// <param name="l">lowest price</param>
        /// <param name="c">closing price</param>
        /// <param name="v">volume</param>
        public OHLCV(double o, double h, double l, double c, double v)
        {
            Open = o;
            High = h;
            Low = l;
            Close = c;
            Volume = v;
        }

        /// <summary>
        /// Convert to string value.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("o={0:C2}, h={1:C2}, l={2:C2}, c={3:C2}, v={4:F0}", Open, High, Low, Close, Volume);
        }
    }
    #endregion
    #region class TimeSeriesAsset
    /// <summary>
    /// Time series for asset data. Assets include any quotes retrieved from
    /// supported data feeds or child algorithms.
    /// </summary>
    public class TimeSeriesAsset : TimeSeries<OHLCV>
    {
        #region internal stuff
        private Func<object, MetaType> _extractMeta;

        private TimeSeriesFloat ExtractFieldSeries(string fieldName, Func<OHLCV, double> extractFun)
        {
            var name = Name + "." + fieldName;

            return Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var ohlcv = Data;
                            var data = new List<BarType<double>>();

                            foreach (var it in ohlcv)
                                data.Add(new BarType<double>(it.Date, extractFun(it.Value)));

                            return data;
                        }));

                    return new TimeSeriesFloat(Owner, name, data);
                });
        }
        #endregion

        /// <summary>
        /// Create new time series for an asset.
        /// </summary>
        /// <param name="owner">Algorithm instance requesting/ owning this time series</param>
        /// <param name="name">Asset's nickname</param>
        /// <param name="retrieve">data retrieval task</param>
        /// <param name="extractBars">extractor for time series bars</param>
        /// <param name="extractMeta">extractor for meta information</param>
        public TimeSeriesAsset(
            Algorithm owner, string name,
            Task<object> retrieve,
            Func<object, List<BarType<OHLCV>>> extractBars,
            Func<object, MetaType> extractMeta)
            : base(owner, name, retrieve, extractBars)
        {
            _extractMeta = extractMeta;
        }

        /// <summary>
        /// Container class to store meta data for TimeSeriesAsset.
        /// </summary>
        public class MetaType
        {
            /// <summary>
            /// Asset's ticker symbol. 
            /// </summary>
            public string Ticker;
            /// <summary>
            /// Asset's full descriptive name.
            /// </summary>
            public string Description;
            /// <summary>
            /// Algorithm instance that generated asset's data.
            /// </summary>
            public Algorithm Generator;
        }

        /// <summary>
        /// Asset's meta data including its ticker symbol and descriptive name.
        /// </summary>
        public MetaType Meta => _extractMeta(_retrieveUntyped.Result);

        /// <summary>
        /// Convenience function to asset's full descriptive name.
        /// </summary>
        public string Description => Meta.Description;

        /// <summary>
        /// Convenience function to return asset's ticker symbol.
        /// </summary>
        public string Ticker => Meta.Ticker;

        /// <summary>
        /// Return time series of opening prices.
        /// </summary>
        public TimeSeriesFloat Open => ExtractFieldSeries("Open", bar => bar.Open);
        /// <summary>
        /// Return time series of highest prices.
        /// </summary>
        public TimeSeriesFloat High => ExtractFieldSeries("High", bar => bar.High);
        /// <summary>
        /// Return time series of lowest prices.
        /// </summary>
        public TimeSeriesFloat Low => ExtractFieldSeries("Low", bar => bar.Low);
        /// <summary>
        /// Return time series of closing prices.
        /// </summary>
        public TimeSeriesFloat Close => ExtractFieldSeries("Close", bar => bar.Close);
        /// <summary>
        /// Return time series of trading volumes.
        /// </summary>
        public TimeSeriesFloat Volume => ExtractFieldSeries("Volume", bar => bar.Volume);

        /// <summary>
        /// Set target allocation as fraction of account's NAV.
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="orderType"></param>
        /// <param name="orderPrice"></param>
        public void Allocate(double weight, OrderType orderType, double orderPrice = 0.0)
        {
            Owner.Account.SubmitOrder(Name, weight, orderType, orderPrice);
        }

        /// <summary>
        /// Return position as fraction of account's NAV.
        /// </summary>
        public double Position
        {
            get
            {
                var positions = Owner.Account.Positions
                    .Where(kv => kv.Key == Name)
                    .Select(kv => kv.Value);

                return positions.Count() != 0 ? positions.First() : 0.0;
            }
        }
    }
    #endregion
    #region class TimeSeriesFloat
    /// <summary>
    /// Time series for floating point data. This is mainly used for
    /// indicators.
    /// </summary>
    public class TimeSeriesFloat : TimeSeries<double>
    {
        /// <summary>
        /// Create and cache new time series.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="name"></param>
        /// <param name="retrieve">task to retrieve data</param>
        /// <param name="extract">function to extract data</param>
        public TimeSeriesFloat(
            Algorithm owner, string name,
            Task<object> retrieve,
            Func<object, List<BarType<double>>> extract)
                : base(owner, name, retrieve, extract)
        { }

        /// <summary>
        /// Create and cache new time series.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="name"></param>
        /// <param name="retrieve">task to retrieve data</param>
        public TimeSeriesFloat(
            Algorithm owner, string name,
            Task<List<BarType<double>>> retrieve)
            : base(owner, name, retrieve)
        { }

        /// <summary>
        /// Create and cache new time series.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="name"></param>
        /// <param name="data">data for time series</param>
        public TimeSeriesFloat(
            Algorithm owner, string name,
            List<BarType<double>> data)
            : base(owner, name, data)
        { }
    }
    #endregion
    #region class TimeSeriesBool
    /*
    /// <summary>
    /// Time series for boolean data. This is used for signals.
    /// </summary>
    public class TimeSeriesBool : TimeSeries<bool>
    {
        public TimeSeriesBool(Algorithm owner, string name, Task<object> retrieve) : base(owner, name, retrieve)
        {
        }
    }
    */
    #endregion
}

//==============================================================================
// end of file
