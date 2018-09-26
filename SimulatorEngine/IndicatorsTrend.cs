//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsTrend
// Description: collection of trend-based indicators
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
    public static class IndicatorsTrend
    {
        #region public static ITimeSeries<double> SMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// simple moving average over past number of bars
        /// </summary>
        public static ITimeSeries<double> SMA(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = Cache<FunctorSMA>.GetData(
                    cacheKey,
                    () => new FunctorSMA(series, n));

            functor.Calc();

            return functor;
        }

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
        #region public static ITimeSeries<double> EMA(this ITimeSeries<double> series, int n)
        /// <summary>
        /// expnentially weighted moving average over past number of bars
        /// </summary>
        public static ITimeSeries<double> EMA(this ITimeSeries<double> series, int n)
        {
            string cacheKey = string.Format("{0}-{1}", series.GetHashCode(), n);

            var functor = Cache<FunctorEMA>.GetData(
                    cacheKey,
                    () => new FunctorEMA(series, n));

            functor.Calc();

            return functor;
        }

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
                    // prevent output from becoming
                    // noisy with N == 1
                    Value = N > 1
                        ? _alpha * (Series[0] - this[1]) + this[0]
                        : Series[0];
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    Value = Series[0];
                }
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file