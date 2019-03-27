//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsPrice
// Description: collection of price indicators
// History:     2018x31, FUB, created
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
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>typical price as time series</returns>
        public static ITimeSeries<double> TypicalPrice(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => (series.High[t] + series.Low[t] + series.Close[t]) / 3.0,
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> CLV(this Instrument series)
        /// <summary>
        /// CLV factor as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>CLV as time series</returns>
        public static ITimeSeries<double> CLV(this Instrument series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => ((series.Close[t] - series.Low[t]) - (series.High[t] - series.Close[t])) 
                    / (series.High[t] - series.Low[t]),
                cacheId);
        }
        #endregion
    }
}

//==============================================================================
// end of file