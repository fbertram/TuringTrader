//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsRange
// Description: collection of range-based indicators
// History:     2018ix14, FUB, created
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
    public static class IndicatorsRange
    {
        #region Highest - highest value
        #region functor caching
        static List<FunctorHighest> _FunctorCacheHighest = new List<FunctorHighest>();

        public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        {
            FunctorHighest functor = null;
            foreach (FunctorHighest f in _FunctorCacheHighest)
            {
                if (f.Series == series && f.N == n)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorHighest(series, n);
                _FunctorCacheHighest.Add(functor);
            }

            functor.Calc();
            return functor;
        }
        #endregion
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
        #region Lowest - lowest value
        #region functor caching
        static List<FunctorLowest> _FunctorCacheLowest = new List<FunctorLowest>();

        public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        {
            FunctorLowest functor = null;
            foreach (FunctorLowest f in _FunctorCacheLowest)
            {
                if (f.Series == series && f.N == n)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorLowest(series, n);
                _FunctorCacheLowest.Add(functor);
            }

            functor.Calc();
            return functor;
        }
        #endregion
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
    }
}

//==============================================================================
// end of file