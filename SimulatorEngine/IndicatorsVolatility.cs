//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsVolatility
// Description: collection of volatility indicators
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
    /// Collection of volatility indicators.
    /// </summary>
    public static class IndicatorsVolatility
    {
        #region public static ITimeSeries<double> StandardDeviation(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate historical standard deviation.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length</param>
        /// <returns>standard deviation as time series</returns>
        public static ITimeSeries<double> StandardDeviation(this ITimeSeries<double> series, int n = 10)
        {
            // see https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double sum = 0.0;
                    double sum2 = 0.0;
                    int num = 0;

                    try
                    {
                        for (int t = 0; t < n; t++)
                        {
                            sum += series[t];
                            sum2 += series[t] * series[t];
                            num++;
                        }
                    }
                    catch (Exception)
                    {
                        // we get here when we access bars too far in the past
                    }

                    double variance = num > 1
                        ? (sum2 - sum * sum / num) / (num - 1)
                        : 0.0;

                    return Math.Sqrt(Math.Max(0.0, variance));
                },
                0.0,
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> FastStandardDeviation(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate standard deviation, based on exponentially weighted filters. This is an
        /// incremental calculation, based on Tony Finch, which is very fast and efficient.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">filtering length</param>
        /// <returns>variance as time series</returns>
        public static ITimeSeries<double> FastStandardDeviation(this ITimeSeries<double> series, int n = 10)
        {
            var functor = Cache<FunctorStandardDeviation>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n),
                    () => new FunctorStandardDeviation(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorStandardDeviation : TimeSeries<double>
        {
            public readonly ITimeSeries<double> Series;
            public readonly int N;

            private readonly double _alpha;
            private double _average;
            private double _variance;

            public FunctorStandardDeviation(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = Math.Max(2, n);
                _alpha = 2.0 / (N + 1.0);
            }

            public void Calc()
            {
                try
                {
                    // calculate exponentially-weighted mean and variance
                    // see Tony Finch, Incremental calculation of mean and variance
                    double diff = Series[0] - _average;
                    double incr = _alpha * diff;
                    _average = _average + incr;
                    _variance = (1.0 - _alpha) * (_variance + diff * incr);

                    Value = Math.Sqrt(_variance);
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    _average = Series[0];
                    Value = 0.0;
                }
            }
        }
        #endregion

        #region public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate historical volatility.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n = 10)
        {
            return series.LogReturn().StandardDeviation(n);
        }
        #endregion
        #region public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate volatility estimate from recent trading range.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hi = series.Highest(n)[0];
                    double lo = series.Lowest(n)[0];

                    return 0.80 * Math.Sqrt(1.0 / n) * Math.Log(hi / lo);
                },
                0.0,
                series.GetHashCode(), n);
        }
        #endregion

        #region public static ITimeSeries<double> TrueRange(this Instrument series)
        /// <summary>
        /// Calculate True Range, non averaged, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>True Range as time series</returns>
        public static ITimeSeries<double> TrueRange(this Instrument series)
        {
            return IndicatorsBasic.Lambda(
                (t) =>
                {
                    double high = Math.Max(series[0].High, series[1].Close);
                    double low = Math.Min(series[0].Low, series[1].Close);
                    return high - low;
                },
                series.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> AverageTrueRange(this Instrument series, int n)
        /// <summary>
        /// Calculate Averaged True Range, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>ATR time series</returns>
        public static ITimeSeries<double> AverageTrueRange(this Instrument series, int n)
        {
            return series.TrueRange().SMA(n);
        }
        #endregion

        // - Bollinger Bands
    }
}

//==============================================================================
// end of file