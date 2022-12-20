//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Volatility
// Description: Volatility indicators.
// History:     2022xi02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of volatility-related indictors.
    /// </summary>
    public static class Volatility
    {
        #region StandardDeviation
        #endregion
        #region SemiDeviation
        #endregion
        #region Volatility
        #endregion
        #region TrueRange
        public static TimeSeriesFloat TrueRange(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.TrueRange", series.Name);

            return series.Algorithm.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Algorithm.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data.Result;
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

                    return new TimeSeriesFloat(series.Algorithm, name, data);
                });
        }
        #endregion
        #region AverageTrueRange
        public static TimeSeriesFloat AverageTrueRange(this TimeSeriesAsset series, int n)
        {
            return series.TrueRange().SMA(n);
        }
        #endregion
        #region UlcerIndex
        #endregion
        #region BollingerBands
        #endregion
    }
}

//==============================================================================
// end of file

