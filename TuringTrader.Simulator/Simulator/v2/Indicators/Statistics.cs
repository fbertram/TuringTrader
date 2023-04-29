//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Statistics
// Description: Statistical indicators.
// History:     2023iii31, FUB, created
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
using TuringTrader.Indicators;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of statistical indicators
    /// </summary>
    public static class Statistics
    {
        #region Variance
        /// <summary>
        /// Calculate historical variance deviation.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat Variance(this TimeSeriesFloat series, int n = 10)
        {
            var name = string.Format("{0}.Variance({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                // see https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance

                                var sum = Enumerable.Range(0, n)
                                    .Sum(t => src[Math.Max(0, idx - t)].Value);
                                var sum2 = Enumerable.Range(0, n)
                                    .Sum(t => Math.Pow(src[Math.Max(0, idx - t)].Value, 2.0));
                                var var = Math.Max(0.0, (sum2 - sum * sum / n) / (n - 1));

                                dst.Add(new BarType<double>(
                                    src[idx].Date, var));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region StandardDeviation
        /// <summary>
        /// Calculate historical standard deviation.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>standard deviation time series</returns>
        public static TimeSeriesFloat StandardDeviation(this TimeSeriesFloat series, int n = 10)
            => series.Variance(n).Sqrt();
        #endregion
        #region Correlation
        /// <summary>
        /// Calculate Pearson Correlation Coefficient.
        /// <see href="https://en.wikipedia.org/wiki/Pearson_correlation_coefficient"/>
        /// </summary>
        /// <param name="series">this time series</param>
        /// <param name="other">other time series</param>
        /// <param name="n">number of bars</param>
        /// <returns>correlation coefficient time series</returns>
        public static TimeSeriesFloat Correlation(this TimeSeriesFloat series, TimeSeriesFloat other, int n)
        {
            var name = string.Format("{0}.Correlation({1},{2})", series.Name, other.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var x = series.Data;
                            var y = other.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < x.Count; idx++)
                            {
                                var avgX = Enumerable.Range(0, n)
                                    .Average(t => x[Math.Max(0, idx - t)].Value);
                                var avgY = Enumerable.Range(0, n)
                                    .Average(t => y[Math.Max(0, idx - t)].Value);
                                var covXY = Enumerable.Range(0, n)
                                    .Sum(t => (x[Math.Max(0, idx - t)].Value - avgX)
                                        * (y[Math.Max(0, idx - t)].Value - avgY)) / n;
                                var varX = Enumerable.Range(0, n)
                                    .Sum(t => Math.Pow(
                                        x[Math.Max(0, idx - t)].Value - avgX, 2.0)) / n;
                                var varY = Enumerable.Range(0, n)
                                    .Sum(t => Math.Pow(
                                        y[Math.Max(0, idx - t)].Value - avgY, 2.0)) / n;
                                var corr = covXY
                                    / Math.Max(1e-99, Math.Sqrt(varX) * Math.Sqrt(varY));

                                dst.Add(new BarType<double>(x[idx].Date, corr));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Covariance
        /// <summary>
        /// Calculate Covariance.
        /// <see href="https://en.wikipedia.org/wiki/Covariance"/>
        /// </summary>
        /// <param name="series">this time series</param>
        /// <param name="other">other time series</param>
        /// <param name="n">number of bars</param>
        /// <returns>covariance time series</returns>
        public static TimeSeriesFloat Covariance(this TimeSeriesFloat series, TimeSeriesFloat other, int n)
        {
            var name = string.Format("{0}.Covariance({1},{2})", series.Name, other.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var x = series.Data;
                            var y = other.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < x.Count; idx++)
                            {
                                var avgX = Enumerable.Range(0, n)
                                    .Average(t => x[Math.Max(0, idx - t)].Value);
                                var avgY = Enumerable.Range(0, n)
                                    .Average(t => y[Math.Max(0, idx - t)].Value);
                                var covXY = Enumerable.Range(0, n)
                                    .Sum(t => (x[Math.Max(0, idx - t)].Value - avgX)
                                    * (y[Math.Max(0, idx - t)].Value - avgY))
                                    / (n - 1.0);

                                dst.Add(new BarType<double>(x[idx].Date, covXY));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region ZScore
        /// <summary>
        /// Calculate z-score of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="period">period for statistics</param>
        /// <returns>z-score time series</returns>
        public static TimeSeriesFloat ZScore(this TimeSeriesFloat series, int period)
            => series
                .Sub(series.SMA(period))
                .Div(series.StandardDeviation(period).Max(1e-99));
        #endregion
    }
}

//==============================================================================
// end of file
