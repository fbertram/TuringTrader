//==============================================================================
// Project:     Trading Simulator
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
                series.GetHashCode());
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

        // - On-Balance Volume
        // - Volume Rate of Change
    }
}

//==============================================================================
// end of file