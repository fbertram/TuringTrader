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
        /// <returns>accumulation/ distribution index as time series</returns>
        public static ITimeSeries<double> AccumulationDistributionIndex(this Instrument series)
        {
            return IndicatorsBasic.BufferedLambda(
                (v) => v + series.Volume[0] * series.CLV()[0],
                0.0,
                new CacheId(series.GetHashCode()));
        }
        #endregion
        #region public static ITimeSeries<double> ChaikinOscillator(this Instrument series)
        /// <summary>
        /// Chaikin oscillator as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>accumulation/ distribution index as time series</returns>
        public static ITimeSeries<double> ChaikinOscillator(this Instrument series)
        {
            var adl = series.AccumulationDistributionIndex();
            return adl
                .EMA(3)
                .Subtract(adl
                    .EMA(10));
        }
        #endregion

        #region public static ITimeSeries<double> OnBalanceVolume(this Instrument series)
        /// <summary>
        /// Calculate On-Balance Volume indicator.
        /// <see href="https://en.wikipedia.org/wiki/On-balance_volume"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>OBV time series</returns>
        public static ITimeSeries<double> OnBalanceVolume(this Instrument series)
        {
            return IndicatorsBasic.BufferedLambda(
                prev => series.Close[0] > series.Close[1]
                        ? prev + series.Volume[0]
                        : prev - series.Volume[0],
                0,
                new CacheId(series.GetHashCode()));
        }
        #endregion

        #region public static ITimeSeries<double> MoneyFlowIndex(this Instrument series, int n)
        /// <summary>
        /// Calculate Money Flow Index indicator
        /// <see href="https://en.wikipedia.org/wiki/Money_flow_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">calculation period</param>
        /// <returns>MFI time series</returns>
        public static ITimeSeries<double> MoneyFlowIndex(this Instrument series, int n)
        {
            var typicalPrice = series.TypicalPrice();

            var postiveMoneyFlow = IndicatorsBasic.BufferedLambda(
                prev => typicalPrice[0] > typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0,
                new CacheId(series.GetHashCode(), n));

            var negativeMoneyFlow = IndicatorsBasic.BufferedLambda(
                prev => typicalPrice[0] < typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0,
                new CacheId(series.GetHashCode(), n));

            return postiveMoneyFlow.SMA(n)
                .Divide(
                    postiveMoneyFlow.SMA(n)
                    .Add(negativeMoneyFlow.SMA(n)))
                .Multiply(100.0);
        }
        #endregion

        // - Volume Rate of Change
    }
}

//==============================================================================
// end of file