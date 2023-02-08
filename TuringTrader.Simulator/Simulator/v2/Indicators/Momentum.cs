//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Momentum
// Description: Momentum indicators.
// History:     2022xi02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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
        #endregion
        #region TSI
        #endregion
        #region RSI
        #endregion
        #region WilliamsPercentR
        #endregion
        #region StochasticOscillator
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
                var src = series.Data.Result;
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
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() => calcRegression()));

                    return new RegressionT(
                        new TimeSeriesFloat(
                            series.Owner, name + ".Slope",
                            Task.Run(() => { var r = data.Result; return r.Item1; })),
                        new TimeSeriesFloat(
                            series.Owner, name + ".Intercept",
                            Task.Run(() => { var r = data.Result; return r.Item2; })),
                        new TimeSeriesFloat(
                            series.Owner, name + ".R2",
                            Task.Run(() => { var r = data.Result; return r.Item3; })));
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
        #region ADX
        #endregion
    }
}

//==============================================================================
// end of file
