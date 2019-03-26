//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsVolatility
// Description: collection of volatility indicators
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static ITimeSeries<double> StandardDeviation(this ITimeSeries<double> series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            // see https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance

            // TODO: (1) rewrite using Linq. See WMA implementation
            //       (2) we should be able to remove try/catch blocks
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
                cacheId);
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
        public static ITimeSeries<double> FastStandardDeviation(this ITimeSeries<double> series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var functor = Cache<FunctorStandardDeviation>.GetData(
                    cacheId,
                    () => new FunctorStandardDeviation(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorStandardDeviation : TimeSeries<double>
        {
            public readonly ITimeSeries<double> Series;
            public readonly int N;

            private readonly double _alpha;
            private double? _average = null;
            private double _variance;

            public FunctorStandardDeviation(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = Math.Max(2, n);
                _alpha = 2.0 / (N + 1.0);
            }

            public void Calc()
            {
                // TODO: check if we can remove try/catch here
                try
                {
                    // calculate exponentially-weighted mean and variance
                    // see Tony Finch, Incremental calculation of mean and variance
                    double diff = Series[0] - (double)_average;
                    double incr = _alpha * diff;
                    _average = _average + incr;
                    _variance = (1.0 - _alpha) * (_variance + diff * incr);

                    Value = Math.Sqrt(_variance);
                }
                catch (Exception)
                {
                    // exception thrown, when _average is null
                    _average = Series[0];
                    _variance = 0.0;
                    Value = 0.0;
                }
            }
        }
        #endregion

        #region public static ITimeSeries<double> SemiDeviation(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate standard deviation, based on exponentially weighted filters. This is an
        /// incremental calculation, based on Tony Finch, which is very fast and efficient.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">filtering length</param>
        /// <returns>variance as time series</returns>
        public static SemiDeviationResult SemiDeviation(this ITimeSeries<double> series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var container = Cache<SemiDeviationResult>.GetData(
                    cacheId,
                    () => new SemiDeviationResult());

            container.Average = series.SMA(n);

            container.Downside = IndicatorsBasic.BufferedLambda(
                v =>
                {
                    var downSeries = Enumerable.Range(0, n)
                        .Where(t => series[t] < container.Average[0]);

                    if (downSeries.Count() == 0)
                        return 0.0;
                    else 
                        return Math.Sqrt(downSeries
                            .Average(t => Math.Pow(series[t] - container.Average[0], 2.0)));
                }, 0.0,
               cacheId);

            container.Upside = IndicatorsBasic.BufferedLambda(
                v =>
                {
                    var upSeries = Enumerable.Range(0, n)
                        .Where(t => series[t] > container.Average[0]);

                    if (upSeries.Count() == 0)
                        return 0.0;
                    else
                        return Math.Sqrt(upSeries
                            .Average(t => Math.Pow(series[t] - container.Average[0], 2.0)));
                }, 0.0,
                cacheId);

            return container;
        }

        /// <summary>
        /// Result for semi-deviation
        /// </summary>
        public class SemiDeviationResult
        {
            /// <summary>
            /// average of time series
            /// </summary>
            public ITimeSeries<double> Average;

            /// <summary>
            /// standard deviation to the upside
            /// </summary>
            public ITimeSeries<double> Upside;

            /// <summary>
            /// standard deviation to the downside
            /// </summary>
            public ITimeSeries<double> Downside;
        }
        #endregion

        #region public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate historical volatility.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return series
                .LogReturn(cacheId)
                .StandardDeviation(n, cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate volatility estimate from recent trading range.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hi = series.Highest(n)[0];
                    double lo = series.Lowest(n)[0];

                    return 0.80 * Math.Sqrt(1.0 / n) * Math.Log(hi / lo);
                },
                0.0,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> TrueRange(this Instrument series)
        /// <summary>
        /// Calculate True Range, non averaged, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">optional parent cache id</param>
        /// <returns>True Range as time series</returns>
        public static ITimeSeries<double> TrueRange(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) =>
                {
                    double high = Math.Max(series[0].High, series[1].Close);
                    double low = Math.Min(series[0].Low, series[1].Close);
                    return high - low;
                },
                cacheId);
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
        public static ITimeSeries<double> AverageTrueRange(this Instrument series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return series
                .TrueRange(cacheId)
                .SMA(n, cacheId);
        }
        #endregion

        // - Bollinger Bands
    }
}

//==============================================================================
// end of file