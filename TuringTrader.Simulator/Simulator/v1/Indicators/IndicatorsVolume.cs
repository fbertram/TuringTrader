//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsVolume
// Description: collection of volume-based indicators
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
using System.Runtime.CompilerServices;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Indicators
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