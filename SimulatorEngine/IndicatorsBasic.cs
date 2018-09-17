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

        #region SMA - Simple Moving Average
        #region functor cache
        static List<FunctorSMA> _FunctorCacheSMA = new List<FunctorSMA>();

        public static ITimeSeries<double> SMA(this ITimeSeries<double> series, int n)
        {
            FunctorSMA sma = null;
            foreach (FunctorSMA f in _FunctorCacheSMA)
            {
                if (f.Series == series && f.N == n)
                {
                    sma = f;
                    break;
                }
            }

            if (sma == null)
            {
                sma = new FunctorSMA(series, n);
                _FunctorCacheSMA.Add(sma);
            }

            sma.Calc();
            return sma;
        }
        #endregion

        private class FunctorSMA : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorSMA(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
            }

            public void Calc()
            {
                double sum = Series[0];
                int num = 1;

                try
                {
                    for (int t = 1; t < N; t++)
                    {
                        sum += Series[t];
                        num++;
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                Value = sum / num;
            }
        }
        #endregion
        #region EMA - Exponentially Weighted Moving Average
        #region functor cache
        static List<FunctorEMA> _FunctorCacheEMA = new List<FunctorEMA>();
        /// <summary>
        /// Exponentially Weighted Moving Average
        /// </summary>
        public static ITimeSeries<double> EMA(this ITimeSeries<double> series, int n)
        {
            FunctorEMA ema = null;
            foreach (FunctorEMA e in _FunctorCacheEMA)
            {
                if (e.Series == series && e.N == n)
                {
                    ema = e;
                    break;
                }
            }

            if (ema == null)
            {
                ema = new FunctorEMA(series, n);
                _FunctorCacheEMA.Add(ema);
            }

            ema.Calc();
            return ema;
        }
        #endregion

        private class FunctorEMA : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            private double _alpha;

            public FunctorEMA(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = n;
                _alpha = 2.0 / (n + 1.0);
            }

            public void Calc()
            {
                try
                {
                    Value = _alpha * (Series[0] - this[1]) + this[0];
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    Value = Series[0];
                }
            }
        }
        #endregion

        #region AbsReturn - absolute return
        #region functor cache
        static List<FunctorAbsReturn> _FunctorCacheAbsReturn = new List<FunctorAbsReturn>();

        public static ITimeSeries<double> AbsReturn(this ITimeSeries<double> series)
        {
            FunctorAbsReturn functor = null;
            foreach (var f in _FunctorCacheAbsReturn)
            {
                if (f.Series == series)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorAbsReturn(series);
                _FunctorCacheAbsReturn.Add(functor);
            }

            return functor;
        }
        #endregion

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
        #region LogReturn - logarithmic return
        #region functor cache
        static List<FunctorLogReturn> _FunctorCacheLogReturn = new List<FunctorLogReturn>();

        public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        {
            FunctorLogReturn functor = null;
            foreach (var f in _FunctorCacheLogReturn)
            {
                if (f.Series == series)
                {
                    functor = f;
                    break;
                }
            }

            if (functor == null)
            {
                functor = new FunctorLogReturn(series);
                _FunctorCacheLogReturn.Add(functor);
            }

            return functor;
        }
        #endregion

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