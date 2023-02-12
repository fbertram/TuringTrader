//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceStooq
// Description: Data source for Stooq EOD Data.
// History:     2021v17, FUB, created (w/ input from Rafal Jonca)
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

#region libraries
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceStooq : DataSource
        {
            // TODO: remove StooqDataRow and use Bar instead
            private class StooqDataRow {
                public DateTime Date;
                public double Open;
                public double High;
                public double Low;
                public double Close;
                public long Volume;
            }

            #region internal helpers
            private static readonly string _urlTemplate = @"https://stooq.pl/q/d/l/?s={0}&d1={1:yyyy}{1:MM}{1:dd}&d2={2:yyyy}{2:MM}{2:dd}&i=d";
            private static readonly string _refererUrlTemplate = @"https://stooq.pl/q/d/?s={0}&d1={1:yyyy}{1:MM}{1:dd}&d2={2:yyyy}{2:MM}{2:dd}";
            private static readonly string _metaUrlTemplate = @"https://stooq.pl/q/d/?s={0}&c=0";

            private static object _lockCache = new object();

            private string convertSymbol(string ticker)
            {
                return ticker.ToLowerInvariant();
            }

            private string getMeta()
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
                string metaCache = Path.Combine(cachePath, "stooq_meta");

                bool writeToDisk = false;
                string rawMeta = null;

                bool validMeta()
                {
                    if (rawMeta == null)
                        return false;

                    if (rawMeta.Length < 10)
                        return false;

                    return true;
                }

                //--- 1) try to read cached json from disk
                if (File.Exists(metaCache))
                {
                    using (StreamReader mc = new StreamReader(File.Open(metaCache, FileMode.Open)))
                        rawMeta = mc.ReadToEnd();
                }

                //--- 2) if failed, try to retrieve from web
                if (!validMeta())
                {
                    Output.WriteLine("DataSourceStooq: retrieving meta for {0}", Info[DataSourceParam.nickName]);

                    string url = string.Format(
                        _metaUrlTemplate,
                        convertSymbol(Info[DataSourceParam.symbolStooq]));

                    using (var client = new WebClient())
                        rawMeta = client.DownloadString(url);

                    writeToDisk = true;
                }

                //--- 3) if failed, return
                if (!validMeta())
                    return null;

                //--- 4) write to disk
                if (writeToDisk)
                {
                    Directory.CreateDirectory(cachePath);
                    using (StreamWriter mc = new StreamWriter(File.Open(metaCache, FileMode.Create)))
                        mc.Write(rawMeta);
                }

                return rawMeta;
            }
            private List<StooqDataRow> parsePrices(string raw)
            {
                var data = new List<StooqDataRow>();

                if (raw == null)
                    return null;

                if (raw.Length < 25)
                    return null;

                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(raw)))
                using (StreamReader reader = new StreamReader(ms))
                {
                    string header = reader.ReadLine(); // skip header line

                    for (string line; (line = reader.ReadLine()) != null;)
                    {
                        if (line.Length == 0)
                            continue; // to handle end of file

                        string[] items = line.Split(',');
                        DateTime date = DateTime.Parse(items[0], CultureInfo.InvariantCulture).Date + DateTime.Parse("16:00").TimeOfDay;
                        double open = double.Parse(items[1], CultureInfo.InvariantCulture);
                        double high = double.Parse(items[2], CultureInfo.InvariantCulture);
                        double low = double.Parse(items[3], CultureInfo.InvariantCulture);
                        double close = double.Parse(items[4], CultureInfo.InvariantCulture);
                        long volume = 0;
                        try {
                            if (items.Length > 5) volume = long.Parse(items[5], CultureInfo.InvariantCulture);
                        } catch { 
                            volume = 0;
                        }

                        data.Add(new StooqDataRow()
                        {
                            Date = date,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            Volume = volume
                        });
                    }

                }

                if (data.Count == 0)
                    return null;

                return data;
            }
            private List<StooqDataRow> getPrices(DateTime startTime, DateTime endTime)
            {
                string cachePath = Path.Combine(GlobalSettings.HomePath, "Cache", Info[DataSourceParam.nickName2]);
                string timeStamps = Path.Combine(cachePath, "stooq_timestamps");
                string priceCache = Path.Combine(cachePath, "stooq_prices");

                bool writeToDisk = false;
                string rawPrices = null;
                var stooqPrices = (List<StooqDataRow>)null;

                //--- 1) try to read cached json from disk
                if (File.Exists(timeStamps) && File.Exists(priceCache))
                {
                    using (BinaryReader pc = new BinaryReader(File.Open(priceCache, FileMode.Open)))
                        rawPrices = pc.ReadString();

                    using (BinaryReader ts = new BinaryReader(File.Open(timeStamps, FileMode.Open)))
                    {
                        DateTime cacheStartTime = new DateTime(ts.ReadInt64());
                        DateTime cacheEndTime = new DateTime(ts.ReadInt64());

                        if (cacheStartTime.Date <= startTime.Date && cacheEndTime.Date >= endTime.Date)
                            stooqPrices = parsePrices(rawPrices);
                    }
                }

                //--- 2) if failed, try to retrieve from web
                if (stooqPrices == null)
                {
                    // always request whole range here, to make
                    // offline behavior as pleasant as possible
                    //DateTime DATA_START = DateTime.Parse("01/01/1970", CultureInfo.InvariantCulture);
                    DateTime DATA_START = DateTime.Parse("01/01/2000", CultureInfo.InvariantCulture);
                    //DateTime DATA_START = DateTime.Now.Date - TimeSpan.FromDays(400); // get only from last year + some padding (

                    startTime = ((DateTime)_firstTime) < DATA_START
                        ? DATA_START
                        : (DateTime)_firstTime;

                    endTime = DateTime.Now.Date + TimeSpan.FromDays(1);

                    string url = string.Format(
                        _urlTemplate,
                        convertSymbol(Info[DataSourceParam.symbolStooq]),
                        startTime,
                        endTime);
                    string refererUrl = string.Format(
                        _refererUrlTemplate,
                        convertSymbol(Info[DataSourceParam.symbolStooq]),
                        startTime,
                        endTime);

                    string tmpPrices = null;
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("Referer", _refererUrlTemplate);
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0");
                        client.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                        client.Headers.Add("Accept-Language", "pl,en-US;q=0.7,en;q=0.3");
                        client.Headers.Add("Upgrade-Insecure-Requests", "1");
                        client.Headers.Add("Sec-GPC", "1");
                        tmpPrices = client.DownloadString(url);
                    }

                    stooqPrices = parsePrices(tmpPrices);

                    if (stooqPrices != null)
                    {
                        rawPrices = tmpPrices;
                        writeToDisk = true;
                    }
                    else
                    {
                        // we might have discarded the data from disk before,
                        // because the time frame wasn't what we were looking for. 
                        // however, in case we can't load from web, e.g. because 
                        // we don't have internet connectivity, it's still better 
                        // to go with what we have cached before
                        stooqPrices = parsePrices(rawPrices);
                    }
                }

                //--- 3) if failed, return
                if (stooqPrices == null)
                    return null;

                //--- 4) write to disk
                if (writeToDisk)
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

                return stooqPrices;
            }
            private DateTime _firstTime = new DateTime(1970, 1, 1);
            #endregion

            //---------- API
            #region public DataSourceStooq(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for Stooq Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceStooq(Dictionary<DataSourceParam, string> info) : base(info)
            {
                // Yahoo does not provide meta data
                // we extract them from the instrument's web page

                lock (_lockCache)
                {
                    string meta = getMeta();

                    try
                    {
                        string startString = "<title>" + info[DataSourceParam.ticker] + " - ";
                        string name = meta.Substring(meta.IndexOf(startString) + startString.Length);
                        name = name.Substring(0, name.IndexOf(" - Stooq</title>") + 1).Replace("&amp;", "&");
                        Info[DataSourceParam.name] = name;

                        // find earliest possible start date
                        string firstDay;
                        string firstMonth;
                        string firstYear;
                        Regex rg = new Regex("<input type=text value=(\\d{1,2}) name=d7 size=3 maxlength=2 id=f13>", RegexOptions.Multiline);
                        MatchCollection matchedDate = rg.Matches(meta);
                        firstDay = matchedDate[0].Groups[1].Value;
                        rg = new Regex("<input type=text value=(\\d{4}) name=d3 size=7 maxlength=4 id=f13>");
                        matchedDate = rg.Matches(meta);
                        firstYear = matchedDate[0].Groups[1].Value;

                        firstMonth = meta.Substring(meta.IndexOf("name=d7 size=3 maxlength=2 id=f13>"));
                        firstMonth = firstMonth.Substring(0, firstMonth.IndexOf("name=d3 size=7 maxlength=4 id=f13>"));
                        rg = new Regex("<option value=(\\d{1,2}) selected>", RegexOptions.Multiline);
                        matchedDate = rg.Matches(firstMonth);
                        firstMonth = matchedDate[0].Groups[1].Value;

                        _firstTime = DateTime.Parse(string.Format("{0}-{1}-{2}", firstYear, firstMonth, firstDay), CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        // failed to load/parse website
                        Output.WriteLine("{0}: failed to parse meta for {1}", GetType().Name, Info[DataSourceParam.symbolStooq]);
                        Info[DataSourceParam.name] = info[DataSourceParam.ticker];
                    }
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

                    var cacheKey = new CacheId().AddParameters(
                        Info[DataSourceParam.nickName].GetHashCode(),
                        startTime.GetHashCode(),
                        endTime.GetHashCode());

                    List<Bar> retrievalFunction()
                    {
                        DateTime t1 = DateTime.Now;
                        Output.Write(string.Format("DataSourceStooq: loading data for {0}...", Info[DataSourceParam.nickName]));

                        List<Bar> bars = new List<Bar>();

                        List<StooqDataRow> stooqData = getPrices(startTime, endTime);
                        var e = stooqData.GetEnumerator();

                        while (e.MoveNext())
                        {
                            var bar = e.Current;

                            DateTime date = bar.Date;

                            if (date >= startTime && date <= endTime)
                                bars.Add(Bar.NewOHLC(
                                    Info[DataSourceParam.ticker],
                                    date,
                                    bar.Open, bar.High, bar.Low, bar.Close,
                                    bar.Volume));
                        }

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return bars;
                    };

                    data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true); ;
                }

                catch (Exception e)
                {
                    throw new Exception(
                        string.Format("DataSourceStooq: failed to load quotes for {0}, {1}",
                            Info[DataSourceParam.nickName], e.Message));
                }

                if (data.Count == 0)
                    throw new Exception(string.Format("DataSourceStooq: no data for {0}", Info[DataSourceParam.nickName]));

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file