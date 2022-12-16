//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        TimeSeries
// Description: Time series class.
// History:     2022x26, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2
{
    #region class BarType
    public class BarType<T>
    {
        public readonly DateTime Date;
        public readonly T Value;

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
        public readonly Algorithm Algorithm;
        public readonly string Name;
        public readonly Task<List<BarType<T>>> Data;
        public readonly Task<object> Meta;
        private double? _daysPerBar;

        /// <summary>
        /// Create new time series.
        /// </summary>
        /// <param name="algo">parent algorithm</param>
        /// <param name="name">time series name</param>
        /// <param name="data">time series data</param>
        public TimeSeries(Algorithm algo, string name, Task<List<BarType<T>>> data, Task<object> meta = null)
        {
            Algorithm = algo;
            Name = name;
            Data = data;
            Meta = meta;

            Time = new TimeIndexer<T>(this);
        }

        private int _CurrentIndex = 0;
        private int GetIndex(DateTime date = default)
        {
            var data = Data.Result;
            var lookupDate = date != default ? date : Algorithm.SimDate;

#if true
            // move forward in time (coarse)
            if (_daysPerBar == null) 
                _daysPerBar = (data.Last().Date - data.First().Date).TotalDays / data.Count;
            var deltaDays = (int)Math.Floor((lookupDate - data[_CurrentIndex].Date).TotalDays);
            var deltaBars = deltaDays / (double)_daysPerBar;
            if (deltaBars > 10)
                _CurrentIndex = Math.Max(0, Math.Min(data.Count - 1,
                    _CurrentIndex + (int)Math.Floor(deltaBars) - 1));
#endif

            // move forward in time (incrementally)
            while (_CurrentIndex < data.Count - 1 && data[_CurrentIndex + 1].Date <= lookupDate)
                _CurrentIndex++;

            // move back in time (incrementally)
            // this may be required due to the coarse forward jump
            while (_CurrentIndex > 0 && data[_CurrentIndex].Date > lookupDate)
                _CurrentIndex--;

            return _CurrentIndex;

        }

        /// <summary>
        /// Indexer to return time series value at offset.
        /// </summary>
        /// <param name="offset">number of bars to offset, positive indices are in the past</param>
        /// <returns>value at offset</returns>
        public T this[int offset]
        {
            get
            {
                var data = Data.Result;
                var baseIdx = GetIndex();
                var idx = Math.Max(0, Math.Min(data.Count - 1, baseIdx - offset));

                return data[idx].Value;
            }
        }

        public T this[DateTime date]
        {
            get
            {
                var data = Data.Result;
                var idx = GetIndex(date);
                return data[idx].Value;
            }
        }

        private DateTime GetDate(int offset)
        {
            var data = Data.Result;
            var baseIdx = GetIndex();
            var idx = Math.Max(0, Math.Min(data.Count - 1, baseIdx - offset));

            return data[idx].Date;
        }

        public class TimeIndexer<T>
        {
            private readonly TimeSeries<T> TimeSeries;
            public TimeIndexer(TimeSeries<T> timeSeries)
            {
                TimeSeries = timeSeries;
            }
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

        public override string ToString()
        {
            return string.Format("o={0:C2}, h={1:C2}, l={2:C2}, c={3:C2}, v={4:F0}", Open, High, Low, Close, Volume);
        }
    }
    #endregion
    #region class TimeSeriesAsset
    public class TimeSeriesAsset : TimeSeries<OHLCV>
    {
        public class MetaType
        {
            public string Ticker;
            public string Description;
        }
        public TimeSeriesAsset(Algorithm algo, string myId, Task<List<BarType<OHLCV>>> myData, Task<object> meta) : base(algo, myId, myData, meta)
        {
        }

        /// <summary>
        /// Return assets full descriptive name
        /// </summary>
        public string Description { get => ((MetaType)Meta.Result).Description; }
        public string Ticker { get => ((MetaType)Meta.Result).Ticker; }

        private TimeSeriesFloat ExtractFieldSeries(string fieldName, Func<OHLCV, double> extractFun)
        {
            List<BarType<double>> extractAsset()
            {
                var ohlcv = Data.Result; // wait until async result is available
                var data = new List<BarType<double>>();

                foreach (var it in ohlcv)
                    data.Add(new BarType<double>(it.Date, extractFun(it.Value)));

                return data;
            }

            var cacheId = Name + "." + fieldName;
            return new TimeSeriesFloat(
                Algorithm,
                cacheId,
                Algorithm.Cache(cacheId, extractAsset));
        }

        /// <summary>
        /// Return time series of opening prices.
        /// </summary>
        public TimeSeriesFloat Open { get => ExtractFieldSeries("Open", bar => bar.Open); }
        /// <summary>
        /// Return time series of highest prices.
        /// </summary>
        public TimeSeriesFloat High { get => ExtractFieldSeries("High", bar => bar.High); }
        /// <summary>
        /// Return time series of lowest prices.
        /// </summary>
        public TimeSeriesFloat Low { get => ExtractFieldSeries("Low", bar => bar.Low); }
        /// <summary>
        /// Return time series of closing prices.
        /// </summary>
        public TimeSeriesFloat Close { get => ExtractFieldSeries("Close", bar => bar.Close); }
        /// <summary>
        /// Return time series of trading volumes.
        /// </summary>
        public TimeSeriesFloat Volume { get => ExtractFieldSeries("Volume", bar => bar.Volume); }

        /// <summary>
        /// Set target allocation as fraction of account's NAV.
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="orderType"></param>
        public void Allocate(double weight, OrderType orderType)
        {
            Algorithm.Account.SubmitOrder(Name, weight, orderType);
        }

        /// <summary>
        /// Return position as fraction of account's NAV.
        /// </summary>
        public double Position
        {
            get
            {
                var positions = Algorithm.Account.Positions
                    .Where(kv => kv.Key == Name)
                    .Select(kv => kv.Value);

                return positions.Count() != 0 ? positions.First() : 0.0;
            }
        }
    }
    #endregion
    #region class TimeSeriesFloat
    public class TimeSeriesFloat : TimeSeries<double>
    {
        public TimeSeriesFloat(Algorithm algo, string myId, Task<List<BarType<double>>> myData) : base(algo, myId, myData)
        {
        }
    }
    #endregion
    #region class TimeSeriesBool
    public class TimeSeriesBool : TimeSeries<bool>
    {
        public TimeSeriesBool(Algorithm algo, string myId, Task<List<BarType<bool>>> myData) : base(algo, myId, myData)
        {
        }
    }
    #endregion
}

//==============================================================================
// end of file
