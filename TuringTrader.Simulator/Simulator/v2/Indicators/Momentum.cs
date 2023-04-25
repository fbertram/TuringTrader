//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Momentum
// Description: Momentum indicators.
// History:     2022xi02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TuringTrader.SimulatorV2.Indicators;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of momentum indicators.
    /// </summary>
    public static class Momentum
    {
        #region LinMomentum
        #endregion
        #region LogMomentum
        #endregion

        #region CCI
        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">averaging length</param>
        /// <returns>CCI time series</returns>
        public static TimeSeriesFloat CCI(this TimeSeriesAsset series, int n = 20)
            => series
                .TypicalPrice()
                .CCI(n);

        /// <summary>
        /// Calculate Commodity Channel Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Commodity_channel_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">averaging length</param>
        /// <returns>CCI time series</returns>
        public static TimeSeriesFloat CCI(this TimeSeriesFloat series, int n = 20)
        {
            var name = string.Format("{0}.CCI({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    /*var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            ...
                            return dst;
                        }));
                        return new TimeSeriesFloat(series.Ownder, name, data);*/

                    var delta = series
                        .Sub(series.SMA(n));

                    var meanDeviation = delta
                        .AbsValue()
                        .SMA(n);

                    return delta
                        .Div(meanDeviation.Mul(0.015).Max(1e-10));
                });
        }
        #endregion
        #region TSI
        /// <summary>
        /// Calculate True Strength Index of input time series, as described here:
        /// <see href="https://en.wikipedia.org/wiki/True_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="r">smoothing period for momentum</param>
        /// <param name="s">smoothing period for smoothed momentum</param>
        /// <returns>TSI time series</returns>
        public static TimeSeriesFloat TSI(this TimeSeriesFloat series, int r = 25, int s = 13)
        {
            var name = string.Format("{0}.TSI({1},{2})", series.Name, r, s);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    /*var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            ...
                            return dst;
                        }));
                        return new TimeSeriesFloat(series.Ownder, name, data);*/

                    var momentum = series.AbsReturn();
                    var numerator = momentum.EMA(r).EMA(s);
                    var denominator = momentum.AbsValue().EMA(r).EMA(s);

                    return numerator.Mul(100.0)
                        .Div(denominator.Max(1e-10));
                });
        }
        #endregion
        #region RSI
        /// <summary>
        /// Calculate Relative Strength Index, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Relative_strength_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">smoothing period</param>
        /// <returns>RSI time series</returns>
        public static TimeSeriesFloat RSI(this TimeSeriesFloat series, int n = 14)
        {
            var returns = series.AbsReturn();

            var avgUp = returns
                .Max(0.0)
                .EMA(n);

            var avgDown = returns
                .Min(0.0)
                .EMA(n)
                .Mul(-1.0);

            var rs = avgUp
                .Div(avgDown.Max(1e-10));

            return series.Const(100.0)
                .Sub(series.Const(100.0).Div(rs.Add(1.0)));
        }
        #endregion
        #region WilliamsPercentR
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">period</param>
        /// <returns>Williams %R as time series</returns>
        public static TimeSeriesFloat WilliamsPercentR(this TimeSeriesAsset series, int n = 10)
        {
            var hh = series.High.Highest(n);
            var ll = series.Low.Lowest(n);
            var price = series.Close;

            return hh.Sub(price)
                .Div(hh.Sub(ll).Max(1e-10))
                .Mul(-100.0);
        }
        /// <summary>
        /// Calculate Williams %R, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Williams_%25R"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">period</param>
        /// <returns>Williams %R as time series</returns>
        public static TimeSeriesFloat WilliamsPercentR(this TimeSeriesFloat series, int n = 10)
        {
            var hh = series.Highest(n);
            var ll = series.Lowest(n);
            var price = series;

            return hh.Sub(price)
                .Div(hh.Sub(ll).Max(1e-10))
                .Mul(-100.0);
        }
        #endregion
        #region StochasticOscillator
        /// <summary>
        /// Container type for result of StochasticOscillator
        /// </summary>
        public class StochasticOscillatorT
        {
            /// <summary>
            /// %K line
            /// </summary>
            public readonly TimeSeriesFloat PercentK;
            /// <summary>
            /// %D line
            /// </summary>
            public readonly TimeSeriesFloat PercentD;

            /// <summary>
            /// Create new container
            /// </summary>
            /// <param name="percentK">%K line</param>
            /// <param name="percentD">%D line</param>
            public StochasticOscillatorT(TimeSeriesFloat percentK, TimeSeriesFloat percentD)
            {
                PercentK = percentK;
                PercentD = percentD;
            }
        }
        /// <summary>
        /// Calculate Stochastic Oscillator, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Stochastic_oscillator"/>
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">oscillator period</param>
        /// <returns>Stochastic Oscillator as time series</returns>
        public static StochasticOscillatorT StochasticOscillator(this TimeSeriesAsset series, int n = 14)
        {
            var name = string.Format("{0}.StochasticOscillator({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    /*var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            ...
                            return dst;
                        }));
                        return new TimeSeriesFloat(series.Ownder, name, data);*/

                    var hh = series.High.Highest(n);
                    var ll = series.Low.Lowest(n);
                    var price = series.Close;

                    var percentK = price.Sub(ll)
                        .Div(hh.Sub(ll).Max(1e-10))
                        .Mul(100.0);

                    var percentD = percentK.SMA(3);

                    return new StochasticOscillatorT(percentK, percentD);
                });
        }
        /// <summary>
        /// Calculate Stochastic Oscillator, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Stochastic_oscillator"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">oscillator period</param>
        /// <returns>Stochastic Oscillator as time series</returns>
        public static StochasticOscillatorT StochasticOscillator(this TimeSeriesFloat series, int n = 14)
        {
            var name = string.Format("{0}.StochasticOscillator({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    /*var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();
                            ...
                            return dst;
                        }));
                        return new TimeSeriesFloat(series.Ownder, name, data);*/

                    var hh = series.Highest(n);
                    var ll = series.Lowest(n);
                    var price = series;

                    var percentK = price.Sub(ll)
                        .Div(hh.Sub(ll).Max(1e-10))
                        .Mul(100.0);

                    var percentD = percentK.SMA(3);

                    return new StochasticOscillatorT(percentK, percentD);
                });
        }
        #endregion
        #region LinRegression
        /// <summary>
        /// Container for regression results.
        /// </summary>
        public class RegressionT
        {
            /// <summary>
            /// Regression slope.
            /// </summary>
            public readonly TimeSeriesFloat Slope;
            /// <summary>
            /// Regression intercept at time offset zero.
            /// </summary>
            public readonly TimeSeriesFloat Intercept;
            /// <summary>
            /// Regression coefficient of determination.
            /// </summary>
            public readonly TimeSeriesFloat R2;

            /// <summary>
            /// Create new regression result object.
            /// </summary>
            /// <param name="slope"></param>
            /// <param name="intercept"></param>
            /// <param name="r2"></param>
            public RegressionT(TimeSeriesFloat slope, TimeSeriesFloat intercept, TimeSeriesFloat r2)
            {
                Slope = slope;
                Intercept = intercept;
                R2 = r2;
            }
        }

        /// <summary>
        /// Calculate linear regression.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">observation window</param>
        /// <returns>lin regression time series</returns>
        public static RegressionT LinRegression(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.LinRegression({1})", series.Name, n);

            Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>> calcRegression()
            {
                var src = series.Data;
                var slope = new List<BarType<double>>();
                var intercept = new List<BarType<double>>();
                var r2 = new List<BarType<double>>();

                var window = new Queue<double>();
                for (var i = 0; i < n; i++)
                    window.Enqueue(src[0].Value);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    window.Enqueue(src[idx].Value);
                    window.Dequeue();

                    // simple linear regression
                    // https://en.wikipedia.org/wiki/Simple_linear_regression
                    // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                    // a = avg(y) - b * avg(x)

                    double avgX = Enumerable.Range(-n + 1, n)
                        .Average(x => x);
                    double avgY = Enumerable.Range(-n + 1, n)
                        .Average(x => src[Math.Max(0, idx + x)].Value);
                    double sumXx = Enumerable.Range(-n + 1, n)
                        .Sum(x => Math.Pow(x - avgX, 2));
                    double sumXy = Enumerable.Range(-n + 1, n)
                        .Sum(x => (x - avgX) * (src[Math.Max(0, idx + x)].Value - avgY));
                    double b = sumXy / sumXx;
                    double a = avgY - b * avgX;

                    // coefficient of determination
                    // https://en.wikipedia.org/wiki/Coefficient_of_determination
                    // f = a + b * x
                    // SStot = sum((y - avg(y))^2)
                    // SSreg = sum((f - avg(y))^2)
                    // SSres = sum((y - f)^2)
                    // R2 = 1 - SSres / SStot
                    //    = SSreg / SStot

                    double totalSumOfSquares = Enumerable.Range(-n + 1, n)
                        .Sum(x => Math.Pow(src[Math.Max(0, idx + x)].Value - avgY, 2));
                    double regressionSumOfSquares = Enumerable.Range(-n + 1, n)
                        .Sum(x => Math.Pow(a + b * x - avgY, 2));
                    //double residualSumOfSquares = Enumerable.Range(-_n + 1, _n)
                    //    .Sum(x => Math.Pow(a + b * x - _series[-x], 2));

                    // NOTE: this is debatable. we are returning r2 = 0.0, 
                    //       when it is actually NaN
                    double rr = totalSumOfSquares != 0.0
                        //? 1.0 - residualSumOfSquares / totalSumOfSquares
                        ? regressionSumOfSquares / totalSumOfSquares
                        : 0.0;

                    slope.Add(new BarType<double>(src[idx].Date, b));
                    intercept.Add(new BarType<double>(src[idx].Date, a));
                    r2.Add(new BarType<double>(src[idx].Date, rr));
                }

                return Tuple.Create(slope, intercept, r2);
            }

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var retrieve = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() => (object)calcRegression()));

                    return new RegressionT(
                        new TimeSeriesFloat(
                            series.Owner, name + ".Slope",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item1),
                        new TimeSeriesFloat(
                            series.Owner, name + ".Intercept",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item2),
                        new TimeSeriesFloat(
                            series.Owner, name + ".R2",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item3));
                });
        }
        #endregion
        #region LogRegression
        /// <summary>
        /// Calculate logarithmic regression.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">observation period</param>
        /// <returns>log regression time series</returns>
        public static RegressionT LogRegression(this TimeSeriesFloat series, int n)
        {
            return series.Log().LinRegression(n);
        }
        #endregion
        #region Regression
        /// <summary>
        /// Calculate regression against independent time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="indep">independent time series</param>
        /// <param name="n">observation window</param>
        /// <returns>regression time series</returns>
        public static RegressionT Regression(this TimeSeriesFloat series, TimeSeriesFloat indep, int n)
        {
            var name = string.Format("{0}.Regression({1},{2})", series.Name, indep.Name, n);

            Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>> calcRegression()
            {
                var src = series.Data;
                var slope = new List<BarType<double>>();
                var intercept = new List<BarType<double>>();
                var r2 = new List<BarType<double>>();

                var windowX = new List<double>();
                var windowY = new List<double>();

                for (var i = 0; i < n; i++)
                {
                    windowX.Insert(0, indep[src[0].Date]);
                    windowY.Insert(0, src[0].Value);
                }

                for (int idx = 0; idx < src.Count; idx++)
                {
                    windowX.Insert(0, indep[src[idx].Date]);
                    windowX.RemoveAt(n - 1);

                    windowY.Insert(0, src[idx].Value);
                    windowY.RemoveAt(n - 1);

                    // simple linear regression
                    // https://en.wikipedia.org/wiki/Simple_linear_regression
                    // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                    // a = avg(y) - b * avg(x)

                    double avgX = windowX.Average();
                    double avgY = windowY.Average();
                    double sumXx = windowX.Sum(v => v * v);
                    double sumXy = Enumerable.Range(0, n)
                        .Sum(i => (windowX[i] - avgX) * (windowY[i] - avgY));
                    double b = sumXy / Math.Max(1e-99, sumXx);
                    double a = avgY - b * avgX;

                    slope.Add(new BarType<double>(src[idx].Date, b));
                    intercept.Add(new BarType<double>(src[idx].Date, a));

#if true
                    // coefficient of determination
                    // https://en.wikipedia.org/wiki/Coefficient_of_determination
                    // f = a + b * x
                    // SStot = sum((y - avg(y))^2)
                    // SSreg = sum((f - avg(y))^2)
                    // SSres = sum((y - f)^2)
                    // R2 = 1 - SSres / SStot
                    //    = SSreg / SStot

                    double totalSumOfSquares = Enumerable.Range(0, n)
                        .Sum(i => Math.Pow(windowY[i] - avgY, 2));
                    double regressionSumOfSquares = Enumerable.Range(0, n)
                        .Sum(i => Math.Pow(a + b * windowX[i] - avgY, 2));
                    double residualSumOfSquares = Enumerable.Range(0, n)
                        .Sum(i => Math.Pow(windowY[i] - a - b * windowX[i], 2));

                    // NOTE: this is debatable. we are returning r2 = 0.0, 
                    //       when it is actually NaN
                    double rr = totalSumOfSquares != 0.0
                        //? 1.0 - residualSumOfSquares / totalSumOfSquares
                        ? regressionSumOfSquares / totalSumOfSquares
                        : 0.0;

                    r2.Add(new BarType<double>(src[idx].Date, rr));
#endif
                }

                return Tuple.Create(slope, intercept, r2);
            }

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var retrieve = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() => (object)calcRegression()));

                    return new RegressionT(
                        new TimeSeriesFloat(
                            series.Owner, name + ".Slope",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item1),
                        new TimeSeriesFloat(
                            series.Owner, name + ".Intercept",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item2),
                        new TimeSeriesFloat(
                            series.Owner, name + ".R2",
                            retrieve,
                            (retrieve) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)retrieve).Item3));
                });
        }
        #endregion
        #region ADX
        /// <summary>
        /// Calculate Average Directional Movement Index.
        /// <see href="https://en.wikipedia.org/wiki/Average_directional_movement_index"/>
        /// </summary>
        /// <param name="series">input OHLC time series</param>
        /// <param name="n">smoothing length</param>
        /// <returns>ADX time series</returns>
        public static TimeSeriesFloat ADX(this TimeSeriesAsset series, int n = 14)
        {
            var name = string.Format("{0}.ADX({1})", series, n);

            var upMove = series.High.Sub(series.High.Delay(1)).Max(0.0);
            var downMove = series.Low.Delay(1).Sub(series.Low).Max(0.0);

            var plusDM = series.Owner.Lambda(
                name + ".+DM",
                () => upMove[0] > downMove[0] ? upMove[0] : 0.0);

            var minusDM = series.Owner.Lambda(
                name + ".-DM",
                () => downMove[0] > upMove[0] ? downMove[0] : 0.0);

            var plusDI = plusDM.EMA(n);
            var minusDI = minusDM.EMA(n);

            var DX = plusDI.Sub(minusDI)
                .Div(plusDI.Add(minusDI).Max(1e-99))
                .AbsValue()
                .Mul(100.0);

            return DX.EMA(n);
        }
        #endregion
    }
}

//==============================================================================
// end of file
