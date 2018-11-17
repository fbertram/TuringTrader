//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMomentum
// Description: collection of momentum-based indicators
// History:     2018ix15, FUB, created
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
    /// Collection of momentum-based indicators.
    /// </summary>
    public static class IndicatorsMomentum
    {
        // TODO:
        // - Stochastic Oscillator

        #region public static ITimeSeries<double> CCI(this Instrument series, int n = 20)
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">averaging length</param>
        /// <returns>CCI time series</returns>
        public static ITimeSeries<double> CCI(this Instrument series, int n = 20)
        {
            return series.TypicalPrice().CCI(n);
        }
        #endregion
        #region public static ITimeSeries<double> CCI(this ITimeSeries<double> series, int n = 20)
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>CCI time series</returns>
        public static ITimeSeries<double> CCI(this ITimeSeries<double> series, int n = 20)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    ITimeSeries<double> delta = series.Subtract(series.SMA(n));
                    ITimeSeries<double> meanDeviation = delta.AbsValue().SMA(n);

                    return delta[0] / (0.15 * meanDeviation[0]);
                },
                0.5,
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> TSI(this ITimeSeries<double> series, int r = 25, int s = 13)
        /// <summary>
        /// Calculate True Strength Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/True_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="r">smoothing period for momentum</param>
        /// <param name="s">smoothing period for smoothed momentum</param>
        /// <returns>TSI time series</returns>
        public static ITimeSeries<double> TSI(this ITimeSeries<double> series, int r = 25, int s = 13)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    ITimeSeries<double> momentum = series.Return();
                    double numerator = momentum.EMA(r).EMA(s)[0];
                    double denominator = momentum.AbsValue().EMA(r).EMA(s)[0];
                    return 100.0 * numerator / denominator;
                },
                0.5,
                series.GetHashCode(), r, s);
        }
        #endregion
        #region public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Relative Strength Index, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Relative_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">smoothing period</param>
        /// <returns>RSI time series</returns>
        public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n = 14)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double avgUp = IndicatorsBasic.Lambda(
                        (t) => Math.Max(0.0, series.Return()[t]),
                        series.GetHashCode(), n).EMA(n)[0];

                    double avgDown = IndicatorsBasic.Lambda(
                        (t) => Math.Max(0.0, -series.Return()[t]),
                        series.GetHashCode(), n).EMA(n)[0];

                    double rs = avgUp / Math.Max(1e-10, avgDown);
                    return 100.0 - 100.0 / (1 + rs);
                },
                50.0,
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> WilliamsPercentR(this Instrument series, int n = 10)
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">period</param>
        /// <returns>Williams %R as time series</returns>
        public static ITimeSeries<double> WilliamsPercentR(this Instrument series, int n = 10)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hh = series.High.Highest(n)[0];
                    double ll = series.Low.Lowest(n)[0];

                    return -100.0 * (hh - series.Close[0]) / (hh - ll);
                },
                50.0,
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> WilliamsPercentR(this ITimeSeries<double> series, int n = 10)
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">period</param>
        /// <returns>Williams %R as time series</returns>
        public static ITimeSeries<double> WilliamsPercentR(this ITimeSeries<double> series, int n = 10)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hh = series.Highest(n)[0];
                    double ll = series.Lowest(n)[0];

                    return -100.0 * (hh - series[0]) / (hh - ll);
                },
                50.0,
                series.GetHashCode(), n);
        }
        #endregion

        #region public static ITimeSeries<double> LinRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate linear regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <returns>annualized return as time series</returns>
        public static ITimeSeries<double> LinRegression(this ITimeSeries<double> series, int n)
        {
            // simple linear regression
            // see https://en.wikipedia.org/wiki/Simple_linear_regression
            // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
            //   = (n * Sxy - Sx * Sy) / (n * Sxx - Sx * Sx)
            // a = avg(y) - b * avg(x)
            //   = 1 / n * Sy - b /n * Sx

            // coefficient of determination
            // see https://en.wikipedia.org/wiki/Coefficient_of_determination
            // f = a + b * x
            // SSreg = sum((f - avg(y))^2)
            // SSres = sum((y - f)^2)

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double sx = 0.0;
                    double sy = 0.0;
                    double sxx = 0.0;
                    double sxy = 0.0;
                    int N = 0;

                    try
                    {
                        for (int t = 0; t < n; t++)
                        {
                            double x = -t;
                            double y = series[t];
                            sx += x;
                            sy += y;
                            sxx += x * x;
                            sxy += x * y;
                            N++;
                        }
                    }
                    catch (Exception)
                    {
                        // we get here when we access bars too far in the past
                    }

                    if (N > 1)
                    {
                        double b = (n * sxy - sx * sy) / (n * sxx - sx * sx);
                        double a = sy / n - b * sx / n;
                        return b;
                    }
                    else
                    {
                        return 0.0;
                    }
                },
                0.0,
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate logarithmic regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <returns>annualized return as time series</returns>
        public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        {
            ITimeSeries<double> logPrice = IndicatorsBasic.BufferedLambda(
                (v) => Math.Log(series[0]),
                0.0,
                series.GetHashCode(), n);

            return logPrice.LinRegression(n);
        }
        #endregion
    }
}

//==============================================================================
// end of file