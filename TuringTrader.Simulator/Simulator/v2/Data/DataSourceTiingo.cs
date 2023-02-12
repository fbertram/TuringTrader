//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceTiingo
// Description: Data source for Tiingo.
// History:     2022xi29, FUB, created
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region internal helpers
        private static string _tiingoApiToken => Simulator.GlobalSettings.TiingoApiKey;
        private static string _tiingoConvertTicker(string ticker) => ticker.Replace('.', '-');
        #endregion
        private static List<BarType<OHLCV>> TiingoLoadData(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadDataHelper<JArray>(
                algo, info,
                () =>
                {   // retrieve data from Yahoo
                    string url = string.Format(
                        "https://api.tiingo.com/tiingo/daily/{0}/prices"
                        + "?startDate={1:yyyy}-{1:MM}-{1:dd}"
                        + "&endDate={2:yyyy}-{2:MM}-{2:dd}"
                        + "&format=json"
                        + "&resampleFreq=daily"
                        + "&token={3}",
                        _tiingoConvertTicker(info[DataSourceParam.symbolTiingo]),
                        DateTime.Parse("01/01/1950", CultureInfo.InvariantCulture),
                        DateTime.Now + TimeSpan.FromDays(5),
                        _tiingoApiToken);

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (raw) =>
                {   // parse data and check validity
                    try
                    {
                        if (raw == null || raw.Length < 25)
                            return null;

                        var json = JArray.Parse(raw);

                        if (!json.HasValues)
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

                    var e = jsonData.GetEnumerator();

                    var bars = new List<BarType<OHLCV>>();
                    while (e.MoveNext())
                    {
                        var bar = e.Current;

                        var exchangeClose = DateTime.Parse((string)bar["date"], CultureInfo.InvariantCulture);
                        var localClose = exchangeClose;

                        double open = (double)bar["adjOpen"];
                        double high = (double)bar["adjHigh"];
                        double low = (double)bar["adjLow"];
                        double close = (double)bar["adjClose"];
                        long volume = (long)bar["adjVolume"];

                        bars.Add(new BarType<OHLCV>(
                            localClose,
                            new OHLCV(open, high, low, close, volume)));
                    }

                    return bars;
                });

        private static TimeSeriesAsset.MetaType TiingoLoadMeta(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadMetaHelper<JObject>(
                algo, info,
                () =>
                {   // retrieve meta from Tiingo
                    string url = string.Format("https://api.tiingo.com/tiingo/daily/{0}?token={1}",
                        _tiingoConvertTicker(info[DataSourceParam.symbolTiingo]),
                        _tiingoApiToken);

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (raw) =>
                {   // parse data and check validity
                    try
                    {
                        if (raw == null || raw.Length < 10)
                            return null;

                        var json = JObject.Parse(raw);

                        if (!json.HasValues || json["name"].Type == JTokenType.Null)
                            return null;

                        // this seems to be valid meta data
                        return json;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (jsonData) => new TimeSeriesAsset.MetaType
                {   // extract meta for TuringTrader
                    Ticker = (string)jsonData["ticker"], //info[DataSourceParam.ticker],
                    Description = (string)jsonData["name"],
                });

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> TiingoGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            return Tuple.Create(
                TiingoLoadData(owner, info),
                TiingoLoadMeta(owner, info));
        }
    }
}

//==============================================================================
// end of file
