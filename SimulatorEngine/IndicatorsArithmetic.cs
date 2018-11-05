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
    }
}

//==============================================================================
