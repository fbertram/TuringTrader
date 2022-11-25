//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgo
// Description: Virtual data source to use data from algorithms.
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

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static List<BarType<OHLCV>> LoadFredData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            throw new NotImplementedException();
        }
        private static TimeSeriesAsset.MetaType LoadFredMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            throw new NotImplementedException();
        }
    }
}

//==============================================================================
// end of file
