//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceJoin
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

using System.Collections.Generic;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static List<BarType<OHLCV>> LoadJoinData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var symbols = info[DataSourceParam.nickName2].Split(",");

#if true
            var mostRecentSymbol = symbols[0];
            return LoadData(algo, mostRecentSymbol);
#else
            var data = new Dictionary<string, List<BarType<OHLCV>>>();

            foreach (var symbol in symbols)
                data[symbol] = LoadData(algo, symbol);

            // FIXME: implement joining magic here
#endif
        }
        private static TimeSeriesAsset.MetaType LoadJoinMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var symbols = info[DataSourceParam.nickName2].Split(",");
            var mostRecentSymbol = symbols[0];

            return LoadMeta(algo, mostRecentSymbol);
        }
    }
}

//==============================================================================
// end of file
