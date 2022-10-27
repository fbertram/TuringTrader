//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        V1DataInterface
// Description: Bridge between v2 engine and v1 data sources.
// History:     2022x27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Enterprises LLC
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
using TuringTrader.Simulator.v2;

namespace TuringTrader.Simulator.Simulator.v2
{
    internal class V1DataInterface
    {
        public static List<BarType<OHLCV>> LoadAsset(string name, ITradingCalendar calendar)
        {
#if false
            Thread.Sleep(2000); // simulate slow load

            var data = new List<BarType<OHLCV>>();

            int baseValue = 0;
            foreach (var tradingDay in calendar.TradingDays)
            {
                data.Add(new BarType<OHLCV>(tradingDay, new OHLCV(baseValue + 0.1, baseValue + 0.2, baseValue + 0.3, baseValue + 0.4, baseValue * 1000)));
                baseValue++;
            }

            return data;
#else
            var ds = TuringTrader.Simulator.DataSource.New(name);
            var tradingDays = calendar.TradingDays;
            var firstDate = tradingDays.First();
            var lastDate = tradingDays.Last();

            TimeZoneInfo exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); // New York, USA

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
                while (v1Idx < v1Data.Count - 1 && v1Data[v1Idx + 1].Date <= tradingDay)
                    v1Idx++;
                var v1Bar = v1Data[v1Idx];

                data.Add(new BarType<OHLCV>(tradingDay,
                    new OHLCV(v1Bar.Open, v1Bar.High, v1Bar.Low, v1Bar.Close, v1Bar.Volume)));
            }

            return data;
#endif
        }
    }
}

//==============================================================================
// end of file
