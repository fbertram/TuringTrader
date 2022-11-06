//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsPrice
// Description: collection of price indicators
// History:     2018x31, FUB, created
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