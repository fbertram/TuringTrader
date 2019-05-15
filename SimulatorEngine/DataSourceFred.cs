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
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
        private class DataSourceFred : DataSource
        {
            #region internal helpers
            private string _apiKey
            {
                get
                {
                    return "967bc3160a70e6f8a501f4e3a3516fdc";
                }
            }

            private JObject GetSeries()
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceValue.nickName2]);
                string metaCache = Path.Combine(cachePath, "fred_meta");

                bool writeToDisk = false;
                string rawMeta = null;
                JObject jsonMeta = null;

                bool validMeta()
                {
                    if (rawMeta == null)
                        return false;

                    if (rawMeta.Length < 10)
                        return false;

                    //if (jsonMeta["seriess"].Type == JTokenType.Null)
                    //    return false;

                    //if (jsonMeta["seriess"][0].Type == JTokenType.Null)
                    //    return false;

                    if (jsonMeta["seriess"][0]["title"].Type == JTokenType.Null)
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
                    Output.WriteLine("DataSourceFred: retrieving meta for {0}", Info[DataSourceValue.nickName]);

                    string url = string.Format(
                        "https://api.stlouisfed.org/fred/series"
                            + "?series_id={0}"
                            + "&api_key={1}&file_type=json",
                        Info[DataSourceValue.symbolFred],
                        _apiKey);

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
            private JObject GetData(DateTime startTime, DateTime endTime)
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceValue.nickName2]);
                string timeStamps = Path.Combine(cachePath, "fred_timestamps");
                string dataCache = Path.Combine(cachePath, "fred_data");

                string rawDataFromDisk = null;
                string rawData = null;
                JObject jsonData = null;

                bool validData()
                {
                    if (rawData == null)
                        return false;

                    if (rawData.Length < 25)
                        return false;

                    if (!jsonData["observations"].HasValues)
                        return false;

                    return true;
                }

                //--- 1) try to read raw json from disk
                if (File.Exists(timeStamps) && File.Exists(dataCache))
                {
                    using (BinaryReader pc = new BinaryReader(File.Open(dataCache, FileMode.Open)))
                        rawDataFromDisk = pc.ReadString();

                    using (BinaryReader ts = new BinaryReader(File.Open(timeStamps, FileMode.Open)))
                    {
                        DateTime cacheStartTime = new DateTime(ts.ReadInt64());
                        DateTime cacheEndTime = new DateTime(ts.ReadInt64());

                        if (cacheStartTime.Date <= startTime.Date && cacheEndTime.Date >= endTime.Date)
                            rawData = rawDataFromDisk;

                        jsonData = JObject.Parse(rawData);
                    }
                }

                //--- 2) if failed, try to retrieve from web
                if (!validData())
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
                        "https://api.stlouisfed.org/fred/series/observations"
                            + "?series_id={0}"
                            + "&api_key={1}"
                            + "&file_type=json"
                            + "&observation_start={2:yyyy}-{2:MM}-{2:dd}"
                            + "&observation_end={3:yyyy}-{3:MM}-{3:dd}",
                        Info[DataSourceValue.symbolFred],
                        _apiKey,
                        startTime,
                        endTime);

                    using (var client = new WebClient())
                        rawData = client.DownloadString(url);

                    jsonData = JObject.Parse(rawData);
                }

                //--- 3) if failed, try to fall back to data from disk
                // we might have discarded the data from disk before,
                // because the time frame wasn't what we were looking for. 
                // however, in case we can't load from web, e.g. because 
                // we don't have internet connectivity, it's still better 
                // to go with what we have cached before
                if (!validData() && rawDataFromDisk != null)
                {
                    rawData = rawDataFromDisk;
                    jsonData = JObject.Parse(rawData);
                }

                //--- 4) if failed, return
                if (!validData())
                    return null;

                //--- 5) write to disk
                if (rawDataFromDisk == null)
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
            #endregion

            //---------- API
            #region public DataSourceTiingo(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for FRED Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceFred(Dictionary<DataSourceValue, string> info) : base(info)
            {
                try
                {
                    JObject jsonData = GetSeries();

                    Info[DataSourceValue.name] = (string)jsonData["seriess"][0]["title"];

                    FirstTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_start"]);
                    LastTime = DateTime.Parse((string)jsonData["seriess"][0]["observation_end"]);
                }
                catch (Exception /*e*/)
                {
                    throw new Exception(
                        string.Format("DataSourceFred: failed to load meta for {0}",
                            Info[DataSourceValue.nickName]));
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
                        Info[DataSourceValue.nickName].GetHashCode(),
                        startTime.GetHashCode(),
                        endTime.GetHashCode());

                    List<Bar> retrievalFunction()
                    {
                        DateTime t1 = DateTime.Now;
                        Output.Write(string.Format("DataSourceFred: loading data for {0}...", Info[DataSourceValue.nickName]));

                        List<Bar> rawBars = new List<Bar>();

                        JObject jsonData = GetData(startTime, endTime);
                        var e = ((JArray)jsonData["observations"]).GetEnumerator();

                        while (e.MoveNext())
                        {
                            var bar = e.Current;

                            DateTime date = DateTime.Parse((string)bar["date"]).Date
                                + DateTime.Parse(Info[DataSourceValue.time]).TimeOfDay;

                            double open = (double)bar["value"];
                            double high = (double)bar["value"];
                            double low = (double)bar["value"];
                            double close = (double)bar["value"];
                            long volume = 0;

                            rawBars.Add(Bar.NewOHLC(
                                Info[DataSourceValue.ticker],
                                date,
                                open, high, low, close,
                                volume));
                        }

                        List<Bar> alignedBars = DataSourceHelper.AlignWithMarket(rawBars, startTime, endTime);

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return alignedBars;
                    };

                    Data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction); ;
                }

                catch (Exception e)
                {
                    throw new Exception(
                        string.Format("DataSourceFred: failed to load quotes for {0}, {1}",
                            Info[DataSourceValue.nickName], e.Message));
                }

                if ((Data as List<Bar>).Count == 0)
                    throw new Exception(string.Format("DataSourceFred: no data for {0}", Info[DataSourceValue.nickName]));

            }
            #endregion
        }
    }
}

//==============================================================================
// end of file