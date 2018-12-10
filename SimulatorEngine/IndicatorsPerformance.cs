//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsPerformance
// Description: collection of performance indicators
// History:     2018xii10, FUB, created
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
    /// Collection of performance indicators
    /// </summary>
    public static class IndicatorsPerformance
    {
        #region public static ITimeSeries<double> SharpeRatio(this ITimeSeries<double> series, ITimeSeries<double> riskFreeRate, int n)
        /// <summary>
        /// Calculate Sharpe Ratio, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Sharpe_ratio"/>
        /// </summary>
        /// <param name="series"></param>
        /// <param name="riskFreeRate"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static ITimeSeries<double> SharpeRatio(this ITimeSeries<double> series, ITimeSeries<double> riskFreeRate, int n)
        {
            var excessReturn = series.Return()
                .Subtract(riskFreeRate.Return());

            return excessReturn
                .EMA(n)
                .Divide(excessReturn
                    .FastStandardDeviation(n)
                    .Max(IndicatorsBasic.Const(1e-10)));
        }
        #endregion

        #region public static ITimeSeries<double> Drawdown(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Return current drawdown.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length of observation window</param>
        /// <returns>drawdown as time series</returns>
        public static ITimeSeries<double> Drawdown(this ITimeSeries<double> series, int n)
        {
            return IndicatorsBasic.Const(1.0)
                .Subtract(series
                    .Divide(series
                        .Highest(n)));
        }
        #endregion
        #region public static ITimeSeries<double> MaxDrawdown(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Return maximum drawdown.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length of observation window</param>
        /// <returns>maximum drawdown as time series</returns>
        public static ITimeSeries<double> MaxDrawdown(this ITimeSeries<double> series, int n)
        {
#if true
            return IndicatorsBasic.BufferedLambda(
                (p) =>
                {
                    double highestHigh = 0.0;
                    double maxDrawdown = 0.0;
                    for (int t = n - 1; t >= 0; t--)
                    {
                        highestHigh = Math.Max(highestHigh, series[t]);
                        maxDrawdown = Math.Max(maxDrawdown, 1.0 - series[t] / highestHigh);
                    }
                    return maxDrawdown;
                },
                0.0,
                Cache.UniqueId(series.GetHashCode(), n));
#else
            // NOTE: the total length of observation is 2x n
            return series
                .Drawdown(n)
                .Highest(n);
#endif
        }
        #endregion
        #region public static ITimeSeries<double> ReturnOnMaxDrawdown(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate return over maximum drawdown.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">length of observation window</param>
        /// <returns>RoMaD</returns>
        public static ITimeSeries<double> ReturnOnMaxDrawdown(this ITimeSeries<double> series, int n)
        {
            return IndicatorsBasic.BufferedLambda(
                (p) =>
                {
                    double ret = series[0] / series[n] - 1.0;
                    double dd = series.MaxDrawdown(n)[0];
                    return ret / Math.Max(1e-3, dd);
                },
                0.0,
                Cache.UniqueId(series.GetHashCode(), n));
        }
        #endregion
    }
}

//==============================================================================
// end of file