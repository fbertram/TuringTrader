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

namespace FUB_TradingSim
{
    public static class IndicatorsVolatility
    {
        #region Volatility
        /// <summary>
        /// Return volatility of time series
        /// </summary>
        public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = DataCache<FunctorVolatility>.GetCachedData(
                    cacheKey,
                    () => new FunctorVolatility(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorVolatility : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorVolatility(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
            }

            public void Calc()
            {
                double sum = 0.0;
                double sum2 = 0.0;
                int num = 0;

                try
                {
                    for (int t = 0; t < N; t++)
                    {
                        double logReturn = Math.Log(Series[t] / Series[t + 1]);
                        sum += logReturn;
                        sum2 += logReturn * logReturn;
                        num++;
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                // see https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance
                double variance = num > 1
                    ? (sum2 - sum * sum / num) / (num - 1)
                    : 0.0;

                Value = Math.Sqrt(252.0 * variance);
            }
        }
#endregion
        #region VolatilityFromRange
        /// <summary>
        /// Return volatility of time series, based on recent trading range
        /// </summary>
        public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = DataCache<FunctorVolatilityFromRange>.GetCachedData(
                    cacheKey,
                    () => new FunctorVolatilityFromRange(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorVolatilityFromRange : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorVolatilityFromRange(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
            }

            public void Calc()
            {
                double hi = Series[0];
                double lo = Series[0];

                try
                {
                    for (int t = 1; t < N; t++)
                    {
                        hi = Math.Max(hi, Series[t]);
                        lo = Math.Min(lo, Series[t]);
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                double volatility = 0.63 * Math.Sqrt(252.0 / N) * Math.Log(hi / lo);
                Value = volatility;
            }
        }
#endregion
        #region FastVariance - exponentially weighted variance
        public static ITimeSeries<double> FastVariance(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = DataCache<FunctorFastVariance>.GetCachedData(
                    cacheKey,
                    () => new FunctorFastVariance(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorFastVariance : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            private double _alpha;
            private double _average;

            public FunctorFastVariance(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
                _alpha = 2.0 / (n + 1.0);
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
                    Value = (1.0 - _alpha) * (this[1] + diff * incr);
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
    }
}

//==============================================================================
// end of file