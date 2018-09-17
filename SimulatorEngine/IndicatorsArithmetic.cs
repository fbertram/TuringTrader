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
        #region Add
        #region functor caching
        private static List<FunctorAdd> _functorCacheAdd = new List<FunctorAdd>();

        public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            FunctorAdd functor = null;
            foreach (FunctorAdd f in _functorCacheAdd)
            {
                if (f.Series1 == series1 && f.Series2 == series2)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorAdd(series1, series2);
                _functorCacheAdd.Add(functor);
            }

            return functor;
        }
        #endregion
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
        #region Subtract
        #region functor caching
        private static List<FunctorSubtract> _functorCacheSub = new List<FunctorSubtract>();

        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            FunctorSubtract functor = null;
            foreach (FunctorSubtract f in _functorCacheSub)
            {
                if (f.Series1 == series1 && f.Series2 == series2)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorSubtract(series1, series2);
                _functorCacheSub.Add(functor);
            }

            return functor;
        }
        #endregion
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
        #region Multiply
        #region functor caching
        private static List<FunctorMultiply> _functorCacheMul = new List<FunctorMultiply>();

        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            FunctorMultiply functor = null;
            foreach (FunctorMultiply f in _functorCacheMul)
            {
                if (f.Series1 == series1 && f.Series2 == series2)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorMultiply(series1, series2);
                _functorCacheMul.Add(functor);
            }

            return functor;
        }
        #endregion
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
