//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsArithmetic
// Description: arithmetic on time series
// History:     2018ix14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of indicators performing arithmetic on time series.
    /// </summary>
    static public class IndicatorsArithmetic
    {
        #region public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate addition of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            var functor = Cache<FunctorAdd>.GetData(
                    Cache.UniqueId(series1.GetHashCode(), series2.GetHashCode()),
                    () => new FunctorAdd(series1, series2));

            return functor;
        }

        private class FunctorAdd : ITimeSeries<double>
        {
            public ITimeSeries<double> Series1;
            public ITimeSeries<double> Series2;

            public FunctorAdd(ITimeSeries<double> series1, ITimeSeries<double> series2)
            {
                Series1 = series1;
                Series2 = series2;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Series1[daysBack] + Series2[daysBack];
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate subtraction of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 - time series #2</returns>
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            var functor = Cache<FunctorSubtract>.GetData(
                    Cache.UniqueId(series1.GetHashCode(), series2.GetHashCode()),
                    () => new FunctorSubtract(series1, series2));

            return functor;
        }

        private class FunctorSubtract : ITimeSeries<double>
        {
            public ITimeSeries<double> Series1;
            public ITimeSeries<double> Series2;

            public FunctorSubtract(ITimeSeries<double> series1, ITimeSeries<double> series2)
            {
                Series1 = series1;
                Series2 = series2;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Series1[daysBack] - Series2[daysBack];
                }
            }

        }
        #endregion
        #region public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate multiplication of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 * time series #2</returns>
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            var functor = Cache<FunctorMultiply>.GetData(
                    Cache.UniqueId(series1.GetHashCode(), series2.GetHashCode()),
                    () => new FunctorMultiply(series1, series2));

            return functor;
        }

        private class FunctorMultiply : ITimeSeries<double>
        {
            public ITimeSeries<double> Series1;
            public ITimeSeries<double> Series2;

            public FunctorMultiply(ITimeSeries<double> series1, ITimeSeries<double> series2)
            {
                Series1 = series1;
                Series2 = series2;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Series1[daysBack] * Series2[daysBack];
                }
            }

        }
        #endregion
        #region public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate absolute value of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>absolue value of input time series</returns>
        public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series)
        {
            var functor = Cache<FunctorAbsValue>.GetData(
                    Cache.UniqueId(series.GetHashCode()),
                    () => new FunctorAbsValue(series));

            return functor;
        }

        private class FunctorAbsValue : ITimeSeries<double>
        {
            public ITimeSeries<double> Series;

            public FunctorAbsValue(ITimeSeries<double> series)
            {
                Series = series;
            }

            public double this[int daysBack]
            {
                get
                {
                    return Math.Abs(Series[daysBack]);
                }
            }
        }
        #endregion
    }
}

//==============================================================================
