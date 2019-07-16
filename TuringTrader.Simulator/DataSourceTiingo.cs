//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceTiingo
// Description: Data source for Tiingo EOD Data.
// History:     2019v09, FUB, created
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
using System.IO;
using System.Net;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceTiingo : DataSource
        {
            #region internal helpers
            private static object _lockCache = new object();
            private string _apiToken
            {
                get
                {
                    return GlobalSettings.TiingoApiKey;
                }
            }

            private string ConvertSymbol(string ticker)
            {
                return ticker.Replace('.', '-');
            }
            private JObject GetMeta()
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
                string metaCache = Path.Combine(cachePath, "tiingo_meta");

                bool writeToDisk = false;
                string rawMeta = null;
                JObject jsonMeta = null;

                bool validMeta()
                {
                    if (rawMeta == null)
                        return false;

                    if (rawMeta.Length < 10)
                        return false;

                    if (jsonMeta["name"].Type == JTokenType.Null)
                        return false;

                    return true;
                }

                //--- 1) try to read raw json from disk
                if (File.Exists(metaCache))
                {
                    using (BinaryReader mc = new BinaryReader(File.Open(metaCache, FileMode.Open)))
                        rawMeta = mc.ReadString();

                    jsonMeta = JObject.Parse(rawMeta);
                }

                //--- 2) if failed, try to retrieve from web
                if (!validMeta())
                {
                    Output.WriteLine("DataSourceTiingo: retrieving meta for {0}", Info[DataSourceParam.nickName]);

                    string url = string.Format("https://api.tiingo.com/tiingo/daily/{0}?token={1}",
                        ConvertSymbol(Info[DataSourceParam.symbolTiingo]),
                        _apiToken);

                    using (var client = new WebClient())
                        rawMeta = client.DownloadString(url);

                    jsonMeta = JObject.Parse(rawMeta);

                    writeToDisk = true;
                }

                //--- 3) if failed, return
                if (!validMeta())
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
            private JArray GetPrices(DateTime startTime, DateTime endTime)
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
                string timeStamps = Path.Combine(cachePath, "tiingo_timestamps");
                string priceCache = Path.Combine(cachePath, "tiingo_prices");

                string rawPricesFromDisk = null;
                string rawPrices = null;
                JArray jsonPrices = null;

                bool validPrices()
                {
                    if (rawPrices == null)
                        return false;

                    if (rawPrices.Length < 25)
                        return false;

                    if (!jsonPrices.HasValues)
                        return false;

                    return true;
                }

                //--- 1) try to read raw json from disk
                if (File.Exists(timeStamps) && File.Exists(priceCache))
                {
                    using (BinaryReader pc = new BinaryReader(File.Open(priceCache, FileMode.Open)))
                        rawPricesFromDisk = pc.ReadString();

                    using (BinaryReader ts = new BinaryReader(File.Open(timeStamps, FileMode.Open)))
                    {
                        DateTime cacheStartTime = new DateTime(ts.ReadInt64());
                        DateTime cacheEndTime = new DateTime(ts.ReadInt64());

                        if (cacheStartTime.Date <= startTime.Date && cacheEndTime.Date >= endTime.Date)
                            rawPrices = rawPricesFromDisk;

                        jsonPrices = JArray.Parse(rawPrices);
                    }
                }

                //--- 2) if failed, try to retrieve from web
                if (!validPrices())
                {
#if true
                    // always request whole range here, to make
                    // offline behavior as pleasant as possible
                    DateTime DATA_START = DateTime.Parse("01/01/1970");

                    startTime = ((DateTime)FirstTime) < DATA_START
                        ? DATA_START
                        : (DateTime)FirstTime;

                    endTime = DateTime.Now.Date + TimeSpan.FromDays(1);
#else
                    startTime = startTime.Date;
                    endTime = endTime.Date + TimeSpan.FromDays(5);
#endif

                    string url = string.Format(
                        "https://api.tiingo.com/tiingo/daily/{0}/prices"
                        + "?startDate={1:yyyy}-{1:MM}-{1:dd}"
                        + "&endDate={2:yyyy}-{2:MM}-{2:dd}"
                        + "&format=json"
                        + "&resampleFreq=daily"
                        + "&token={3}",
                        ConvertSymbol(Info[DataSourceParam.symbolTiingo]),
                        startTime,
                        endTime,
                        _apiToken);

                    using (var client = new WebClient())
                        rawPrices = client.DownloadString(url);

                    jsonPrices = JArray.Parse(rawPrices);
                }

                //--- 3) if failed, try to fall back to data from disk
                // we might have discarded the data from disk before,
                // because the time frame wasn't what we were looking for. 
                // however, in case we can't load from web, e.g. because 
                // we don't have internet connectivity, it's still better 
                // to go with what we have cached before
                if (!validPrices() && rawPricesFromDisk != null)
                {
                    rawPrices = rawPricesFromDisk;
                    jsonPrices = JArray.Parse(rawPrices);
                }

                //--- 4) if failed, return
                if (!validPrices())
                    return null;

                //--- 5) write to disk
                if (rawPricesFromDisk == null)
                {
                    Directory.CreateDirectory(cachePath);
                    using (BinaryWriter pc = new BinaryWriter(File.Open(priceCache, FileMode.Create)))
                        pc.Write(rawPrices);

                    using (BinaryWriter ts = new BinaryWriter(File.Open(timeStamps, FileMode.Create)))
                    {
                        ts.Write(startTime.Ticks);
                        ts.Write(endTime.Ticks);
                    }
                }

                return jsonPrices;
            }
            #endregion

            //---------- API
            #region public DataSourceTiingo(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for Tiingo Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceTiingo(Dictionary<DataSourceParam, string> info) : base(info)
            {
                try
                {
                    lock (_lockCache)
                    {
                        JObject jsonData = GetMeta();

                        Info[DataSourceParam.name] = (string)jsonData["name"];

                        FirstTime = DateTime.Parse((string)jsonData["startDate"]);
                        LastTime = DateTime.Parse((string)jsonData["endDate"]);
                    }
                }
                catch (Exception /*e*/)
                {
                    throw new Exception(
                        string.Format("DataSourceTiingo: failed to load meta for {0}",
                            Info[DataSourceParam.nickName]));
                }
            }
            #endregion
            #region override public void LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            override public void LoadData(DateTime startTime, DateTime endTime)
            {
                try
                {
                    if (startTime < (DateTime)FirstTime)
                        startTime = (DateTime)FirstTime;

                    //if (endTime > (DateTime)LastTime)
                    //    endTime = (DateTime)LastTime;

                    var cacheKey = new CacheId(null, "", 0,
                        Info[DataSourceParam.nickName].GetHashCode(),
                        startTime.GetHashCode(),
                        endTime.GetHashCode());

                    List<Bar> retrievalFunction()
                    {
                        DateTime t1 = DateTime.Now;
                        Output.Write(string.Format("DataSourceTiingo: loading data for {0}...", Info[DataSourceParam.nickName]));

                        List<Bar> bars = new List<Bar>();

                        JArray jsonData = GetPrices(startTime, endTime);
                        var e = jsonData.GetEnumerator();

                        while (e.MoveNext())
                        {
                            var bar = e.Current;

                            DateTime date = DateTime.Parse((string)bar["date"]).Date
                                + DateTime.Parse(Info[DataSourceParam.time]).TimeOfDay;

                            double open = (double)bar["adjOpen"];
                            double high = (double)bar["adjHigh"];
                            double low = (double)bar["adjLow"];
                            double close = (double)bar["adjClose"];
                            long volume = (long)bar["adjVolume"];

                            if (date >= startTime && date <= endTime)
                                bars.Add(Bar.NewOHLC(
                                    Info[DataSourceParam.ticker],
                                    date,
                                    open, high, low, close,
                                    volume));
                        }

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return bars;
                    };

                    Data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction); ;
                }

                catch (Exception e)
                {
                    throw new Exception(
                        string.Format("DataSourceTiingo: failed to load quotes for {0}, {1}",
                            Info[DataSourceParam.nickName], e.Message));
                }

                if ((Data as List<Bar>).Count == 0)
                    throw new Exception(string.Format("DataSourceTiingo: no data for {0}", Info[DataSourceParam.nickName]));

            }
            #endregion
        }
    }
}

//==============================================================================
// end of file