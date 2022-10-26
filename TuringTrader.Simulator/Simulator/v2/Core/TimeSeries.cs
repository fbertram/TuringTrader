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
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator.v2
{
    public class TimeSeries<T>
    {
        public readonly Algorithm Algo;
        public readonly string CacheId;
        public readonly Task<Dictionary<DateTime, T>> Data;

        public TimeSeries(Algorithm algo, string cacheId, Task<Dictionary<DateTime, T>> data)
        {
            Algo = algo;
            CacheId = cacheId;
            Data = data;
        }
    }

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
    }

    public class TimeSeriesOHLCV : TimeSeries<OHLCV>
    {
        public TimeSeriesOHLCV(Algorithm algo, string myId, Task<Dictionary<DateTime, OHLCV>> myData) : base(algo, myId, myData)
        {
        }

        private TimeSeriesFloat ExtractFieldSeries(string fieldName, Func<OHLCV, double> extractFun)
        {
            Dictionary<DateTime, double> extractAsset()
            {
                var ohlcv = Data.Result; // wait until async result is available

                var data = new Dictionary<DateTime, double>();

                foreach (var timestamp in ohlcv.Keys)
                {
                    data[timestamp] = extractFun(ohlcv[timestamp]);
                }

                return data;
            }

            var cacheId = CacheId + "." + fieldName;
            return new TimeSeriesFloat(
                Algo,
                cacheId,
                Algo.Cache(cacheId, extractAsset));
        }
        public TimeSeriesFloat Open { get => ExtractFieldSeries("Open", bar => bar.Open); }
        public TimeSeriesFloat High { get => ExtractFieldSeries("High", bar => bar.High); }
        public TimeSeriesFloat Low { get => ExtractFieldSeries("Low", bar => bar.Low); }
        public TimeSeriesFloat Close { get => ExtractFieldSeries("Close", bar => bar.Close); }
        public TimeSeriesFloat Volume { get => ExtractFieldSeries("Volume", bar => bar.Volume); }
    }

    public class TimeSeriesFloat : TimeSeries<double>
    {
        public TimeSeriesFloat(Algorithm algo, string myId, Task<Dictionary<DateTime, double>> myData) : base(algo, myId, myData)
        {
        }
    }
}

//==============================================================================
// end of file
