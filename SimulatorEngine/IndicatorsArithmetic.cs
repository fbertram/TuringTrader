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

namespace FUB_TradingSim
{
    static public class IndicatorsArithmetic
    {
        #region public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            string cacheKey = string.Format("{0}-{1}", series1.GetHashCode(), series2.GetHashCode());

            var functor = DataCache<FunctorAdd>.GetCachedData(
                    cacheKey,
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
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            string cacheKey = string.Format("{0}-{1}", series1.GetHashCode(), series2.GetHashCode());

            var functor = DataCache<FunctorSubtract>.GetCachedData(
                    cacheKey,
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
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            string cacheKey = string.Format("{0}-{1}", series1.GetHashCode(), series2.GetHashCode());

            var functor = DataCache<FunctorMultiply>.GetCachedData(
                    cacheKey,
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
    }
}

//==============================================================================
