//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceFred
// Description: Data source for FRED Data 
//              find FRED here: https://fred.stlouisfed.org/
//              see documentation here:
//              https://research.stlouisfed.org/docs/api/fred/
// History:     2022xi29, FUB, created
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static string _fredApiKey => "967bc3160a70e6f8a501f4e3a3516fdc";

        private static List<BarType<OHLCV>> FredLoadData(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadDataHelper<JObject>(
                algo, info,
                () =>
                {   // retrieve data from FRED
                    // NOTE: we request a static range here, to make
                    //       offline behavior as pleasant as possible
                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series/observations"
                            + "?series_id={0}"
                            + "&api_key={1}"
                            + "&file_type=json"
                            + "&observation_start={2:yyyy}-{2:MM}-{2:dd}"
                            + "&observation_end={3:yyyy}-{3:MM}-{3:dd}",
                        info[DataSourceParam.symbolFred],
                        _fredApiKey,
                        DateTime.Parse("01/01/1950", CultureInfo.InvariantCulture),
                        DateTime.Now + TimeSpan.FromDays(5));

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringData) =>
                {   // parse data and check validity
                    try
                    {
                        if (stringData == null || stringData.Length < 25)
                            return null;

                        var json = JObject.Parse(stringData);

                        if (!json["observations"].HasValues)
                            return null;

                        return json;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (jsonData) =>
                {   // extract data for TuringTrader
                    var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                    var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                    var e = ((JArray)jsonData["observations"]).GetEnumerator();

                    var bars = new List<BarType<OHLCV>>();
                    while (e.MoveNext())
                    {
                        var bar = e.Current;

                        var observationDate = DateTime.Parse((string)bar["date"], CultureInfo.InvariantCulture).Date + timeOfDay;
                        var localDate = TimeZoneInfo.ConvertTimeToUtc(observationDate, exchangeTimeZone).ToLocalTime();

                        string valueString = (string)bar["value"];

                        if (valueString == ".")
                            continue; // missing value, avoid throwing exception here

                        double value;
                        try
                        {
                            value = double.Parse(valueString, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            // when we get here, this was probably a missing value,
                            // which FRED substitutes with "."
                            // we ignore and move on, resampling will take
                            // care of the issue gracefully
                            continue;
                        }

                        bars.Add(new BarType<OHLCV>(
                            localDate,
                            new OHLCV(value, value, value, value, 0.0)));
                    }

                    return bars;
                });

        private static TimeSeriesAsset.MetaType FredLoadMeta(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadMetaHelper<JObject>(
                algo, info,
                () =>
                {   // retrieve data from FRED
                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series"
                            + "?series_id={0}"
                            + "&api_key={1}&file_type=json",
                        info[DataSourceParam.symbolFred],
                        _fredApiKey);

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringData) =>
                {   // parse data and check validity
                    try
                    {
                        if (stringData == null || stringData.Length < 10)
                            return null;

                        var json = JObject.Parse(stringData);

                        if (json["seriess"][0]["title"].Type == JTokenType.Null)
                            return null;

                        return json;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (jsonData) => new TimeSeriesAsset.MetaType
                {   // extract meta for TuringTrader
                    Ticker = (string)jsonData["seriess"][0]["id"],
                    Description = (string)jsonData["seriess"][0]["title"],
                });

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> FredGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            return Tuple.Create(
                FredLoadData(owner, info),
                FredLoadMeta(owner, info));
        }
    }
}

//==============================================================================
// end of file
