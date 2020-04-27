//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsCorrelation
// Description: collection of correlation indicators
// History:     2020iv25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
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

#region libraries
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Indicators
{
    /// <summary>
    /// Collection of correlation indicators.
    /// </summary>
    public static class IndicatorsCorrrelation
    {
        #region public static ITimeSeries<double> Correlation(this ITimeSeries<double> series, ITimeSeries<double> other, int n)
        /// <summary>
        /// Calculate Pearson Correlation Coefficient.
        /// <see href="https://en.wikipedia.org/wiki/Pearson_correlation_coefficient"/>
        /// </summary>
        /// <param name="series">this time series</param>
        /// <param name="other">other time series</param>
        /// <param name="n">number of bars</param>
        /// <param name="subSample">distance between bars</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>correlation coefficient time series</returns>
        public static ITimeSeries<double> Correlation(this ITimeSeries<double> series, ITimeSeries<double> other, int n, int subSample = 1,
        CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), other.GetHashCode(), n);

            var x = series;
            var y = other;

            double sum = n / 2.0 * (n + 1.0);
            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    var avgX = Enumerable.Range(0, n)
                        .Average(t => x[t * subSample]);
                    var avgY = Enumerable.Range(0, n)
                        .Average(t => y[t * subSample]);
                    var covXY = Enumerable.Range(0, n)
                        .Sum(t => (x[t * subSample] - avgX) * (y[t * subSample] - avgY)) / n;
                    var varX = Enumerable.Range(0, n)
                        .Sum(t => Math.Pow(x[t * subSample] - avgX, 2.0)) / n;
                    var varY = Enumerable.Range(0, n)
                        .Sum(t => Math.Pow(y[t * subSample] - avgY, 2.0)) / n;
                    var corr = covXY
                        / Math.Max(1e-99, Math.Sqrt(varX) * Math.Sqrt(varY));

                    return corr;
                },
                0.0,
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Covariance(this ITimeSeries<double> series, ITimeSeries<double> other, int n)
        /// <summary>
        /// Calculate Covariance.
        /// <see href="https://en.wikipedia.org/wiki/Covariance"/>
        /// </summary>
        /// <param name="series">this time series</param>
        /// <param name="other">other time series</param>
        /// <param name="n">number of bars</param>
        /// <param name="subSample">distance between bars</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>covariance time series</returns>
        public static ITimeSeries<double> Covariance(this ITimeSeries<double> series, ITimeSeries<double> other, int n, int subSample = 1,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), other.GetHashCode(), n);

            var x = series;
            var y = other;

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    var avgX = Enumerable.Range(0, n)
                        .Average(t => x[t * subSample]);
                    var avgY = Enumerable.Range(0, n)
                        .Average(t => y[t * subSample]);
                    var covXY = Enumerable.Range(0, n)
                        .Sum(t => (x[t * subSample] - avgX) * (y[t * subSample] - avgY))
                        / (n - 1.0);

                    return covXY;
                },
                0.0,
                cacheId);
        }
        #endregion
    }
}

//==============================================================================
// end of file