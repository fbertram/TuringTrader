//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceSplice
// Description: Virtual data source to splice results from multiple other sources.
// History:     2022xi25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static List<BarType<OHLCV>> LoadSpliceData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var symbols = info[DataSourceParam.nickName2].Split(",");

#if false
            var mostRecentSymbol = symbols[0];
            return _loadData(algo, mostRecentSymbol);
#else
            var tradingDays = algo.TradingCalendar.TradingDays;

            var splice = (List<BarType<OHLCV>>)null;
            for (int symIdx = 0; symIdx < symbols.Length; symIdx++)
            {
                var src = _loadData(algo, symbols[symIdx], false);

                if (splice == null)
                {
                    splice = src;
                }
                else
                {
                    var srcFiltered = src
                        .Where(b => b.Date < splice.First().Date)
                        .ToList();

                    var dataExisting = splice.First();
                    var dataSplicing = src
                        .Where(b => b.Date == splice.First().Date)
                        .FirstOrDefault();

                    if (dataSplicing == null)
                        throw new Exception(string.Format("No overlap while splicing {0}", info[DataSourceParam.nickName2]));

                    var scaleSplicing = new List<double>
                        {
                            dataExisting.Value.Open / dataSplicing.Value.Open,
                            dataExisting.Value.High / dataSplicing.Value.High,
                            dataExisting.Value.Low / dataSplicing.Value.Low,
                            dataExisting.Value.Close / dataSplicing.Value.Close,
                        }
                        .Average();

                    splice = srcFiltered
                        .Select(bar => new BarType<OHLCV>(bar.Date,
                            new OHLCV(scaleSplicing * bar.Value.Open, scaleSplicing * bar.Value.High, scaleSplicing * bar.Value.Low, scaleSplicing * bar.Value.Close, 0.0)))
                        .Concat(splice)
                        .ToList();
                }

                if (splice.First().Date <= tradingDays.First())
                    break;
            }

            return splice;
#endif
        }
        private static TimeSeriesAsset.MetaType LoadSpliceMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var symbols = info[DataSourceParam.nickName2].Split(",");
            var mostRecentSymbol = symbols[0];

            return _loadMeta(algo, mostRecentSymbol);
        }
    }
}

//==============================================================================
// end of file
