﻿//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Data/DataSourceV1
// Description: Bridge between v2 engine and v1 data sources.
// History:     2022x27, FUB, created
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

// FIXME: this module is temporary. It is coded under the assumption that
// all incoming quotes are in the New York timezone. This may fail, if using
// quotes from other markets, or other time zones.

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    internal class DataSourceV1
    {
        private static TimeZoneInfo exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); // New York, USA

        public static TimeSeriesAsset LoadAsset(Algorithm algo, string name)
        {
            var data = algo.Cache(name, () =>
            {
                var ds = TuringTrader.Simulator.DataSource.New(name);
                var tradingDays = algo.TradingCalendar.TradingDays;
                var firstDate = tradingDays.First();
                var lastDate = tradingDays.Last();

                var v1Data = ds.LoadData(firstDate, lastDate)
                    .Select(b => new
                    {
                        Date = TimeZoneInfo.ConvertTimeToUtc(b.Time, exchangeTimeZone).ToLocalTime(),
                        Open = b.Open,
                        High = b.High,
                        Low = b.Low,
                        Close = b.Close,
                        Volume = b.Volume,
                    })
                    .ToList();
                var v1Idx = 0;

                var data = new List<BarType<OHLCV>>();
                foreach (var tradingDay in tradingDays)
                {
                    if (v1Data.Count > 0)
                    {
                        while (v1Idx < v1Data.Count - 1 && v1Data[v1Idx + 1].Date <= tradingDay)
                            v1Idx++;
                        var v1Bar = v1Data[v1Idx];

                        data.Add(new BarType<OHLCV>(tradingDay,
                            new OHLCV(v1Bar.Open, v1Bar.High, v1Bar.Low, v1Bar.Close, v1Bar.Volume)));
                    }
                    else
                    {
                        // FIXME: not sure if we really need this
                        data.Add(new BarType<OHLCV>(tradingDay, new OHLCV(0, 0, 0, 0, 0)));
                    }
                }

                return data;
            });

            var meta = algo.Cache(name + ".Meta", () =>
            {
                var ds = TuringTrader.Simulator.DataSource.New(name);
                return (object)new TimeSeriesAsset.MetaType
                {
                    Ticker = ds.Info[Simulator.DataSourceParam.ticker],
                    Description = ds.Info[Simulator.DataSourceParam.name],
                };
            });

            return new TimeSeriesAsset(
                algo,
                name,
                data,
                meta);
        }

        public static HashSet<string> GetConstituents(Algorithm algo, string name)
        {
            if (Simulator.GlobalSettings.DefaultDataFeed.ToLower().Contains("norgate"))
            {
                var v1Universe = algo.Cache(string.Format("Universe({0})", name), () => Simulator.Universe.New(name)).Result;
                var exchangeTime = TimeZoneInfo.ConvertTime(algo.SimDate, exchangeTimeZone);
                return v1Universe.Constituents
                    .Where(c => v1Universe.IsConstituent(c, exchangeTime))
                    .ToHashSet();
            }
            else
            {
                throw new Exception(string.Format("data source {0} does not support universes",
                    Simulator.GlobalSettings.DefaultDataFeed));
            }
        }
    }
}

//==============================================================================
// end of file