//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceSplice
// Description: Virtual data source to splice results from multiple other sources.
// History:     2022xi25, FUB, created
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
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region internal helpers
        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> _spliceGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info, bool splice)
        {
            var symbols = info[DataSourceParam.nickName2].Split(",");

#if false
            // debugging only
            return _loadAsset(owner, symbols.First(), true);
#else

            var tradingDays = owner.TradingCalendar.TradingDays;

            var data = (List<BarType<OHLCV>>)null;
            var meta = (TimeSeriesAsset.MetaType)null;
            foreach (var symbol in symbols)
            {
                if (data == null)
                {
                    var asset = _loadAsset(owner, symbol);
                    if (asset.Item1.Count > 0)
                    {
                        // FIXME: if the first symbol on the list does not 
                        //        have any bars in the requested range,
                        //        we will end up using the meta of a
                        //        lower-priority asset
                        data = _resampleToTradingCalendar(owner, asset.Item1, false);
                        meta = asset.Item2;
                    }
                }
                else
                {
                    if (data.First().Date <= tradingDays.First())
                        break;

                    var asset = _loadAsset(owner, symbol);
                    var src = _resampleToTradingCalendar(owner, asset.Item1, false);

                    var srcFiltered = src
                        .Where(b => b.Date < data.First().Date)
                        .ToList();

                    if (srcFiltered.Count == 0)
                    {
                        Output.WriteWarning("Failed to backfill {0}: no data for {1}",
                            info[DataSourceParam.nickName2], symbol);
                        continue; // try next backfill or return what we have
                    }

                    var scaleSplicing = 1.0;
                    if (splice)
                    {
                        var dataExisting = data.First();
                        var dataSplicing = src
                            .Where(b => b.Date == dataExisting.Date)
                            .FirstOrDefault();

                        if (dataSplicing == null)
                        {
                            Output.WriteWarning("Failed to backfill {0}: no overlap for {1}",
                                info[DataSourceParam.nickName2], symbol);
                            continue; // try next backfill or return what we have
                        }

                        scaleSplicing = new List<double>
                        {
                            dataExisting.Value.Open / dataSplicing.Value.Open,
                            dataExisting.Value.High / dataSplicing.Value.High,
                            dataExisting.Value.Low / dataSplicing.Value.Low,
                            dataExisting.Value.Close / dataSplicing.Value.Close,
                        }
                        .Average();
                    }

                    data = srcFiltered
                        .Select(bar => new BarType<OHLCV>(bar.Date,
                            new OHLCV(scaleSplicing * bar.Value.Open, scaleSplicing * bar.Value.High, scaleSplicing * bar.Value.Low, scaleSplicing * bar.Value.Close, 0.0)))
                        .Concat(data)
                        .ToList();
                }
            }

            return Tuple.Create(data, meta);
#endif
        }
        #endregion

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> SpliceGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
            => _spliceGetAsset(owner, info, true);

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> JoinGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
            => _spliceGetAsset(owner, info, false);
    }
}

//==============================================================================
// end of file
