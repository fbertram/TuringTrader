//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceYahoo
// Description: Data source for Yahoo EOD Data.
// History:     2019vi02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#if true

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
        private class DataSourceYahoo : DataSource
        {
            #region internal helpers
            private static readonly DateTime _epochOrigin 
                = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            private static DateTime FromUnixTime(long unixTime)
            {
                return _epochOrigin.AddSeconds(unixTime);
            }

            private static long ToUnixTime(DateTime date)
            {
                return Convert.ToInt64((date - _epochOrigin).TotalSeconds);
            }

            private string ConvertSymbol(string ticker)
            {
                return ticker.Replace('.', '-');
            }

            private JArray GetPrices(DateTime startTime, DateTime endTime)
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceValue.nickName2]);
                string timeStamps = Path.Combine(cachePath, "yahoo_timestamps");
                string priceCache = Path.Combine(cachePath, "yahoo_prices");

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
                        @"http://l1-query.finance.yahoo.com/v8/finance/chart/"
                        + "{0}"
                        + "?interval=1d"
                        + "&period1={1}"
                        + "&period2={2}",
                        ConvertSymbol(Info[DataSourceValue.symbolYahoo]),
                        ToUnixTime(startTime),
                        ToUnixTime(endTime));

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
            #region public DataSourceYahoo(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for Yahoo Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceYahoo(Dictionary<DataSourceValue, string> info) : base(info)
            {
                // Yahoo does not provide meta data
                // the instrument's name will be filled with the ticker symbol
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
                        Output.Write(string.Format("DataSourceYahoo: loading data for {0}...", Info[DataSourceValue.nickName]));

                        List<Bar> bars = new List<Bar>();

                        JArray jsonData = GetPrices(startTime, endTime);
                        var e = jsonData.GetEnumerator();

                        while (e.MoveNext())
                        {
                            var bar = e.Current;

                            DateTime date = DateTime.Parse((string)bar["date"]).Date
                                + DateTime.Parse(Info[DataSourceValue.time]).TimeOfDay;

                            double open = (double)bar["adjOpen"];
                            double high = (double)bar["adjHigh"];
                            double low = (double)bar["adjLow"];
                            double close = (double)bar["adjClose"];
                            long volume = (long)bar["adjVolume"];

                            if (date >= startTime && date <= endTime)
                                bars.Add(Bar.NewOHLC(
                                    Info[DataSourceValue.ticker],
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
                        string.Format("DataSourceYahoo: failed to load quotes for {0}, {1}",
                            Info[DataSourceValue.nickName], e.Message));
                }

                if ((Data as List<Bar>).Count == 0)
                    throw new Exception(string.Format("DataSourceYahoo: no data for {0}", Info[DataSourceValue.nickName]));

            }
            #endregion
        }
    }
}
#endif

//==============================================================================
// end of file