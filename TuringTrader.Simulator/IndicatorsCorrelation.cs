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
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>correlation coefficient time series</returns>
        public static ITimeSeries<double> Correlation(this ITimeSeries<double> series, ITimeSeries<double> other, int n,
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
                    // determine averages
                    var sumX = 0.0;
                    var sumY = 0.0;
                    for (int t = 0; t < n; t++)
                    {
                        sumX += x[t];
                        sumY += y[t];
                    }
                    var avgX = sumX / n;
                    var avgY = sumY / n;

                    // determine factors
                    var sumDXX = 0.0;
                    var sumDYY = 0.0;
                    var sumDXY = 0.0;
                    for (int t = 0; t < n; t++)
                    {
                        sumDXX += (x[t] - avgX) * (x[t] - avgX);
                        sumDYY += (y[t] - avgY) * (y[t] - avgY);
                        sumDXY += (x[t] - avgX) * (y[t] - avgY);
                    }

                    // put it all together
                    return sumDXY / Math.Sqrt(sumDXX) / Math.Sqrt(sumDYY);
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
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>covariance time series</returns>
        public static ITimeSeries<double> Covariance(this ITimeSeries<double> series, ITimeSeries<double> other, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), other.GetHashCode(), n);

            var x = series;
            var y = other;

            return IndicatorsBasic.BufferedLambda(
                (v) =>
                {
                    // determine averages
                    var sumX = 0.0;
                    var sumY = 0.0;
                    for (int t = 0; t < n; t++)
                    {
                        sumX += x[t];
                        sumY += y[t];
                    }
                    var avgX = sumX / n;
                    var avgY = sumY / n;

                    // determine factors
                    var sumDXY = 0.0;
                    for (int t = 0; t < n; t++)
                    {
                        sumDXY += (x[t] - avgX) * (y[t] - avgY);
                    }

                    return sumDXY / n;
                },
                0.0,
                cacheId);
        }
        #endregion
    }
}

//==============================================================================
// end of file