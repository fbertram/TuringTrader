//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceCsv
// Description: Data source for CSV files.
// History:     2018ix10, FUB, created
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

#define UPDATE_DATA

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression; // requires reference to System.IO.Compression.dll
using System.Linq;
using System.Text.RegularExpressions;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        /// <summary>
        /// Data source for CSV files.
        /// </summary>
        private class DataSourceCsv : DataSource
        {
            #region internal data
            private static int _totalBarsRead = 0;
            private DateTime? _firstTime;
            private DateTime? _lastTime;
            #endregion
            #region internal helpers
            private DateTime ParseDate(string value, string mapping)
            {
                var mapping2 = Regex.Replace(mapping, "{.*:", "").Replace("}", "");
                var parsed = DateTime.ParseExact(value, mapping2, CultureInfo.InvariantCulture);
                return parsed;
            }
            private double ParseDouble(string value)
            {
                // FIXME: do we still need this? why?
                if (!value.Contains('.'))
                    value += ".0";

                try
                {
                    return double.Parse(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return 0.0;
                }
            }
            private Bar CreateBar(string line)
            {
                string[] items = (Info[DataSourceParam.ticker] + Info[DataSourceParam.delim] + line)
                    .Split(Info[DataSourceParam.delim]);

                string ticker = Info[DataSourceParam.ticker];
                DateTime date = default(DateTime);
                DateTime time = default(DateTime);

                bool hasOHLC = false;
                double open = default(double);
                double high = default(double);
                double low = default(double);
                double close = default(double);
                long volume = default(long);

                bool hasBidAsk = false;
                double bid = default(double);
                double ask = default(double);
                long bidVolume = default(long);
                long askVolume = default(long);

                DateTime optionExpiry = default(DateTime);
                double optionStrike = default(double);
                bool optionIsPut = false;

                foreach (var mapping in Info)
                {
                    try
                    {
                        // extract the relevant sub-string
                        string mappedString = string.Format(mapping.Value, items);

                        // drop leading spaces, introduced by a space after the delimiting comma
                        mappedString = mappedString.Trim(' ');

                        // assign sub-string to field
                        switch (mapping.Key)
                        {
                            //case DataSourceParam.ticker: ticker = mappedString; break;
                            case DataSourceParam.date: date = ParseDate(mappedString, mapping.Value); break;
                            case DataSourceParam.time: time = DateTime.Parse(mappedString); break;

                            case DataSourceParam.open: open = ParseDouble(mappedString); hasOHLC = true; break;
                            case DataSourceParam.high: high = ParseDouble(mappedString); hasOHLC = true; break;
                            case DataSourceParam.low: low = ParseDouble(mappedString); hasOHLC = true; break;
                            case DataSourceParam.close: close = ParseDouble(mappedString); hasOHLC = true; break;
                            case DataSourceParam.volume: volume = long.Parse(mappedString); break;

                            case DataSourceParam.bid: bid = ParseDouble(mappedString); hasBidAsk = true; break;
                            case DataSourceParam.ask: ask = ParseDouble(mappedString); hasBidAsk = true; break;
                            case DataSourceParam.bidSize: bidVolume = long.Parse(mappedString); break;
                            case DataSourceParam.askSize: askVolume = long.Parse(mappedString); break;

                            case DataSourceParam.optionExpiration: optionExpiry = ParseDate(mappedString, mapping.Value); break;
                            case DataSourceParam.optionStrike: optionStrike = ParseDouble(mappedString); break;
                            case DataSourceParam.optionRight: optionIsPut = Regex.IsMatch(mappedString, "^[pP]"); break;
                        }
                    }
                    catch
                    {
                        // there are a lot of things that can go wrong here
                        // we try to move on for as long as we can and
                        // parse at least some if not most of the fields
                        Output.WriteLine("DataSourceCsv: parsing exception: {0}, {1}, {2}={3}",
                            Info[DataSourceParam.nickName], line, mapping.Key, mapping.Value);
                    }
                }

                DateTime barTime = Info.ContainsKey(DataSourceParam.time)
                    ? date.Date + time.TimeOfDay
                    : date;

                return new Bar(
                                ticker, barTime,
                                open, high, low, close, volume, hasOHLC,
                                bid, ask, bidVolume, askVolume, hasBidAsk,
                                optionExpiry, optionStrike, optionIsPut);
            }
            private void LoadDir(List<Bar> data, string path, DateTime startTime, DateTime endTime)
            {
                DirectoryInfo d = new DirectoryInfo(path);
                FileInfo[] Files = d.GetFiles("*.*");

                // an empty directory should not hinder us from updating
                //if (Files.Count() == 0)
                //    throw new Exception(string.Format("no files to load for {0}", Info[DataSourceValue.ticker]));

                foreach (FileInfo file in Files.OrderBy(f => f.Name))
                {
                    LoadFile(data, file.FullName, startTime, endTime);
                }
            }
            private void LoadFile(List<Bar> data, string filePath, DateTime startTime, DateTime endTime)
            {
                if (Path.GetExtension(filePath).Equals(".zip"))
                {
                    try
                    {
                        using (FileStream zipFile = File.OpenRead(filePath))
                        using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                        {
                            foreach (var zippedFile in zipArchive.Entries)
                            {
                                using (Stream unzippedFile = zippedFile.Open())
                                using (StreamReader reader = new StreamReader(unzippedFile))
                                {
                                    LoadCsv(data, reader, startTime, endTime);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception(string.Format("DataSourceCSv: failed to load zipped data file {0}, {1}", filePath, e.Message));
                    }
                }
                else
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        LoadCsv(data, sr, startTime, endTime);
                    }
                }
            }
            /// <summary>
            /// Load CSV into data structure.
            /// </summary>
            /// <param name="data">destination data container</param>
            /// <param name="reader">stream reader source</param>
            /// <param name="loadStartTime">minimum time stamp</param>
            /// <param name="loadEndTime">maximum time stamp</param>
            public void LoadCsv(List<Bar> data, StreamReader reader, DateTime loadStartTime, DateTime loadEndTime)
            {
                string header = reader.ReadLine(); // skip header line

                Bar prevBar = null;
                for (string line; (line = reader.ReadLine()) != null;)
                {
                    // handle end of file gracefully
                    if (line.Length == 0)
                        continue;

                    // skip invalid bars
                    Bar bar = CreateBar(line);
                    if (bar == null)
                        continue;

                    // skip duplicate timestamps, unless we are processing options
                    if (prevBar != null && !bar.IsOption && prevBar.Time == bar.Time)
                        continue;

                    if (_firstTime == null)
                        _firstTime = bar.Time;

                    if (_lastTime == null || bar.Time > _lastTime)
                        _lastTime = bar.Time;

                    if (bar.Time < _lastTime)
                        throw new Exception("DataSourceCsv: bars out of sequence");

#if true
                    // add previous bar, if we don't have a bar at the reqested start
                    if (data.Count == 0
                    && bar.Time > loadStartTime
                    && prevBar != null)
                        data.Add(prevBar);
#endif

                    if (bar.Time >= loadStartTime
                    && bar.Time <= loadEndTime)
                        data.Add(bar);

                    prevBar = bar;
                    _totalBarsRead++;
                }
            }
            private void WriteCsv(string sourceName, IEnumerable<Bar> updateBars)
            {
                string updateFilePath = Path.Combine(Info[DataSourceParam.dataPath],
                    string.Format("{0:yyyy-MM-dd}-{1}.csv", updateBars.Select(b => b.Time).Max(), sourceName.ToLower()));

                DirectoryInfo d = new DirectoryInfo(Info[DataSourceParam.dataPath]);
                FileInfo[] files = d.GetFiles("*.*");
                if (files.Count() > 0
                && files.Select(i => i.Name).OrderByDescending(n => n).FirstOrDefault().CompareTo(updateFilePath) > 0)
                {
                    // try filename at end of the alphabet
                    updateFilePath = Path.Combine(Info[DataSourceParam.dataPath],
                        string.Format("zzz-{0:yyyy-MM-dd}-{1}.csv", updateBars.Select(b => b.Time).Max(), sourceName.ToLower()));
                }


                if (Directory.Exists(Info[DataSourceParam.dataPath])
                && !File.Exists(updateFilePath))
                {
                    using (StreamWriter writer = new StreamWriter(updateFilePath))
                    {
#if true
                        //--- find mapping
                        Dictionary<int, DataSourceParam> mapping = new Dictionary<int, DataSourceParam>();
                        for (int column = 1; column < 20; column++)
                        {
                            string map1 = string.Format("{{{0}}}", column);
                            string map2 = string.Format("{{{0}:", column);

                            DataSourceParam value = Info
                                .Where(i => i.Value.Contains(map1) || i.Value.Contains(map2))
                                .Select(i => i.Key)
                                .FirstOrDefault();

                            if (value != default(DataSourceParam))
                                mapping[column] = value;
                        }

                        //--- write header row
                        int highestColumn = mapping.Keys.Max(i => i);
                        for (int column = 1; column <= highestColumn; column++)
                        {
                            if (mapping.ContainsKey(column))
                                writer.Write("{0},", mapping[column]);
                            else
                                writer.Write(",");
                        }
                        writer.WriteLine();

                        //--- write bars
                        foreach (Bar bar in updateBars)
                        {
                            for (int column = 1; column <= highestColumn; column++)
                            {
                                if (mapping.ContainsKey(column))
                                {
                                    DataSourceParam prop = mapping[column];
                                    object value = null;
                                    switch (prop)
                                    {
                                        case DataSourceParam.date:
                                            value = bar.Time.Date;
                                            break;
                                        case DataSourceParam.time:
                                            value = bar.Time.TimeOfDay;
                                            break;

                                        case DataSourceParam.open:
                                            value = bar.Open;
                                            break;
                                        case DataSourceParam.high:
                                            value = bar.High;
                                            break;
                                        case DataSourceParam.low:
                                            value = bar.Low;
                                            break;
                                        case DataSourceParam.close:
                                            value = bar.Close;
                                            break;
                                        case DataSourceParam.volume:
                                            value = bar.Volume;
                                            break;

                                        case DataSourceParam.bid:
                                            value = bar.Bid;
                                            break;
                                        case DataSourceParam.ask:
                                            value = bar.Ask;
                                            break;
                                        case DataSourceParam.bidSize:
                                            value = bar.BidVolume;
                                            break;
                                        case DataSourceParam.askSize:
                                            value = bar.AskVolume;
                                            break;

                                        case DataSourceParam.optionExpiration:
                                            value = bar.OptionExpiry;
                                            break;
                                        case DataSourceParam.optionStrike:
                                            value = bar.OptionStrike;
                                            break;
                                        case DataSourceParam.optionRight:
                                            value = bar.OptionIsPut ? "P" : "C";
                                            break;
                                    }
                                    writer.Write(Info[prop], Enumerable.Range(1, 20).Select(i => value).ToArray());
                                }
                                writer.Write(",");
                            }
                            writer.WriteLine();
                        }
#else
                    writer.WriteLine("Date,Open,High,Low,Close,Volume");

                    foreach (Bar bar in updateBars)
                    {
                        writer.WriteLine("{0:MM/dd/yyyy},{1},{2},{3},{4},{5}",
                            bar.Time, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                    }
#endif
                    }
                }
            }
            private void UpdateData(List<Bar> data, DateTime startTime, DateTime endTime)
            {
                // we have two start times
                // - the time at which we start loading bars into memory
                // - the time at which we start writing bars to the update file
                // as our update can only append to the end, we need to make
                // sure we start our database early (e.g. 1990) when we
                // start initializing an empty data set
                DateTime loadStartTime = _lastTime != null && _lastTime >= startTime
                    ? (DateTime)_lastTime + TimeSpan.FromSeconds(1)
                    : startTime;
                DateTime updateStartTime = _lastTime != null
                    ? (DateTime)_lastTime + TimeSpan.FromSeconds(1)
                    : DateTime.Parse("01/01/1970");

                // we also have two end times
                // - the time at which we stop loading bars into memory
                // - the time at which we stop writing bars to the update file
                // we run our update for a few days more than requested (if that's possible)
                // to make sure we don't run it again, in case the same update is requested again
                DateTime loadEndTime = endTime;
                DateTime updateEndTime = DateTime.Now + TimeSpan.FromDays(4);

                // it doesn't seem to bother our update clients, if we request a time in the future
                // this also helps overcoming the issue of not requesting enough data, due to 
                // differences in time zone
                //if (updateEndTime > DateTime.Now)
                //    updateEndTime = DateTime.Now;

#if UPDATE_DATA
                DataUpdater updater = DataUpdater.New(Simulator, Info);
                if (updater != null)
                {
                    DateTime t1 = DateTime.Now;
                    Output.Write(string.Format("DataSourceCsv: updating data for {0} using {1}...", Info[DataSourceParam.nickName], updater.Name));

                    // retrieve update data
                    // we copy these to a list, to avoid evaluating this multiple times
                    IEnumerable<Bar> updateBars = updater.UpdateData(updateStartTime, updateEndTime).ToList();

                    // write a new csv file
                    if (updateBars.Count() > 0)
                        WriteCsv(updater.Name, updateBars);

                    // load the bars into memory
                    foreach (Bar bar in updateBars)
                    {
                        if (bar.Time >= loadStartTime
                        && bar.Time <= loadEndTime)
                            data.Add(bar);
                    }

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
                }
#endif
            }
            #endregion

            //---------- API
            #region public DataSourceCsv(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for CSV files.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceCsv(Dictionary<DataSourceParam, string> info) : base(info)
            {
                if (!Info.ContainsKey(DataSourceParam.dataPath))
                    throw new Exception(string.Format("DataSourceCsv: {0} missing mandatory dataPath key", info[DataSourceParam.nickName]));

                // expand relative paths, if required
                if (!Info[DataSourceParam.dataPath].Substring(1, 1).Equals(":")   // drive letter
                && !Info[DataSourceParam.dataPath].Substring(0, 1).Equals(@"\")) // absolute path
                    Info[DataSourceParam.dataPath] = Path.Combine(DataPath, Info[DataSourceParam.dataPath]);

                // dataPath should be either a file name, or a directory
                // if it's neither, try to create a directory
                if (!File.Exists(Info[DataSourceParam.dataPath])
                && !Directory.Exists(Info[DataSourceParam.dataPath]))
                    Directory.CreateDirectory(Info[DataSourceParam.dataPath]);

                // remove time field, if date format also contains time
                if (Info[DataSourceParam.date].Contains("H") || Info[DataSourceParam.date].Contains("h")
                || Info[DataSourceParam.date].Contains("m"))
                    Info.Remove(DataSourceParam.time);
            }
            #endregion
            #region override public void LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                var cacheKey = new CacheId().AddParameters(
                    Info[DataSourceParam.nickName].GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    DateTime t1 = DateTime.Now;
                    Output.Write(string.Format("DataSourceCsv: loading data for {0}...", Info[DataSourceParam.nickName]));

                    List<Bar> bars = new List<Bar>();

                    if (File.Exists(Info[DataSourceParam.dataPath]))
                        LoadFile(bars, Info[DataSourceParam.dataPath], startTime, endTime);
                    else if (Directory.Exists(Info[DataSourceParam.dataPath]))
                        LoadDir(bars, Info[DataSourceParam.dataPath], startTime, endTime);

                    // this should never happen - we create an empty directory in DataSource.New
                    else
                        throw new Exception("DataSourceCsv: data path not found");

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    if (_lastTime == null
                    || _lastTime < endTime)
                        UpdateData(bars, startTime, endTime);

                    return bars;
                };

                List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                if (data.Count == 0)
                    throw new Exception(string.Format("DataSourceCsv: no data for {0}", Info[DataSourceParam.nickName]));

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}
//==============================================================================
// end of file