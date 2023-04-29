//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Price
// Description: Price indicators.
// History:     2023iii27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of price indicators.
    /// </summary>
    public static class Price
    {
        #region TypicalPrice
        /// <summary>
        /// Calculate Typical Price as described here:
        /// <see href="https://en.wikipedia.org/wiki/Typical_price"/>
        /// </summary>
        /// <param name="series">input instrument</param>
        /// <returns>typical price as time series</returns>
        public static TimeSeriesFloat TypicalPrice(this TimeSeriesAsset series)
            => series.High
                .Add(series.Low)
                .Add(series.Close)
                .Div(3.0);
        #endregion
        #region CLV
        /// <summary>
        /// CLV factor as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>CLV as time series</returns>
        public static TimeSeriesFloat CLV(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.CLV", series.Name);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<double>>();

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var clv = ((src[idx].Value.Close - src[idx].Value.Low) - (src[idx].Value.High - src[idx].Value.Close))
                                    / (src[idx].Value.High - src[idx].Value.Low);

                                dst.Add(new BarType<double>(src[idx].Date, clv));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
    }
}
