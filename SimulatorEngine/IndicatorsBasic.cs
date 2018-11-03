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

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of basic indicators.
    /// </summary>
    public static class IndicatorsBasic
    {
        #region public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate highest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>highest value of past n bars</returns>
        public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorHighest>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n),
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
                N = Math.Max(1, n);
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
        /// Calculate lowest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>lowest value of past n bars</returns>
        public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorLowest>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n),
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
                N = Math.Max(1, n);
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
        /// Calculate absolute return, from the previous to the current
        /// value of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>absolute return</returns>
        public static ITimeSeries<double> AbsReturn(this ITimeSeries<double> series)
        {
            var functor = Cache<FunctorAbsReturn>.GetData(
                    Cache.UniqueId(series.GetHashCode()),
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
        /// Calculate logarithmic return from the previous to the current value
        /// of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>logarithm of relative return</returns>
        public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        {
            var functor = Cache<FunctorLogReturn>.GetData(
                    Cache.UniqueId(series.GetHashCode()),
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