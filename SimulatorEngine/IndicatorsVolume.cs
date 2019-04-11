//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsVolume
// Description: collection of volume-based indicators
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of volume-based indicators.
    /// </summary>
    public static class IndicatorsVolume
    {
        #region public static ITimeSeries<double> AccumulationDistributionIndex(this Instrument series)
        /// <summary>
        /// Accumulation/ distribution index as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>accumulation/ distribution index as time series</returns>
        public static ITimeSeries<double> AccumulationDistributionIndex(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.BufferedLambda(
                (v) => v + series.Volume[0] * series.CLV()[0],
                0.0,
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> ChaikinOscillator(this Instrument series)
        /// <summary>
        /// Chaikin oscillator as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>accumulation/ distribution index as time series</returns>
        public static ITimeSeries<double> ChaikinOscillator(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            var adl = series.AccumulationDistributionIndex(cacheId);

            return adl
                .EMA(3, cacheId)
                .Subtract(
                    adl
                        .EMA(10, cacheId),
                    cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> OnBalanceVolume(this Instrument series)
        /// <summary>
        /// Calculate On-Balance Volume indicator.
        /// <see href="https://en.wikipedia.org/wiki/On-balance_volume"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>OBV time series</returns>
        public static ITimeSeries<double> OnBalanceVolume(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.BufferedLambda(
                prev => series.Close[0] > series.Close[1]
                        ? prev + series.Volume[0]
                        : prev - series.Volume[0],
                0,
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> MoneyFlowIndex(this Instrument series, int n)
        /// <summary>
        /// Calculate Money Flow Index indicator
        /// <see href="https://en.wikipedia.org/wiki/Money_flow_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">calculation period</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>MFI time series</returns>
        public static ITimeSeries<double> MoneyFlowIndex(this Instrument series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            var typicalPrice = series.TypicalPrice();

            var postiveMoneyFlow = IndicatorsBasic.BufferedLambda(
                prev => typicalPrice[0] > typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0,
                cacheId);

            var negativeMoneyFlow = IndicatorsBasic.BufferedLambda(
                prev => typicalPrice[0] < typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0,
                cacheId);

            return postiveMoneyFlow
                .SMA(n, cacheId)
                .Divide(
                    postiveMoneyFlow
                        .SMA(n, cacheId)
                        .Add(
                            negativeMoneyFlow
                                .SMA(n, cacheId),
                            cacheId),
                    cacheId)
                .Multiply(100.0, cacheId);
        }
        #endregion

        // - Volume Rate of Change
    }
}

//==============================================================================
// end of file