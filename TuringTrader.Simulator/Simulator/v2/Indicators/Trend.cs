//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Trend
// Description: Trend indicators.
// History:     2022xi02, FUB, created
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

using System.Collections.Generic;
using System.Linq;
using TuringTrader.SimulatorV2;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of trend indicators.
    /// </summary>
    public static class Trend
    {
        #region Sum
        #endregion
        #region SMA
        /// <summary>
        /// Simple Moving Average.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">filter length</param>
        /// <returns>SMA series</returns>
        public static TimeSeriesFloat SMA(this TimeSeriesFloat series, int n)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                var window = new Queue<double>();
                for (var i = 0; i < n; i++)
                    window.Enqueue(src[0].Value);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    window.Enqueue(src[idx].Value);
                    window.Dequeue();

                    var sma = window.Average(w => w);
                    dst.Add(new BarType<double>(src[idx].Date, sma));
                }

                return dst;
            }

            var name = string.Format("{0}.SMA({1})", series.Name, n);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region WMA
        #endregion
        #region EMA
        /// <summary>
        /// Exponential Moving Average.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">filter length</param>
        /// <returns>EMA series</returns>
        public static TimeSeriesFloat EMA(this TimeSeriesFloat series, int n)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();
                var ema = src[0].Value;
                var alpha = 2.0 / (1.0 + n);

                foreach (var it in src)
                {
                    ema += alpha * (it.Value - ema);
                    dst.Add(new BarType<double>(it.Date, ema));
                }

                return dst;
            }

            var name = string.Format("{0}.EMA({1})", series.Name, n);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion

        #region DEMA
        #endregion
        #region HMA
        #endregion
        #region TEMA
        #endregion

        #region ZLEMA
        #endregion
        #region KAMA
        #endregion
        #region MACD
        #endregion
    }
}

//==============================================================================
// end of file
