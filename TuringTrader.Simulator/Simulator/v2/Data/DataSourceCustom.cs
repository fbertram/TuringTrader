//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceCustom
// Description: Data source for custom data.
// History:     2023iii04, FUB, created
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

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="nickname"></param>
        /// <param name="retrieve"></param>
        /// <returns></returns>
        public static TimeSeriesAsset CustomGetAsset(Algorithm owner, string nickname, Func<List<BarType<OHLCV>>> retrieve)
        {
            var name = string.Format("CustomData({0}-{1:X})", nickname, owner.GetHashCode());

            return owner.ObjectCache.Fetch(
                name,
                () =>
                {
#if true
                    // NOTE: custom data are currently not put into the
                    //       data cache. consequently, they are not shared
                    //       across multiple instances, e.g., while running
                    //       the optimizer

                    var data = Task.Run(() =>
                    {
                        var bars = retrieve(); // TODO: do we need to pass the nickname in here?
                        var meta = new TimeSeriesAsset.MetaType
                        {
                            Ticker = nickname,
                            Description = string.Format("Custom Data '{0}'", nickname),
                        };

                        return (object)Tuple.Create(
                            _resampleToTradingCalendar(owner, bars),
                            meta);
                    });
#else
                    // NOTE: this is how the code for the other data sources looks

                    var data = owner.DataCache.Fetch(
                        nickname,
                        () => Task.Run(() =>
                        {
                            var data2 = _loadAsset(owner, nickname);

                            return (object)Tuple.Create(
                                _resampleToTradingCalendar(owner, data2.Item1),
                                data2.Item2);
                        }));
#endif
                    return new TimeSeriesAsset(
                        owner, name,
                        data,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item1,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item2);
                });
        }
    }
}

//==============================================================================
// end of file
