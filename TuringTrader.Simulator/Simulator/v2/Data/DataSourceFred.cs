//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceFred
// Description: Data source for FRED Data 
//              find FRED here: https://fred.stlouisfed.org/
//              see documentation here:
//              https://research.stlouisfed.org/docs/api/fred/
// History:     2022xi29, FUB, created
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region internal helpers
        private static object _lockGetMeta = new object();
        private static object _lockGetData = new object();
        private static object _lockCache = new object();
        private static string _apiKey
        {
            // this API key is registered with FRED 
            // by Bertram Solutions for use with TuringTrader
            get => "967bc3160a70e6f8a501f4e3a3516fdc";
        }

        private static JObject parseMeta(string raw)
        {
            if (raw == null)
                return null;

            if (raw.Length < 10)
                return null;

            JObject json = null;
            try
            {
                json = JObject.Parse(raw);
            }
            catch
            {
                return null;
            }

            //if (jsonMeta["seriess"].Type == JTokenType.Null)
            //    return false;

            //if (jsonMeta["seriess"][0].Type == JTokenType.Null)
            //    return false;

            if (json["seriess"][0]["title"].Type == JTokenType.Null)
                return null;

            return json;
        }
        private static JObject getMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            lock (_lockGetMeta)
            {
                string cachePath = Path.Combine(Simulator.GlobalSettings.HomePath, "Cache", info[DataSourceParam.nickName2]);
                string metaCache = Path.Combine(cachePath, "fred_meta");

                bool writeToDisk = false;
                string rawMeta = null;
                JObject jsonMeta = null;

                //--- 1) try to read raw json from disk
                if (File.Exists(metaCache))
                {
                    using (BinaryReader mc = new BinaryReader(File.Open(metaCache, FileMode.Open)))
                        rawMeta = mc.ReadString();

                    jsonMeta = parseMeta(rawMeta);
                }

                //--- 2) if failed, try to retrieve from web
                if (jsonMeta == null)
                {
                    //Output.WriteLine("DataSourceFred: retrieving meta for {0}", Info[DataSourceParam.nickName]);

                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series"
                            + "?series_id={0}"
                            + "&api_key={1}&file_type=json",
                        info[DataSourceParam.symbolFred],
                        _apiKey);

                    using (var client = new HttpClient())
                        rawMeta = client.GetStringAsync(url).Result;

                    jsonMeta = parseMeta(rawMeta);
                    writeToDisk = true;
                }

                //--- 3) if failed, return
                if (jsonMeta == null)
                    return null;

                //--- 4) write to disk
                if (writeToDisk)
                {
                    Directory.CreateDirectory(cachePath);
                    using (BinaryWriter mc = new BinaryWriter(File.Open(metaCache, FileMode.Create)))
                        mc.Write(rawMeta);
                }

                return jsonMeta;
            }
        }
        private static JObject parseData(string raw)
        {
            if (raw == null)
                return null;

            if (raw.Length < 25)
                return null;

            JObject json = null;
            try
            {
                json = JObject.Parse(raw);
            }
            catch
            {
                return null;
            }

            if (!json["observations"].HasValues)
                return null;

            return json;
        }
        private static JObject getData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var tradingDays = algo.TradingCalendar.TradingDays;
            var startDate = tradingDays.First();
            var endDate = tradingDays.Last();

            lock (_lockGetData)
            {
                string cachePath = Path.Combine(Simulator.GlobalSettings.HomePath, "Cache", info[DataSourceParam.nickName2]);
                string timeStamps = Path.Combine(cachePath, "fred_timestamps");
                string dataCache = Path.Combine(cachePath, "fred_data");

                bool writeToDisk = false;
                string rawData = null;
                JObject jsonData = null;

                //--- 1) try to read raw json from disk
                if (File.Exists(timeStamps) && File.Exists(dataCache))
                {
                    using (BinaryReader pc = new BinaryReader(File.Open(dataCache, FileMode.Open)))
                        rawData = pc.ReadString();

                    using (BinaryReader ts = new BinaryReader(File.Open(timeStamps, FileMode.Open)))
                    {
                        DateTime cacheStartTime = new DateTime(ts.ReadInt64());
                        DateTime cacheEndTime = new DateTime(ts.ReadInt64());

                        if (cacheStartTime.Date <= startDate.Date && cacheEndTime.Date >= endDate.Date)
                            jsonData = parseData(rawData);
                    }
                }

                //--- 2) if failed, try to retrieve from web
                if (jsonData == null)
                {
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
                        _apiKey,
                        DateTime.Parse("01/01/1950", CultureInfo.InvariantCulture),
                        DateTime.Now + TimeSpan.FromDays(5));

                    string tmpData = null;
                    using (var client = new HttpClient())
                        tmpData = client.GetStringAsync(url).Result;

                    jsonData = parseData(tmpData);

                    if (jsonData != null)
                    {
                        rawData = tmpData;
                        writeToDisk = true;
                    }
                    else
                    {
                        // we might have discarded the data from disk before,
                        // because the time frame wasn't what we were looking for. 
                        // however, in case we can't load from web, e.g. because 
                        // we don't have internet connectivity, it's still better 
                        // to go with what we have in the cache
                        jsonData = parseData(rawData);
                    }
                }

                //--- 3) if failed, return
                if (jsonData == null)
                    return null;

                //--- 4) write to disk
                if (writeToDisk)
                {
                    Directory.CreateDirectory(cachePath);
                    using (BinaryWriter pc = new BinaryWriter(File.Open(dataCache, FileMode.Create)))
                        pc.Write(rawData);

                    using (BinaryWriter ts = new BinaryWriter(File.Open(timeStamps, FileMode.Create)))
                    {
                        ts.Write(startDate.Ticks);
                        ts.Write(endDate.Ticks);
                    }
                }

                return jsonData;
            }
        }
        #endregion



        private static List<BarType<OHLCV>> LoadFredData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            try
            {
                lock (_lockCache)
                {
                    var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                    var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                    var jsonData = getData(algo, info);
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
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("FRED: failed to load data for {0}",
                        info[DataSourceParam.nickName2]));
            }
        }
        private static TimeSeriesAsset.MetaType LoadFredMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            try
            {
                lock (_lockCache)
                {
                    var jsonData = getMeta(algo, info);

                    return new TimeSeriesAsset.MetaType
                    {
                        Ticker = (string)jsonData["seriess"][0]["id"],
                        Description = (string)jsonData["seriess"][0]["title"],
                    };

                    //_firstTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_start"], CultureInfo.InvariantCulture);
                    //_lastTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_end"], CultureInfo.InvariantCulture);
                }
            }
            catch (Exception /*e*/)
            {
                throw new Exception(
                    string.Format("FRED: failed to load meta for {0}",
                        info[DataSourceParam.nickName2]));
            }
        }
    }
}

//==============================================================================
// end of file
