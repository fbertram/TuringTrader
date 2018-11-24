//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsTrend
// Description: collection of trend-based indicators
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
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of trend-based indicators.
    /// </summary>
    public static class IndicatorsTrend
    {
        #region public static ITimeSeries<double> SMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Simple Moving Average as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Simple_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>SMA time series</returns>
        public static ITimeSeries<double> SMA(this ITimeSeries<double> series, int n)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double sum = series[0];
                    int num = 1;

                    try
                    {
                        for (int t = 1; t < n; t++)
                        {
                            sum += series[t];
                            num++;
                        }
                    }
                    catch (Exception)
                    {
                        // we get here when we access bars too far in the past
                    }

                    return sum / num;
                },
                series[0],
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> EMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>EMA time series</returns>
        public static ITimeSeries<double> EMA(this ITimeSeries<double> series, int n)
        {
            int N = Math.Max(1, n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double alpha = 2.0 / (N + 1);
                    double r = alpha * (series[0] - v) + v;
                    return double.IsNaN(r) ? 0.0 : r;
                },
                series[0],
                series.GetHashCode(), N);
        }
        #endregion
        #region public static ITimeSeries<double> EnvelopeDetector(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate envelope of time series. For input values higher than the current
        /// output value, the output follows the input immediately. For input values lower,
        /// the output is an EMA of the input. The overall function is much like an
        /// envelope detector in electronics.
        /// <see href="https://en.wikipedia.org/wiki/Envelope_detector"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>envelope time series</returns>
        public static ITimeSeries<double> EnvelopeDetector(this ITimeSeries<double> series, int n)
        {
            int N = Math.Max(1, n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double alpha = 2.0 / (N + 1);
                    double r = series[0] >= v
                        ? series[0]
                        : alpha * (series[0] - v) + v;
                    return double.IsNaN(r) ? 0.0 : r;
                },
                series[0],
                series.GetHashCode(), N);
        }
        #endregion

        #region public static ITimeSeries<double> DEMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Double Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Double_exponential_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>DEMA time series</returns>
        public static ITimeSeries<double> DEMA(this ITimeSeries<double> series, int n)
        {
            return series
                .Multiply(2.0)
                .EMA(n)
                .Subtract(series
                    .EMA(n)
                    .EMA(n));
        }
        #endregion
        #region public static ITimeSeries<double> TEMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Triple Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Triple_exponential_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>TEMA time series</returns>
        public static ITimeSeries<double> TEMA(this ITimeSeries<double> series, int n)
        {
            return series
                .Multiply(3.0)
                .EMA(n)
                .Subtract(series
                    .Multiply(3.0)
                    .EMA(n)
                    .EMA(n))
                .Add(series
                    .EMA(n)
                    .EMA(n)
                    .EMA(n));
        }
        #endregion

        #region public static ITimeSeries<double> ZLEMA(this ITimeSeries<double> series, int period)
        /// <summary>
        /// Calculate Ehlers' Zero Lag Exponential Moving Average, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Zero_lag_exponential_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="period">averaging length</param>
        /// <returns>ZLEMA as time series</returns>
        public static ITimeSeries<double> ZLEMA(this ITimeSeries<double> series, int period)
        {
            int lag = (int)Math.Round((period - 1.0) / 2.0);

            return series
                .Add(series
                    .Subtract(series
                        .Delay(lag)))
                .EMA(period);
        }
        #endregion

        #region public static ITimeSeries<double> KAMA(this ITimeSeries<double> series, int erPeriod, int fastEma, int slowEma)
        /// <summary>
        /// Calculate Kaufman's Adaptive Moving Average, as described here:
        /// <see href="https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:kaufman_s_adaptive_moving_average"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="erPeriod">period for efficiency ratio</param>
        /// <param name="fastEma">period for fast EMA</param>
        /// <param name="slowEma">period for slow EMA</param>
        /// <returns>KAMA as time series</returns>
        public static ITimeSeries<double> KAMA(this ITimeSeries<double> series, int erPeriod = 10, int fastEma = 2, int slowEma = 30)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    try
                    {
                        double scFast = 2.0 / (1.0 + fastEma);
                        double scSlow = 2.0 / (1.0 + slowEma);

                        double change = Math.Abs(series[0] - series[erPeriod]);
                        double volatility = Enumerable.Range(0, erPeriod)
                            .Sum(t => Math.Abs(series[t] - series[t + 1]));

                        double efficiencyRatio = change / Math.Max(1e-10, volatility);
                        double smoothingConstant = Math.Pow(efficiencyRatio * (scFast - scSlow) + scSlow, 2);

                        double r = smoothingConstant * (series[0] - v) + v;
                        return double.IsNaN(r) ? 0.0 : r;
                    }
                    catch (Exception)
                    {
                        // we get here when we access bars too far in the past
                        return series[0];
                    }
                },
                series[0],
                series.GetHashCode(), erPeriod, fastEma, slowEma);
        }
        #endregion

        #region public static MACDResult MACD(this ITimeSeries<double> series, int fast = 12, int slow = 26, int signal = 9)
        /// <summary>
        /// Calculate MACD, as described here:
        /// <see href="https://en.wikipedia.org/wiki/MACD"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="fast">fast EMA period</param>
        /// <param name="slow">slow EMA period</param>
        /// <param name="signal">signal line period</param>
        /// <returns></returns>
        public static MACDResult MACD(this ITimeSeries<double> series, int fast = 12, int slow = 26, int signal = 9)
        {
            var container = Cache<MACDResult>.GetData(
                    Cache.UniqueId(series.GetHashCode(), fast, slow, signal),
                    () => new MACDResult());

            container.Fast = series.EMA(fast);
            container.Slow = series.EMA(slow);
            container.MACD = container.Fast.Subtract(container.Slow);
            container.Signal = container.MACD.EMA(signal);
            container.Divergence = container.MACD.Subtract(container.Signal);

            return container;
        }

        /// <summary>
        /// Container for MACD result.
        /// </summary>
        public class MACDResult
        {
            /// <summary>
            /// Fast EMA.
            /// </summary>
            public ITimeSeries<double> Fast;

            /// <summary>
            /// Slow EMA.
            /// </summary>
            public ITimeSeries<double> Slow;

            /// <summary>
            /// MACD, as the difference between fast and slow EMA.
            /// </summary>
            public ITimeSeries<double> MACD;

            /// <summary>
            /// Signal line, as the EMA of the MACD.
            /// </summary>
            public ITimeSeries<double> Signal;

            /// <summary>
            /// Divergence, the difference between MACD and signal line.
            /// </summary>
            public ITimeSeries<double> Divergence;
        }
        #endregion
    }
}

//==============================================================================
// end of file