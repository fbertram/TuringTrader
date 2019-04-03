//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsMomentum
// Description: collection of momentum-based indicators
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        #region public static ITimeSeries<double> CCI(this Instrument series, int n = 20)
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">averaging length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>CCI time series</returns>
        public static ITimeSeries<double> CCI(this Instrument series, int n = 20,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return series
                .TypicalPrice(cacheId)
                .CCI(n, cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> CCI(this ITimeSeries<double> series, int n = 20)
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>CCI time series</returns>
        public static ITimeSeries<double> CCI(this ITimeSeries<double> series, int n = 20,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    ITimeSeries<double> delta = series
                        .Subtract(
                            series
                                .SMA(n, cacheId),
                            cacheId);

                    ITimeSeries<double> meanDeviation = delta
                        .AbsValue(cacheId)
                        .SMA(n, cacheId);

                    return delta[0] / Math.Max(1e-10, 0.015 * meanDeviation[0]);
                },
                0.5,
                cacheId);
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
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>TSI time series</returns>
        public static ITimeSeries<double> TSI(this ITimeSeries<double> series, int r = 25, int s = 13,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), r, s);

            ITimeSeries<double> momentum = series
                .Return(cacheId);

            double numerator = momentum
                .EMA(r, cacheId)
                .EMA(s, cacheId)[0];

            double denominator = momentum
                .AbsValue(cacheId)
                .EMA(r, cacheId)
                .EMA(s, cacheId)[0];

            return IndicatorsBasic.BufferedLambda(
                v => 100.0 * numerator / Math.Max(1e-10, denominator),
                0.5,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate Relative Strength Index, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Relative_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">smoothing period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>RSI time series</returns>
        public static ITimeSeries<double> RSI(this ITimeSeries<double> series, int n = 14,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            ITimeSeries<double> returns = series.Return(cacheId);

            double avgUp = returns
                .Max(0.0, cacheId)
                .EMA(n, cacheId)[0];

            double avgDown = -returns
                .Min(0.0, cacheId)
                .EMA(n, cacheId)[0];

            double rs = avgUp / Math.Max(1e-10, avgDown);

            return IndicatorsBasic.BufferedLambda(
                v => 100.0 - 100.0 / (1 + rs),
                50.0,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> WilliamsPercentR(this Instrument series, int n = 10)
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>Williams %R as time series</returns>
        public static ITimeSeries<double> WilliamsPercentR(this Instrument series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hh = series.High.Highest(n)[0];
                    double ll = series.Low.Lowest(n)[0];
                    double price = series.Close[0];
                    return -100.0 * (hh - price) / Math.Max(1e-10, hh - ll);
                },
                -50.0,
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> WilliamsPercentR(this ITimeSeries<double> series, int n = 10)
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>Williams %R as time series</returns>
        public static ITimeSeries<double> WilliamsPercentR(this ITimeSeries<double> series, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    double hh = series.Highest(n)[0];
                    double ll = series.Lowest(n)[0];
                    double price = series[0];
                    return -100.0 * (hh - price) / Math.Max(1e-10, hh - ll);
                },
                -50.0,
                cacheId);
        }
        #endregion

        #region public static StochasticOscillatorResult StochasticOscillator(this Instrument series, int n = 14)
        /// <summary>
        /// Calculate Stochastic Oscillator, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Stochastic_oscillator"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">oscillator period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>Stochastic Oscillator as time series</returns>
        public static StochasticOscillatorResult StochasticOscillator(this Instrument series, int n = 14,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var container = Cache<StochasticOscillatorResult>.GetData(
                    cacheId,
                    () => new StochasticOscillatorResult());

            double hh = series.High
                .Highest(n, cacheId)[0];

            double ll = series
                .Low.Lowest(n, cacheId)[0];

            double price = series.Close[0];

            container.PercentK = IndicatorsBasic.BufferedLambda(
                v => 100.0 * (price - ll) / Math.Max(1e-10, hh - ll),
                50.0,
                cacheId);

            container.PercentD = container.PercentK
                .SMA(3, cacheId);

            return container;
        }
        #endregion
        #region public static StochasticOscillatorResult StochasticOscillator(this ITimeSeries<double> series, int n = 14)
        /// <summary>
        /// Calculate Stochastic Oscillator, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Stochastic_oscillator"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">oscillator period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>Stochastic Oscillator as time series</returns>
        public static StochasticOscillatorResult StochasticOscillator(this ITimeSeries<double> series, int n = 14,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);


            var container = Cache<StochasticOscillatorResult>.GetData(
                    cacheId,
                    () => new StochasticOscillatorResult());

            double hh = series
                .Highest(n, cacheId)[0];

            double ll = series
                .Lowest(n, cacheId)[0];

            double price = series[0];

            container.PercentK = IndicatorsBasic.BufferedLambda(
                v => 100.0 * (price - ll) / Math.Max(1e-10, hh - ll),
                50.0,
                cacheId);

            container.PercentD = container.PercentK
                .SMA(3, cacheId);

            return container;
        }

        /// <summary>
        /// Container for Stochastic Oscillator result.
        /// </summary>
        public class StochasticOscillatorResult
        {
            /// <summary>
            /// %K
            /// </summary>
            public ITimeSeries<double> PercentK;

            /// <summary>
            /// %D (filtered %K)
            /// </summary>
            public ITimeSeries<double> PercentD;
        }
        #endregion

        #region public static ITimeSeries<double> Momentum(this ITimeSeries<double> series, int n = 21)
        /// <summary>
        /// Calculate simple momentum of time series, normalized to 1 bar.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>regression momentum as time series</returns>
        public static ITimeSeries<double> Momentum(this ITimeSeries<double> series, int n = 21,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n.GetHashCode());

