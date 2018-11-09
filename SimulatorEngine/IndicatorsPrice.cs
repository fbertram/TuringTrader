//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsPrice
// Description: collection of price indicators
// History:     2018x31, FUB, created
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
    /// Collection of price indicators.
    /// </summary>
    public static class IndicatorsPrice
    {
        #region public static ITimeSeries<double> TypicalPrice(this Instrument series)
        /// <summary>
        /// Calculate Typical Price as described here:
        /// <see href="https://en.wikipedia.org/wiki/Typical_price"/>
        /// </summary>
        /// <param name="series">input instrument</param>
        /// <returns>typical price as time series</returns>
        public static ITimeSeries<double> TypicalPrice(this Instrument series)
        {
            return IndicatorsBasic.Lambda(
                (t) => (series.High[t] + series.Low[t] + series.Close[t]) / 3.0,
                series.GetHashCode());
        }
        #endregion
        #region public static ITimeSeries<double> CLV(this Instrument series)
        /// <summary>
        /// CLV factor as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>CLV as time series</returns>
        public static ITimeSeries<double> CLV(this Instrument series)
        {
            return IndicatorsBasic.Lambda(
                (t) => ((series.Close[t] - series.Low[t]) - (series.High[t] - series.Close[t])) 
                    / (series.High[t] - series.Low[t]),
                series.GetHashCode());
        }
        #endregion
    }
}

//==============================================================================
// end of file