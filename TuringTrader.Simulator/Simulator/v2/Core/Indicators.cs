//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators
// Description: Dummy indicators for API development.
// History:     2022x26, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// Collection of indicators
    /// </summary>
    public static class Indicators
    {
        /// <summary>
        /// Exponential Moving Average.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">filter length</param>
        /// <returns>EMA series</returns>
        public static TimeSeriesFloat EMA(this TimeSeriesFloat series, int n)
        {
            Dictionary<DateTime, double> calcIndicator()
            {
                // NOTE: this is executed in a new task.
                // Because the input series might not be calculated yet,
                // we first wait until the result is available.
                var src = series.Data.Result;
                var timestamps = src.Keys.OrderBy(ts => ts);

                var dst = new Dictionary<DateTime, double>();
                var ema = src[timestamps.First()];
                var alpha = 2.0 / (1.0 + n);

                foreach (var timestamp in src.Keys)
                {
                    ema += alpha * (src[timestamp] - ema);
                    dst[timestamp] = ema;
                }

                return dst;
            }

            var cacheId = string.Format("{0}.EMA({1})", series.CacheId, n);
            return new TimeSeriesFloat(
                series.Algo,
                cacheId,
                series.Algo.Cache(cacheId, calcIndicator));
        }
    }
}

//==============================================================================
// end of file
