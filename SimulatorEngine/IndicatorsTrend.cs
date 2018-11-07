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
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double alpha = 2.0 / (n + 1);
                    return alpha * (series[0] - v) + v;
                },
                series[0],
                series.GetHashCode(), n);
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

                        return smoothingConstant * (series[0] - v) + v;
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
    }
}

//==============================================================================
// end of file