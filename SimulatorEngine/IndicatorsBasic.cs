//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsBasic
// Description: collection of basic indicators
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
    public static class IndicatorsBasic
    {
        #region public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// highest value of past number of bars
        /// </summary>
        public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = Cache<FunctorHighest>.GetData(
                    cacheKey,
                    () => new FunctorHighest(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorHighest : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorHighest(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
            }

            public void Calc()
            {
                double value = Series[0];

                try
                {
                    for (int t = 1; t < N; t++)
                        value = Math.Max(value, Series[t]);
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                Value = value;
            }
        }
        #endregion
        #region public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// lowest value of past number of bars
        /// </summary>
        public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = Cache<FunctorLowest>.GetData(
                    cacheKey,
                    () => new FunctorLowest(series, n));

            functor.Calc();

            return functor;
        }

        private class FunctorLowest : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorLowest(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
            }

            public void Calc()
            {
                double value = Series[0];

                try
                {
                    for (int t = 1; t < N; t++)
                        value = Math.Min(value, Series[t]);
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                Value = value;
            }
        }
        #endregion

        #region public static ITimeSeries<double> AbsReturn(this ITimeSeries<double> series)
        /// <summary>
        /// absolute return
        /// </summary>
        public static ITimeSeries<double> AbsReturn(this ITimeSeries<double> series)
        {
            string cacheKey = string.Format("{0}", series.GetHashCode());

            var functor = Cache<FunctorAbsReturn>.GetData(
                    cacheKey,
                    () => new FunctorAbsReturn(series));

            return functor;
        }

        private class FunctorAbsReturn : ITimeSeries<double>
        {
            public ITimeSeries<double> Series;

            public FunctorAbsReturn(ITimeSeries<double> series)
            {
                Series = series;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Series[daysBack] - Series[daysBack + 1];
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        /// <summary>
        /// logarithmic return
        /// </summary>
        public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        {
            string cacheKey = string.Format("{0}", series.GetHashCode());

            var functor = Cache<FunctorLogReturn>.GetData(
                    cacheKey,
                    () => new FunctorLogReturn(series));

            return functor;
        }

        private class FunctorLogReturn : ITimeSeries<double>
        {
            public ITimeSeries<double> Series;

            public FunctorLogReturn(ITimeSeries<double> series)
            {
                Series = series;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Math.Log(Series[daysBack] / Series[daysBack + 1]);
                }
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file