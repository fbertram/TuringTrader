//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Trend
// Description: Trend indicators.
// History:     2022xi02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of trend indicators.
    /// </summary>
    public static class Trend
    {
        #region Sum
        /// <summary>
        /// Calculate rolling sum.
        /// </summary>
        #endregion
        #region SMA
        /// <summary>
        /// Calculate Simple Moving Average as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Simple_moving_average"/>
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">filter length</param>
        /// <returns>SMA series</returns>
        public static TimeSeriesFloat SMA(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.SMA({1})", series.Name, n);

            return series.Algorithm.Cache(
                name,
                () => new TimeSeriesFloat(
                    series.Algorithm, name,
                    Task.Run(() =>
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
                    })));
        }
        #endregion
        #region WMA
        /// <summary>
        /// Calculate Weighted Moving Average as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Weighted_moving_average"/>
        /// </summary>
        #endregion
        #region EMA
        /// <summary>
        /// Calculate Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average"/>
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">filter length</param>
        /// <returns>EMA series</returns>
        public static TimeSeriesFloat EMA(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.EMA({1})", series.Name, n);

            return series.Algorithm.Cache(
                name,
                () => new TimeSeriesFloat(
                    series.Algorithm, name,
                    Task.Run(() =>
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
                    })));
        }
        #endregion

        #region DEMA
        /// <summary>
        /// Calculate Double Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Double_exponential_moving_average"/>
        /// </summary>
        #endregion
        #region HMA
        /// <summary>
        /// Calculate Hull Moving Average, as described here:
        /// <see href="https://alanhull.com/hull-moving-average"/>
        /// </summary>
        #endregion
        #region TEMA
        /// <summary>
        /// Calculate Triple Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Triple_exponential_moving_average"/>
        /// </summary>
        #endregion

        #region ZLEMA
        /// <summary>
        /// Calculate Ehlers' Zero Lag Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Zero_lag_exponential_moving_average"/>
        /// </summary>
        #endregion
        #region KAMA
        /// <summary>
        /// Calculate Kaufman's Adaptive Moving Average, as described here:
        /// <see href="https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:kaufman_s_adaptive_moving_average"/>
        /// </summary>
        #endregion
        #region MACD
        /// <summary>
        /// Calculate MACD, as described here:
        /// <see href="https://en.wikipedia.org/wiki/MACD"/>
        /// </summary>
        #endregion
    }
}

//==============================================================================
// end of file