#if true
            return IndicatorsBasic.BufferedLambda(
                prev => Math.Log(series[0] / series[n]) / n,
                0.0,
                cacheId);
#else
            // retired 04/02/2019
            return series
                .Divide(series
                        .Delay(n, cacheId)
                        .Max(1e-10, cacheId),
                    cacheId)
                .Log(cacheId)
                .Divide(n, cacheId);
#endif
        }
        #endregion

        #region public static _Regression LinRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate linear regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>regression parameters as time series</returns>
        public static _Regression LinRegression(this ITimeSeries<double> series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var functor = Cache<LinRegressionFunctor>.GetData(
                    cacheId,
                    () => new LinRegressionFunctor(series, n));

            functor.Calc();

            return functor;
        }

        /// <summary>
        /// Container holding regression parameters.
        /// </summary>
        public class _Regression
        {
            /// <summary>
            /// Slope as time series.
            /// </summary>
            public ITimeSeries<double> Slope = new TimeSeries<double>();
            /// <summary>
            /// Y-axis intercept as time series.
            /// </summary>
            public ITimeSeries<double> Intercept = new TimeSeries<double>();
            /// <summary>
            /// Coefficient of determination, R2, as time series.
            /// </summary>
            public ITimeSeries<double> R2 = new TimeSeries<double>();
        }
        private class LinRegressionFunctor : _Regression
        {
            private ITimeSeries<double> _series;
            private int _n;

            public LinRegressionFunctor(ITimeSeries<double> series, int n)
            {
                _series = series;
                _n = n;
            }

            public void Calc()
            {
                // simple linear regression
                // https://en.wikipedia.org/wiki/Simple_linear_regression
                // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                // a = avg(y) - b * avg(x)

                double avgX = Enumerable.Range(-_n + 1, _n)
                    .Average(x => x);
                double avgY = Enumerable.Range(-_n + 1, _n)
                    .Average(x => _series[-x]);
                double sumXx = Enumerable.Range(-_n + 1, _n)
                    .Sum(x => Math.Pow(x - avgX, 2));
                double sumXy = Enumerable.Range(-_n + 1, _n)
                    .Sum(x => (x - avgX) * (_series[-x] - avgY));
                double b = sumXy / sumXx;
                double a = avgY - b * avgX;

                // coefficient of determination
                // https://en.wikipedia.org/wiki/Coefficient_of_determination
                // f = a + b * x
                // SSreg = sum((f - avg(y))^2)
                // SSres = sum((y - f)^2)

                double regressionSumOfSquares = Enumerable.Range(-_n + 1, _n)
                    .Sum(x => Math.Pow(a + b * x - avgY, 2));
                double residualSumOfSquares = Enumerable.Range(-_n + 1, _n)
                    .Sum(x => Math.Pow(a + b * x - _series[-x], 2));
                double r2 = regressionSumOfSquares != 0.0
                    ? 1.0 - Math.Min(1.0, residualSumOfSquares / regressionSumOfSquares)
                    : 0.0;

                (Slope as TimeSeries<double>).Value = b;
                (Intercept as TimeSeries<double>).Value = a;
                (R2 as TimeSeries<double>).Value = r2;
            }
        }
        #endregion
        #region public static _Regression LogRegression(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate logarithmic regression of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars for regression</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>regression parameters as time series</returns>
        public static _Regression LogRegression(this ITimeSeries<double> series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            return series
                .Log(cacheId)
                .LinRegression(n, cacheId);
        }
        #endregion

        #region public static TimeSeries<double> ADX(this Instrument series, int n = 14)
        /// <summary>
        /// Calculate Average Directional Movement Index.
        /// <see href="https://en.wikipedia.org/wiki/Average_directional_movement_index"/>
        /// </summary>
        /// <param name="series">input OHLC time series</param>
        /// <param name="n">smoothing length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>ADX time series</returns>
        public static ITimeSeries<double> ADX(this Instrument series, int n = 14,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var upMove = Math.Max(0.0, series.High[0] - series.High[1]);
            var downMove = Math.Max(0.0, series.Low[1] - series.Low[0]);

            var plusDM = IndicatorsBasic.BufferedLambda(
                prev => upMove > downMove ? upMove : 0.0,
                0.0,
                cacheId);

            var minusDM = IndicatorsBasic.BufferedLambda(
                prev => downMove > upMove ? downMove : 0.0,
                0.0,
                cacheId);

            //var atr = series.AverageTrueRange(n);

            // +DI = 100 * Smoothed+DM / ATR
            var plusDI = plusDM
                .EMA(n, cacheId);
                //.Divide(atr)
                //.Multiply(100.0);

            // -DI = 100 * Smoothed-DM / ATR
            var minusDI = minusDM
                .EMA(n, cacheId);
                //.Divide(atr)
                //.Multiply(100.0);

            // DX = Abs(+DI - -DI) / (+DI + -DI)
            var DX = IndicatorsBasic.BufferedLambda(
                prev => 100.0 * Math.Abs(plusDI[0] - minusDI[0]) / (plusDI[0] + minusDI[0]),
                0.0,
                cacheId);

            // ADX = (13 * ADX[1] + DX) / 14
            var ADX = DX
                .EMA(n, cacheId);
                //.Multiply(100.0);

            return ADX;
        }
        #endregion
    }
}

//==============================================================================
// end of file