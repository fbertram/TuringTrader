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
        #region functor cache
        static List<FunctorVolatility> _FunctorCacheVolatility = new List<FunctorVolatility>();

        /// <summary>
        /// Return volatility of time series
        /// </summary>
        public static ITimeSeries<double> Volatility(this ITimeSeries<double> series, int n)
        {
            FunctorVolatility functor = null;
            foreach (FunctorVolatility f in _FunctorCacheVolatility)
            {
                if (f.Series == series && f.N == n)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorVolatility(series, n);
                _FunctorCacheVolatility.Add(functor);
            }

            functor.Calc();
            return functor;
        }
        #endregion

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
        #region functor cache
        static List<FunctorVolatilityFromRange> _FunctorCacheVolatilityFromRange = new List<FunctorVolatilityFromRange>();

        /// <summary>
        /// Return volatility of time series, based on recent trading range
        /// </summary>
        public static ITimeSeries<double> VolatilityFromRange(this ITimeSeries<double> series, int n)
        {
            FunctorVolatilityFromRange functor = null;
            foreach (FunctorVolatilityFromRange f in _FunctorCacheVolatilityFromRange)
            {
                if (f.Series == series && f.N == n)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorVolatilityFromRange(series, n);
                _FunctorCacheVolatilityFromRange.Add(functor);
            }

            functor.Calc();
            return functor;
        }
        #endregion

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
    }
}

//==============================================================================
// end of file