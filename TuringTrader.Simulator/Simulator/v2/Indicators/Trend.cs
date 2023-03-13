//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Trend
// Description: Trend indicators.
// History:     2022xi02, FUB, created
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
        public static TimeSeriesFloat Sum(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Sum({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var sum = Enumerable.Range(0, n)
                                    .Sum(t => src[Math.Max(0, idx - t)].Value);

                                dst.Add(new BarType<double>(src[idx].Date, sum));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var sma = Enumerable.Range(0, n)
                                    .Sum(t => src[Math.Max(0, idx - t)].Value / n);

                                dst.Add(new BarType<double>(src[idx].Date, sma));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region WMA
        /// <summary>
        /// Calculate Weighted Moving Average as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Weighted_moving_average"/>
        /// </summary>
        public static TimeSeriesFloat WMA(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.WMA({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            var denom = (n + 1.0) * n / 2.0;

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var wma = Enumerable.Range(0, n)
                                    .Sum(t => (n - t) * src[Math.Max(0, idx - t)].Value) / denom;

                                dst.Add(new BarType<double>(src[idx].Date, wma));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            var ema = src[0].Value;
                            var alpha = 2.0 / (1.0 + n);

                            foreach (var it in src)
                            {
                                ema += alpha * (it.Value - ema);
                                dst.Add(new BarType<double>(it.Date, ema));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion

        #region DEMA
        /// <summary>
        /// Calculate Double Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Double_exponential_moving_average"/>
        /// </summary>
        public static TimeSeriesFloat DEMA(this TimeSeriesFloat series, int n)
        {
            // DEMA = 2 × EMA − EMA(EMA)
            return series
                .EMA(n)
                .Mul(2.0)
                .Sub(series.EMA(n).EMA(n));
        }
        #endregion
        #region HMA
        /// <summary>
        /// Calculate Hull Moving Average, as described here:
        /// <see href="https://alanhull.com/hull-moving-average"/>
        /// </summary>
        public static TimeSeriesFloat HMA(this TimeSeriesFloat series, int n)
        {
            // Hull Moving Average (HMA) formula
            //     Integer(SquareRoot(Period)) WMA [2 x Integer(Period/2) WMA(Price) - Period WMA(Price)]
            //
            // MetaStock formula
            //     period:= Input("period", 1, 200, 20);
            //     sqrtperiod:= Sqrt(period);
            //     Mov(2 * Mov(C, period / 2, W) - Mov(C, period, W), LastValue(sqrtperiod), W);

            return series
                .WMA((int)Math.Round(n / 2.0))
                .Mul(2.0)
                .Sub(series.WMA(n))
                .WMA((int)Math.Round(Math.Sqrt(n)));
        }
        #endregion
        #region TEMA
        /// <summary>
        /// Calculate Triple Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Triple_exponential_moving_average"/>
        /// </summary>
        public static TimeSeriesFloat TEMA(this TimeSeriesFloat series, int n)
        {
            // TEMA = 3 x EMA - 3 x EMA(EMA) + EMA(EMA(EMA))
            return series
                .EMA(n)
                .Sub(series.EMA(n).EMA(n))
                .Mul(3.0)
                .Add(series.EMA(n).EMA(n).EMA(n));
        }
        #endregion

        #region ZLEMA
        /// <summary>
        /// Calculate Ehlers' Zero Lag Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Zero_lag_exponential_moving_average"/>
        /// </summary>
        public static TimeSeriesFloat ZLEMA(this TimeSeriesFloat series, int n)
        {
            // Lag = (Period - 1) / 2
            // EmaData = Data + (Data - Data(Lag))
            // ZMEMA = EMA(EmaData,  Period)
            return series
                .Mul(2.0)
                .Sub(series.Delay((int)Math.Floor((n - 1) / 2.0)))
                .EMA(n);
        }
        #endregion
        #region KAMA
        /// <summary>
        /// Calculate Kaufman's Adaptive Moving Average, as described here:
        /// <see href="https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:kaufman_s_adaptive_moving_average"/>
        /// </summary>
        public static TimeSeriesFloat KAMA(this TimeSeriesFloat series, int erPeriod = 10, int fastEMA = 2, int slowEMA = 30)
        {
            var name = string.Format("{0}.KAMA({1})", series.Name, erPeriod);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            var kama = src[0].Value;
                            double scFast = 2.0 / (1.0 + fastEMA);
                            double scSlow = 2.0 / (1.0 + slowEMA);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var change = Math.Abs(src[idx].Value - src[Math.Max(0, idx - erPeriod)].Value);
                                var volatility = Enumerable.Range(0, erPeriod)
                                    .Sum(t => Math.Abs(src[Math.Max(0, idx - t)].Value - src[Math.Max(0, idx - t - 1)].Value));

                                var er = change / Math.Max(1e-99, volatility);
                                var sc = Math.Pow(er * (scFast - scSlow) + scSlow, 2.0);

                                kama += sc * (src[idx].Value - kama);
                                dst.Add(new BarType<double>(src[idx].Date, kama));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region MACD
        /// <summary>
        /// Container for MACD results.
        /// </summary>
        public class MACDT
        {
            /// <summary>
            /// MACD line
            /// </summary>
            public readonly TimeSeriesFloat MACD;
            /// <summary>
            /// MACD signal line
            /// </summary>
            public readonly TimeSeriesFloat Signal;

            public MACDT(TimeSeriesFloat macd, TimeSeriesFloat signal)
            {
                MACD = macd;
                Signal = signal;
            }
        }
        /// <summary>
        /// Calculate MACD, as described here:
        /// <see href="https://en.wikipedia.org/wiki/MACD"/>
        /// </summary>
        public static MACDT MACD(this TimeSeriesFloat series, int fast = 12, int slow = 26, int signal = 9)
        {
            var name = string.Format("{0}.MACD({1},{2},{3})", series.Name, fast, slow, signal);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    /*var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            ...
                            return dst;
                        }));*/

                    // MACD = series.EMA(fast) - series.EMA(slow)
                    var macdLine = series
                        .EMA(fast)
                        .Sub(series.EMA(slow));

                    // Signal = MACD.EMA(signal)
                    var signalLine = macdLine.EMA(signal);

                    return new MACDT(macdLine, signalLine);
                });
        }
    }
    #endregion
}

//==============================================================================
// end of file
