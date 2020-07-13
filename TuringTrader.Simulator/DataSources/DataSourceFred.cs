//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceFred
// Description: Data source for FRED Data 
//              find FRED here: https://fred.stlouisfed.org/
//              see documentation here:
//              https://research.stlouisfed.org/docs/api/fred/
// History:     2019v15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

#region libraries
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceFred : DataSource
        {
            #region internal helpers
            private static object _lockCache = new object();
            private string _apiKey
            {
                // this API key is registered with FRED 
                // by Bertram Solutions for use with TuringTrader
                get => "967bc3160a70e6f8a501f4e3a3516fdc";
            }

            private JObject parseMeta(string raw)
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
            private JObject getMeta()
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
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
                    Output.WriteLine("DataSourceFred: retrieving meta for {0}", Info[DataSourceParam.nickName]);

                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series"
                            + "?series_id={0}"
                            + "&api_key={1}&file_type=json",
                        Info[DataSourceParam.symbolFred],
                        _apiKey);

                    using (var client = new WebClient())
                        rawMeta = client.DownloadString(url);

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
            private JObject parseData(string raw)
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
            private JObject getData(DateTime startTime, DateTime endTime)
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
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

                        if (cacheStartTime.Date <= startTime.Date && cacheEndTime.Date >= endTime.Date)
                            jsonData = parseData(rawData);
                    }
                }

                //--- 2) if failed, try to retrieve from web
                if (jsonData == null)
                {
#if true
                    // always request whole range here, to make
                    // offline behavior as pleasant as possible
                    DateTime DATA_START = DateTime.Parse("01/01/1970", CultureInfo.InvariantCulture);

                    startTime = ((DateTime)_firstTime) < DATA_START
                        ? DATA_START
                        : (DateTime)_firstTime;

                    endTime = DateTime.Now.Date + TimeSpan.FromDays(1);
#else
                    startTime = startTime.Date;
                    endTime = endTime.Date + TimeSpan.FromDays(5);
#endif

                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series/observations"
                            + "?series_id={0}"
                            + "&api_key={1}"
                            + "&file_type=json"
                            + "&observation_start={2:yyyy}-{2:MM}-{2:dd}"
                            + "&observation_end={3:yyyy}-{3:MM}-{3:dd}",
                        Info[DataSourceParam.symbolFred],
                        _apiKey,
                        startTime,
                        endTime);

                    string tmpData = null;
                    using (var client = new WebClient())
                        tmpData = client.DownloadString(url);

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
                        // to go with what we have cached before
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
                        ts.Write(startTime.Ticks);
                        ts.Write(endTime.Ticks);
                    }
                }

                return jsonData;
            }
            private DateTime? _firstTime;
            private DateTime? _lastTime;
            #endregion

            //---------- API
            #region public DataSourceFred(Dictionary<DataSourceParam, string> info) : base(info)
            /// <summary>
            /// Create and initialize new data source for FRED Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceFred(Dictionary<DataSourceParam, string> info) : base(info)
            {
                try
                {
                    lock (_lockCache)
                    {
                        JObject jsonData = getMeta();

                        Info[DataSourceParam.name] = (string)jsonData["seriess"][0]["title"];

                        _firstTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_start"], CultureInfo.InvariantCulture);
                        _lastTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_end"], CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception /*e*/)
                {
                    throw new Exception(
                        string.Format("DataSourceFred: failed to load meta for {0}",
                            Info[DataSourceParam.nickName]));
                }
            }
            #endregion
            #region public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                List<Bar> data = new List<Bar>();

                try
                {
                    if (startTime < (DateTime)_firstTime)
                        startTime = (DateTime)_firstTime;

                    //if (endTime > (DateTime)LastTime)
                    //    endTime = (DateTime)LastTime;

                    var cacheKey = new CacheId(null, "", 0,
                        Info[DataSourceParam.nickName].GetHashCode(),
                        startTime.GetHashCode(),
                        endTime.GetHashCode());

                    List<Bar> retrievalFunction()
                    {
                        DateTime t1 = DateTime.Now;
                        Output.Write(string.Format("DataSourceFred: loading data for {0}...", Info[DataSourceParam.nickName]));

                        List<Bar> rawBars = new List<Bar>();

                        JObject jsonData = getData(startTime, endTime);
                        var e = ((JArray)jsonData["observations"]).GetEnumerator();

                        while (e.MoveNext())
                        {
                            var bar = e.Current;

                            DateTime date = DateTime.Parse((string)bar["date"], CultureInfo.InvariantCulture).Date
                                + DateTime.Parse(Info[DataSourceParam.time], CultureInfo.InvariantCulture).TimeOfDay;

                            string valueString = (string)bar["value"];

                            if (valueString == ".")
                                continue; // missing value, avoid throwing exception here

                            double value;
                            try
                            {
                                value = double.Parse(valueString);
                            }
                            catch
                            {
                                // when we get here, this was probably a missing value,
                                // which FRED substitutes with "."
                                // we ignore and move on, AlignWithMarket will take
                                // care of the issue gracefully
                                continue;
                            }

                            rawBars.Add(Bar.NewOHLC(
                                Info[DataSourceParam.ticker],
                                date,
                                value, value, value, value,
                                0));
                        }

                        List<Bar> alignedBars = DataSourceHelper.AlignWithMarket(rawBars, startTime, endTime);

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return alignedBars;
                    };

                    data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                    // FIXME: this is far from ideal. We want to make sure that retired
                    //        series are not extended indefinitely
                    _lastTime = data.FindLast(b => true).Time;
                }

                catch (Exception e)
                {
                    throw new Exception(
                        string.Format("DataSourceFred: failed to load quotes for {0}, {1}",
                            Info[DataSourceParam.nickName], e.Message));
                }

                if (data.Count == 0)
                    throw new Exception(string.Format("DataSourceFred: no data for {0}", Info[DataSourceParam.nickName]));

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file