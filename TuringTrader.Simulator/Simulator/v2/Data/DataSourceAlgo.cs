//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgo
// Description: Virtual data source to use data from algorithms.
// History:     2022xi25, FUB, created
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
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> AlgoGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            var algoName = info[DataSourceParam.nickName2];
            var algoInstance = Simulator.AlgorithmLoader.InstantiateAlgorithm(algoName);

            if (algoInstance == null)
                throw new Exception(string.Format("failed to instantiate algorithm '{0}'", algoName));

            return AlgoGetAssetInstance(owner, algoInstance, info[DataSourceParam.nickName]);
        }

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> AlgoGetAssetInstance(Algorithm owner, Simulator.IAlgorithm generator, string nickname)
        {
            var tradingDays = owner.TradingCalendar.TradingDays;
            var startDate = tradingDays.First();
            var endDate = tradingDays.Last();

            var instanceV1 = (generator as Simulator.Algorithm);
            var instanceV2 = (generator as Algorithm);

            if (instanceV1 != null)
            {
                instanceV1.IsDataSource = true;
                var barsV1 = instanceV1.Run(startDate, endDate)
                    .ToList();

                var barsV2 = new List<BarType<OHLCV>>();
                foreach (var bar in barsV1)
                {
                    // v1 algorithms run in the exchange's time zone
                    var exchangeAtClose = bar.Time;
                    var localAtClose = TimeZoneInfo.ConvertTimeToUtc(exchangeAtClose, owner.TradingCalendar.ExchangeTimeZone)
                        .ToLocalTime();

                    barsV2.Add(new BarType<OHLCV>(
                        localAtClose,
                        new OHLCV(bar.Open, bar.High, bar.Low, bar.Close, bar.Volume)));
                }

                // TODO: create a fake generator instance here,
                //       which we can use to examine the asset allocation
                var generatorV2 = (Algorithm)null;

                return Tuple.Create(
                    barsV2,
                    new TimeSeriesAsset.MetaType
                    {
                        Ticker = nickname,
                        Description = generator.Name,
                        Generator = generatorV2,
                    });
            }

            if (instanceV2 != null)
            {
                // also see Algorithm.Asset(Algorithm)
                instanceV2.StartDate = startDate;
                instanceV2.EndDate = endDate;
                instanceV2.IsDataSource = true;
                instanceV2.Run();

                return Tuple.Create(
                    instanceV2.EquityCurve,
                    new TimeSeriesAsset.MetaType
                    {
                        Ticker = nickname,
                        Description = generator.Name,
                        Generator = instanceV2,
                    });
            }

            return null;
        }
    }
}

//==============================================================================
// end of file
