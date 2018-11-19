//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsArithmetic
// Description: arithmetic on time series
// History:     2018ix14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
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
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Add(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => series1[t] + series2[t],
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Add(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate addition of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Add(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => series[t] + constValue,
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion
        
        #region public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate subtraction of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 - time series #2</returns>
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => series1[t] - series2[t],
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Subtract(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate subtraction of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series - constant value</returns>
        public static ITimeSeries<double> Subtract(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => series[t] - constValue,
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion

        #region public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate multiplication of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 * time series #2</returns>
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => series1[t] * series2[t],
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Multiply(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate multiplication of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series * constant value</returns>
        public static ITimeSeries<double> Multiply(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => series[t] * constValue,
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion

        #region public static ITimeSeries<double> Divide(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate division of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 / time series #2</returns>
        public static ITimeSeries<double> Divide(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => series1[t] / series2[t],
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Divide(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate division of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series / constant value</returns>
        public static ITimeSeries<double> Divide(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => series[t] / constValue,
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion

        #region public static ITimeSeries<double> Max(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate maximum of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Max(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => Math.Max(series1[t], series2[t]),
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Max(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate maximum of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Max(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => Math.Max(series[t], constValue),
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion

        #region public static ITimeSeries<double> Min(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        /// <summary>
        /// Calculate minimum of two time series.
        /// </summary>
        /// <param name="series1">time series #1</param>
        /// <param name="series2">time series #2</param>
        /// <returns>time series #1 + time series #2</returns>
        public static ITimeSeries<double> Min(this ITimeSeries<double> series1, ITimeSeries<double> series2)
        {
            return IndicatorsBasic.Lambda(
                (t) => Math.Min(series1[t], series2[t]),
                series1.GetHashCode(), series2.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Min(this ITimeSeries<double> series, double constValue)
        /// <summary>
        /// Calculate minimum of time series and constant value.
        /// </summary>
        /// <param name="series">time series</param>
        /// <param name="constValue">constant value</param>
        /// <returns>time series + constant value</returns>
        public static ITimeSeries<double> Min(this ITimeSeries<double> series, double constValue)
        {
            return IndicatorsBasic.Lambda(
                (t) => Math.Min(series[t], constValue),
                series.GetHashCode(), constValue.GetHashCode());
        }
        #endregion
    }
}

//==============================================================================
