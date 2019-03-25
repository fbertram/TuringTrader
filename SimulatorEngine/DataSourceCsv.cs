//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceCsv
// Description: Data source for CSV files.
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression; // requires reference to System.IO.Compression.dll
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
            private List<Bar> _data;
            private IEnumerator<Bar> _barEnumerator;
            private static int _totalBarsRead = 0;
            #endregion
            #region internal helpers
            private Bar CreateBar(string line)
            {
                string[] items = (Info[DataSourceValue.ticker] + "," + line).Split(',');

                string ticker = Info[DataSourceValue.ticker];
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

                        // assign sub-string to field
                        switch (mapping.Key)
                        {
                            case DataSourceValue.date: date = DateTime.Parse(mappedString); break;
                            case DataSourceValue.time: time = DateTime.Parse(mappedString); break;

                            case DataSourceValue.open: open = double.Parse(mappedString); hasOHLC = true; break;
                            case DataSourceValue.high: high = double.Parse(mappedString); hasOHLC = true; break;
                            case DataSourceValue.low: low = double.Parse(mappedString); hasOHLC = true; break;
                            case DataSourceValue.close: close = double.Parse(mappedString); hasOHLC = true; break;
                            case DataSourceValue.volume: volume = long.Parse(mappedString); break;

                            case DataSourceValue.bid: bid = double.Parse(mappedString); hasBidAsk = true; break;
                            case DataSourceValue.ask: ask = double.Parse(mappedString); hasBidAsk = true; break;
                            case DataSourceValue.bidSize: bidVolume = long.Parse(mappedString); break;
                            case DataSourceValue.askSize: askVolume = long.Parse(mappedString); break;

                            case DataSourceValue.optionExpiration: optionExpiry = DateTime.Parse(mappedString); break;
                            case DataSourceValue.optionStrike: optionStrike = double.Parse(mappedString); break;
                            case DataSourceValue.optionRight: optionIsPut = Regex.IsMatch(mappedString, "^[pP]"); break;
                        }
                    }
                    catch
                    {
                        // there are a lot of things that can go wrong here
                        // we try to move on, as long as we can parse at least part of the fields
                        Output.WriteLine("DataSourceCsv: parsing exception: {0}, {1}, {2}={3}", Info[DataSourceValue.nickName], line, mapping.Key, mapping.Value);
                    }
                }

                DateTime barTime = date.Date + time.TimeOfDay;

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
                    if (line.Length == 0)
                        continue; // to handle end of file

                    Bar bar = ValidateBar(CreateBar(line));
                    if (bar == null)
                        continue;

                    if (FirstTime == null)
                        FirstTime = bar.Time;

                    if (LastTime == null || bar.Time > LastTime)
                        LastTime = bar.Time;

                    if (bar.Time < LastTime)
                        throw new Exception("DataSourceCsv: bars out of sequence");

                    if (data.Count == 0
                    && bar.Time > loadStartTime
                    && prevBar != null)
                        data.Add(prevBar); // add previous bar, if we don't have a bar at the reqested start
                  
                    if (bar.Time >= loadStartTime
                    && bar.Time <= loadEndTime)
                        data.Add(bar);

                    prevBar = bar;
                    _totalBarsRead++;
                }
            }
            private void WriteCsv(string sourceName, IEnumerable<Bar> updateBars)
            {
                string updateFilePath = Path.Combine(Info[DataSourceValue.dataPath],
                    string.Format("{0:yyyy-MM-dd}-{1}.csv", updateBars.Select(b => b.Time).Max(), sourceName.ToLower()));

                DirectoryInfo d = new DirectoryInfo(Info[DataSourceValue.dataPath]);
                FileInfo[] files = d.GetFiles("*.*");
                if (files.Count() > 0
                && files.Select(i => i.Name).OrderByDescending(n => n).FirstOrDefault().CompareTo(updateFilePath) > 0)
                {
                    // try filename at end of the alphabet
                    updateFilePath = Path.Combine(Info[DataSourceValue.dataPath],
                        string.Format("zzz-{0:yyyy-MM-dd}-{1}.csv", updateBars.Select(b => b.Time).Max(), sourceName.ToLower()));
                }


                if (Directory.Exists(Info[DataSourceValue.dataPath])
                && !File.Exists(updateFilePath))
                {
                    using (StreamWriter writer = new StreamWriter(updateFilePath))
                    {
#if true
                        //--- find mapping
                        Dictionary<int, DataSourceValue> mapping = new Dictionary<int, DataSourceValue>();
                        for (int column = 1; column < 20; column++)
                        {
                            string map1 = string.Format("{{{0}}}", column);
                            string map2 = string.Format("{{{0}:", column);

                            DataSourceValue value = Info
                                .Where(i => i.Value.Contains(map1) || i.Value.Contains(map2))
                                .Select(i => i.Key)
                                .FirstOrDefault();

                            if (value != default(DataSourceValue))
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
                                    DataSourceValue prop = mapping[column];
                                    object value = null;
                                    switch (prop)
                                    {
                                        case DataSourceValue.date:
                                            value = bar.Time.Date;
                                            break;
                                        case DataSourceValue.time:
                                            value = bar.Time.TimeOfDay;
                                            break;

                                        case DataSourceValue.open:
                                            value = bar.Open;
                                            break;
                                        case DataSourceValue.high:
                                            value = bar.High;
                                            break;
                                        case DataSourceValue.low:
                                            value = bar.Low;
                                            break;
                                        case DataSourceValue.close:
                                            value = bar.Close;
                                            break;
                                        case DataSourceValue.volume:
                                            value = bar.Volume;
                                            break;

                                        case DataSourceValue.bid:
                                            value = bar.Bid;
                                            break;
                                        case DataSourceValue.ask:
                                            value = bar.Ask;
                                            break;
                                        case DataSourceValue.bidSize:
                                            value = bar.BidVolume;
                                            break;
                                        case DataSourceValue.askSize:
                                            value = bar.AskVolume;
                                            break;

                                        case DataSourceValue.optionExpiration:
                                            value = bar.OptionExpiry;
                                            break;
                                        case DataSourceValue.optionStrike:
                                            value = bar.OptionStrike;
                                            break;
                                        case DataSourceValue.optionRight:
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
                DateTime loadStartTime = LastTime != null
                    ? (DateTime)LastTime + TimeSpan.FromSeconds(1)
                    : startTime;
                DateTime updateStartTime = LastTime != null
                    ? (DateTime)LastTime + TimeSpan.FromSeconds(1)
                    : DateTime.Parse("01/01/1990");

                // we also have two end times
                // - the time at which we stop loading bars into memory
                // - the time at which we stop writing bars to the update file
                // we run our update for a few days more than requested (if that's possible)
                // to make sure we don't run it again, in case the same update is requested again
                DateTime loadEndTime = endTime;
                DateTime updateEndTime = endTime.Date + TimeSpan.FromDays(4) - TimeSpan.FromSeconds(1);

                // it doesn't seem to bother our update clients, if we request a time in the future
                // this also helps overcoming the issue of not requesting enough data, due to 
                // differences in time zone
                //if (updateEndTime > DateTime.Now)
                //    updateEndTime = DateTime.Now;

                DataUpdater updater = DataUpdater.New(Simulator, Info);
                if (updater != null)
                {
                    DateTime t1 = DateTime.Now;
                    Output.Write(string.Format("DataSourceCsv: updating data for {0}...", Info[DataSourceValue.nickName]));

                    // retrieve update data
                    // we copy these to a list, to avoid evaluating this multiple times
                    IEnumerable<Bar> updateBars = updater.UpdateData(updateStartTime, updateEndTime).ToList();

                    // write a new csv file
                    if (updateBars.Count() > 0)
                        WriteCsv(updater.Name, updateBars);

                    // load the bars into memory
                    foreach (Bar b in updateBars)
                    {
                        Bar bar = ValidateBar(b);
                        if (bar == null)
                            continue;

                        if (bar.Time >= loadStartTime
                        && bar.Time <= loadEndTime)
                            data.Add(bar);
                    }

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
                }
            }
            #endregion

            //---------- API
            #region public DataSourceCsv(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for CSV files.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceCsv(Dictionary<DataSourceValue, string> info) : base(info)
            {
                if (!Info.ContainsKey(DataSourceValue.dataPath))
                    throw new Exception("DataSourceCsv: missing mandatory dataPath key");

                // expand relative paths, if required
                if (!Info[DataSourceValue.dataPath].Substring(1, 1).Equals(":")   // drive letter
                && !Info[DataSourceValue.dataPath].Substring(0, 1).Equals(@"\")) // absolute path
                {
                    Info[DataSourceValue.dataPath] = string.Format(@"{0}\{1}",
                        Info[DataSourceValue.infoPath], Info[DataSourceValue.dataPath]);
                }

                // dataPath should be either a file name, or a directory
                // if it's neither, try to create a directory
                if (!File.Exists(Info[DataSourceValue.dataPath])
                && !Directory.Exists(Info[DataSourceValue.dataPath]))
                    Directory.CreateDirectory(Info[DataSourceValue.dataPath]);
            }
            #endregion
            #region override public IEnumerator<Bar> BarEnumerator
            /// <summary>
            /// Retrieve enumerator for this data source's bars.
            /// </summary>
            override public IEnumerator<Bar> BarEnumerator
            {
                get
                {
                    if (_barEnumerator == null)
                        _barEnumerator = _data.GetEnumerator();
                    return _barEnumerator;
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
                var cacheKey = CacheId.NewFromParameters(
                    Info[DataSourceValue.nickName].GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    DateTime t1 = DateTime.Now;
                    Output.Write(string.Format("DataSourceCsv: loading data for {0}...", Info[DataSourceValue.nickName]));

                    List<Bar> data = new List<Bar>();

                    if (File.Exists(Info[DataSourceValue.dataPath]))
                        LoadFile(data, Info[DataSourceValue.dataPath], startTime, endTime);
                    else if (Directory.Exists(Info[DataSourceValue.dataPath]))
                        LoadDir(data, Info[DataSourceValue.dataPath], startTime, endTime);

                    // this should never happen - we create an empty directory in DataSource.New
                    else
                        throw new Exception("DataSourceCsv: data path not found");

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    if (LastTime == null
                    || LastTime < endTime)
                        UpdateData(data, startTime, endTime);

                    return data;
                };

                _data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction);

                if (_data.Count == 0)
                    throw new Exception(string.Format("DataSourceCsv: no data for {0}", Info[DataSourceValue.nickName]));
            }
            #endregion
        }
    }
}
//==============================================================================
// end of file