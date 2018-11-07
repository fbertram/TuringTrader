//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsBasic
// Description: collection of basic indicators
// History:     2018ix10, FUB, created
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
    /// Collection of basic indicators.
    /// </summary>
    public static class IndicatorsBasic
    {
        #region public static ITimeSeries<double> Lambda(Func<int, double> lambda, params int[] identifier)
        /// <summary>
        /// Create time series based on lambda, with lambda being executed once for
        /// every call to the indexer method. Use this for leight-weight lambdas.
        /// </summary>
        /// <param name="lambda">lambda, taking bars back as parameter and returning time series value</param>
        /// <param name="identifier">array of integers used to identify functor</param>
        /// <returns>lambda time series</returns>
        public static ITimeSeries<double> Lambda(Func<int, double> lambda, params int[] identifier)
        {
            var functor = Cache<FunctorLambda>.GetData(
                    // TODO: try to eliminate nested calls to Cache.UniqueId
                    Cache.UniqueId(lambda.GetHashCode(), Cache.UniqueId(identifier)),
                    () => new FunctorLambda(lambda));

            return functor;
        }

        private class FunctorLambda : ITimeSeries<double>
        {
            public readonly Func<int, double> Lambda;

            public FunctorLambda(Func<int, double> lambda)
            {
                Lambda = lambda;
            }

            public double this[int barsBack]
            {
                get
                {
                    return Lambda(barsBack);
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> BufferedLambda(Func<double, double> lambda, double first, params int[] identifier)
        /// <summary>
        /// Create time series based on lambda, with lambda being executed once for
        /// every new bar.
        /// </summary>
        /// <param name="lambda">lambda, with previous value as parameter and returning current time series value</param>
        /// <param name="first">first value to return</param>
        /// <param name="identifier">array of integers used to identify functor</param>
        /// <returns>lambda time series</returns>
        public static ITimeSeries<double> BufferedLambda(Func<double, double> lambda, double first = 0.0, params int[] identifier)
        {
            var timeSeries = Cache<TimeSeries<double>>.GetData(
                Cache.UniqueId(lambda.GetHashCode(), Cache.UniqueId(identifier)),
                () => new TimeSeries<double>());

            double prevValue = timeSeries.BarsAvailable >= 1
                ? timeSeries[0]
                : first;

            timeSeries.Value = lambda(prevValue);

            return timeSeries;
        }
        #endregion

        #region public static ITimeSeries<double> Const(double constantValue)
        /// <summary>
        /// Return constant value time series.
        /// </summary>
        /// <param name="constantValue">value of time series</param>
        /// <returns>value as time series</returns>
        public static ITimeSeries<double> Const(double constantValue)
        {
            return Lambda(
                (t) => constantValue,
                constantValue.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> Delay(this ITimeSeries<double> series, int delay)
        /// <summary>
        /// Delay time series by number of bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="delay">delay length</param>
        /// <returns>delayed time series</returns>
        public static ITimeSeries<double> Delay(this ITimeSeries<double> series, int delay)
        {
            return Lambda(
                (t) => series[t + delay],
                series.GetHashCode(), delay);
        }
        #endregion

        #region public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate highest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>highest value of past n bars</returns>
        public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        {
            return BufferedLambda(
                (v) => Enumerable.Range(1, n).Max(t => series[t]),
                series[0],
                series.GetHashCode(), n);
        }
        #endregion
        #region public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate lowest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>lowest value of past n bars</returns>
        public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        {
            return BufferedLambda(
                (v) => Enumerable.Range(1, n).Min(t => series[t]),
                series[0],
                series.GetHashCode(), n);
        }
        #endregion

        #region public static ITimeSeries<double> Return(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate absolute return, from the previous to the current
        /// value of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>absolute return</returns>
        public static ITimeSeries<double> Return(this ITimeSeries<double> series)
        {
            return Lambda(
                (t) => series[t] - series[t + 1], 
                series.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate logarithmic return from the previous to the current value
        /// of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>logarithm of relative return</returns>
        public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        {
            return Lambda(
                (t) => Math.Log(series[t] / series[t + 1]),
                series.GetHashCode());
        }
        #endregion

        #region public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate absolute value of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>absolue value of input time series</returns>
        public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series)
        {
            return IndicatorsBasic.Lambda(
                (t) => Math.Abs(series[t]),
                series.GetHashCode());
        }
        #endregion
    }
}

//==============================================================================
// end of file