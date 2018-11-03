//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMomentum
// Description: collection of momentum-based indicators
// History:     2018ix15, FUB, created
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
    /// Collection of momentum-based indicators.
    /// </summary>
    public static class IndicatorsMomentum
    {
        // TODO:
        // - Stochastic Oscillator

        #region public static ITimeSeries<double> CCI(this Instrument series, int n = 20)
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>CCI time series</returns>
        public static ITimeSeries<double> CCI(this Instrument series, int n = 20)
        {
            var functor = Cache<FunctorCCI>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n),
                    () => new FunctorCCI(series, n));

            functor.Calc();

            return functor;
        }
        private class FunctorCCI : TimeSeries<double>
        {
            public Instrument Series;
            public int N;

            public FunctorCCI(Instrument series, int n)
            {
                Series = series;
                N = Math.Max(2, n);
            }

            public void Calc()
            {
                try
                {
                    // see https://www.tradingview.com/wiki/Commodity_Channel_Index_(CCI)
                    // and https://en.wikipedia.org/wiki/Commodity_channel_index

                    ITimeSeries<double> typicalPrices = Series.TypicalPrice();
                    //ITimeSeries<double> typicalPriceSMA = typicalPrices.SMA(N);
                    ITimeSeries<double> delta = typicalPrices.Subtract(typicalPrices.SMA(N));
                    ITimeSeries<double> meanDeviation = delta.AbsValue().SMA(N);

                    Value = delta[0] / (0.15 * meanDeviation[0]);
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    Value = 0.5;
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> TSI(this ITimeSeries<double> series, int r = 25, int s = 13)
        /// <summary>
        /// Calculate True Strength Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/True_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="r">smoothing period for momentum</param>
        /// <param name="s">smoothing period for smoothed momentum</param>
        /// <returns>TSI time series</returns>
        public static ITimeSeries<double> TSI(this ITimeSeries<double> series, int r = 25, int s = 13)
        {
            var functor = Cache<FunctorTSI>.GetData(
                    Cache.UniqueId(series.GetHashCode(), r, s),
                    () => new FunctorTSI(series, r, s));

            functor.Calc();

            return functor;
        }
        private class FunctorTSI : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public readonly int R;
            public readonly int S;

            public FunctorTSI(ITimeSeries<double> series, int r, int s)
            {
                Series = series;
                R = Math.Max(2, r);
                S = Math.Max(2, s);
            }

            public void Calc()
            {
                try
                {
                    // see https://en.wikipedia.org/wiki/True_strength_index

                    ITimeSeries<double> momentum = Series.AbsReturn();
                    double numerator = momentum.EMA(R).EMA(S)[0];
                    double denominator = momentum.AbsValue().EMA(R).EMA(S)[0];
                    Value = 100.0 * numerator / denominator;
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    Value = 0.0;
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Relative Strength Index, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Relative_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">smoothing period</param>
        /// <returns>RSI time series</returns>
        public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n = 14)
        {
            var functor = Cache<FunctorRSI>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n),
                    () => new FunctorRSI(series, n));

            functor.Calc();

            return functor;
        }
        private class FunctorRSI : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            private readonly double _alpha;
            private double _avgUp;
            private double _avgDown;

            public FunctorRSI(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = Math.Max(2, n);

                _alpha = 2.0 / (N + 1);
            }

            public void Calc()
            {
                try
                {
                    // see https://en.wikipedia.org/wiki/Relative_strength_index
                    // Wilder originally formulated the calculation of the moving average as:
                    // newval = (prevval * (period - 1) + newdata) / period.

                    double up = Math.Max(0.0, Series[0] - Series[1]);
                    double down = Math.Max(0.0, Series[1] - Series[0]);

                    _avgUp = _alpha * (up - _avgUp) + _avgUp;
                    _avgDown = _alpha * (down - _avgDown) + _avgDown;

                    double rs = _avgUp / Math.Max(1e-10, _avgDown);
                    Value = 100.0 - 100.0 / (1 + rs);
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                    // we get here when we access bars too far in the past
                    Value = 0.5;
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> LinRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate annualized linear regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <returns>annualized return as time series</returns>
        public static ITimeSeries<double> LinRegression(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorLinRegression>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n, 1),
                    () => new FunctorLinRegression(series, n, true));

            functor.Calc();

            return functor;
        }
        private class FunctorLinRegression : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            private readonly Func<double, double> _yFunc;

            public FunctorLinRegression(ITimeSeries<double> series, int n, bool linear)
            {
                Series = series;
                N = Math.Max(2, n);

                if (linear)
                    _yFunc = (y) => y;
                else
                    _yFunc = (y) => Math.Log(y);
            }

            public void Calc()
            {
                double sx = 0.0;
                double sy = 0.0;
                double sxx = 0.0;
                double sxy = 0.0;
                int n = 0;

                try
                {
                    for (int t = 0; t < N; t++)
                    {
                        double x = -t;
                        double y = _yFunc(Series[t]);
                        sx += x;
                        sy += y;
                        sxx += x * x;
                        sxy += x * y;
                        n++;
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                // simple linear regression
                // see https://en.wikipedia.org/wiki/Simple_linear_regression
                // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                //   = (n * Sxy - Sx * Sy) / (n * Sxx - Sx * Sx)
                // a = avg(y) - b * avg(x)
                //   = 1 / n * Sy - b /n * Sx
                if (n > 1)
                {
                    double b = (n * sxy - sx * sy) / (n * sxx - sx * sx);
                    double a = sy / n - b * sx / n;
                    Value = 252.0 * b;
                }
                else
                {
                    Value = 0.0;
                }

                // coefficient of determination
                // see https://en.wikipedia.org/wiki/Coefficient_of_determination
                // f = a + b * x
                // SSreg = sum((f - avg(y))^2)
                // SSres = sum((y - f)^2)
            }
        }
        #endregion
        #region public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate annualized logarithmic regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <returns>annualized return as time series</returns>
        public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorLinRegression>.GetData(
                    Cache.UniqueId(series.GetHashCode(), n, 2),
                    () => new FunctorLinRegression(series, n, false));

            functor.Calc();

            return functor;
        }
        #endregion
    }
}

//==============================================================================
// end of file