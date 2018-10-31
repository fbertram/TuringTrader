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
            var functor = Cache<FunctorSMA>.GetData(
                    Tuple.Create(series, n).GetHashCode(),
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
                N = Math.Max(1, n);
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
            var functor = Cache<FunctorEMA>.GetData(
                    Tuple.Create(series, n).GetHashCode(),
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
                N = Math.Max(1, n);
                _alpha = 2.0 / (N + 1.0);
            }

            public void Calc()
            {
                try
                {
                    // prevent output from becoming
                    // noisy with N == 1
                    Value = N > 1
                        ? _alpha * (Series[0] - this[0]) + this[0]
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
        #region public static ITimeSeries<double> KAMA(this ITimeSeries<double> series, int erPeriod, int fastEma, int slowEma)
        public static ITimeSeries<double> KAMA(this ITimeSeries<double> series, int erPeriod = 10, int fastEma = 2, int slowEma = 30)
        {

            var functor = Cache<FunctorKAMA>.GetData(
                    Tuple.Create(series, erPeriod, fastEma, slowEma).GetHashCode(),
                    () => new FunctorKAMA(series, erPeriod, fastEma, slowEma));

            functor.Calc();

            return functor;
        }

        private class FunctorKAMA : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int ErPeriod;
            public int FastEma;
            public int SlowEma;

            private double _scFast;
            private double _scSlow;

            public FunctorKAMA(ITimeSeries<double> series, int erPeriod, int fastEma, int slowEma)
            {
                Series = series;
                ErPeriod = erPeriod;
                FastEma = fastEma;
                SlowEma = slowEma;

                _scFast = 2.0 / (FastEma + 1.0);
                _scSlow = 2.0 / (SlowEma + 1.0);
            }

            public void Calc()
            {

                try
                {
                    double change = Math.Abs(Series[0] - Series[ErPeriod]);
                    double volatility = Enumerable.Range(0, ErPeriod)
                        .Sum(t => Math.Abs(Series[t] - Series[t + 1]));

                    double efficiencyRatio = change / Math.Max(1e-10, volatility);
                    double smoothingConstant = Math.Pow(efficiencyRatio * (_scFast - _scSlow) + _scSlow, 2);

                    Value = this[0] + smoothingConstant * (Series[0] - this[0]);
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