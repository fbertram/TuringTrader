//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsArithmetic
// Description: arithmetic on time series
// History:     2018ix14, FUB, created
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

#region libraries
using System;
using System.Runtime.CompilerServices;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Indicators
{
    /// <summary>
    /// Collection of indicators performing arithmetic on time series.
    /// </summary>
    static public class IndicatorsArithmetic
    {
        // NOTE: as of .Net Framework 4.6.1, extension methods can not overload operators

        #region public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate addition of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series1[t] + series2[t],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Add(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate addition of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Add(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series[t] + constValue,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate subtraction of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 - time series #2</returns>
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series1[t] - series2[t],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Subtract(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate subtraction of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series - constant value</returns>
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series[t] - constValue,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate multiplication of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series #1 * time series #2</returns>
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series1[t] * series2[t],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Multiply(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate multiplication of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series * constant value</returns>
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series[t] * constValue,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Divide(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate division of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series #1 / time series #2</returns>
        public static ITimeSeries<double> Divide(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series1[t] / (series2[t] == 0 ? 1e-25 : series2[t]),
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Divide(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate division of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series / constant value</returns>
        public static ITimeSeries<double> Divide(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series[t] / constValue,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Max(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate maximum of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Max(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Max(series1[t], series2[t]),
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Max(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate maximum of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Max(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Max(series[t], constValue),
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Min(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate minimum of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Min(this ITimeSeries<double> series1, ITimeSeries<double> series2,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series1.GetHashCode(), series2.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Min(series1[t], series2[t]),
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Min(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate minimum of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Min(this ITimeSeries<double> series, double constValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), constValue.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Min(series[t], constValue),
                cacheId);
        }
        #endregion
    }
}

//==============================================================================
