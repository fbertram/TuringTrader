//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Volatility
// Description: Volatility indicators.
// History:     2022xi02, FUB, created
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of volatility-related indictors.
    /// </summary>
    public static class Volatility
    {
        #region StandardDeviation
        /// <summary>
        /// Calculate historical standard deviation.
        /// </summary>
        #endregion
        #region SemiDeviation
        #endregion
        #region Volatility
        /// <summary>
        /// Calculate historical volatility, based on log-returns.
        /// </summary>
        #endregion
        #region TrueRange
        /// <summary>
        /// Calculate True Range, non averaged, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
        /// </summary>
        public static TimeSeriesFloat TrueRange(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.TrueRange", series.Name);

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
                                var idxPrev = Math.Max(0, idx - 1);
                                var high = Math.Max(src[idxPrev].Value.Close, src[idx].Value.High);
                                var low = Math.Min(src[idxPrev].Value.Close, src[idx].Value.Low);
                                dst.Add(new BarType<double>(src[idx].Date, high - low));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region AverageTrueRange
        /// <summary>
        /// Calculate Averaged True Range, as described here:
        /// <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
        /// </summary>
        public static TimeSeriesFloat AverageTrueRange(this TimeSeriesAsset series, int n)
        {
            return series.TrueRange().SMA(n);
        }
        #endregion
        #region UlcerIndex
        /// <summary>
        /// Calculate Ulcer Index.
        /// </summary>
        #endregion
        #region BollingerBands
        /// <summary>
        /// Calculate Bollinger Bands, as described here:
        /// <see href="https://traderhq.com/ultimate-guide-to-bollinger-bands/"/>.
        /// </summary>
        #endregion
    }
}

//==============================================================================
// end of file

