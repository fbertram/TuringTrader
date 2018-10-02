//==============================================================================
// Project:     Trading Simulator
// Name:        DataSourceCsv
// Description: Csv instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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

namespace FUB_TradingSim
{
    /// <summary>
    /// data source for CSV files
    /// </summary>
    public class DataSourceCsv : DataSource
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

            string symbol = Info[DataSourceValue.ticker];
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
                            symbol, barTime,
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
        private void LoadCsv(List<Bar> data, StreamReader reader, DateTime loadStartTime, DateTime loadEndTime)
        {
            string header = reader.ReadLine(); // skip header line

            for (string line; (line = reader.ReadLine()) != null;)
            {
                if (line.Length == 0)
                    continue; // to handle end of file

                Bar bar = CreateBar(line);

                if (FirstTime == null)
                    FirstTime = bar.Time;

                if (LastTime == null || bar.Time > LastTime)
                    LastTime = bar.Time;

                if (bar.Time < LastTime)
                    throw new Exception("DataSourceCsv: bars out of sequence");

                if (bar.Time >= loadStartTime
                && bar.Time <= loadEndTime)
                    data.Add(bar);

                _totalBarsRead++;
            }
        }
        private void WriteCsv(string name, IEnumerable<Bar> updateBars)
        {
            string updateFilePath = Path.Combine(Info[DataSourceValue.dataPath],
                string.Format("{0:yyyy-MM-dd}-{1}.csv", updateBars.Select(b => b.Time).Max(), name.ToLower()));

            if (Directory.Exists(Info[DataSourceValue.dataPath])
            && !File.Exists(updateFilePath))
            {
                using (StreamWriter writer = new StreamWriter(updateFilePath))
                {
                    // TODO: need to write with the proper column mapping
                    //       as defined by the Info property
                    writer.WriteLine("Date,Open,High,Low,Close,Volume");

                    foreach (Bar bar in updateBars)
                    {
                        writer.WriteLine("{0:MM/dd/yyyy},{1},{2},{3},{4},{5}",
                            bar.Time, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                    }
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
            if (updateEndTime > DateTime.Now)
                updateEndTime = DateTime.Now;

            DataUpdater updater = DataUpdater.New(Info);
            if (updater != null)
            {
                DateTime t1 = DateTime.Now;
                Output.Write(string.Format("DataSourceCsv: updating data for {0}...", Info[DataSourceValue.nickName]));

                // retrieve raw update data
                IEnumerable<Bar> updateBars = updater.UpdateData(updateStartTime, updateEndTime);

                // write a new csv file
                if (updateBars.Count() > 0)
                    WriteCsv(updater.Name, updateBars);

                // load the bars into memory
                foreach (Bar bar in updateBars.Where(b => b.Time >= loadStartTime && b.Time <= loadEndTime))
                    data.Add(bar);

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
            }
        }
        #endregion

        //---------- API
        #region public DataSourceCsv(Dictionary<DataSourceValue, string> info)
        public DataSourceCsv(Dictionary<DataSourceValue, string> info) : base(info)
        {
            // expand relative paths, if required
            if (!Info[DataSourceValue.dataPath].Substring(1, 1).Equals(":")   // drive letter
            &&  !Info[DataSourceValue.dataPath].Substring(0, 1).Equals(@"\")) // absolute path
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
        override public void LoadData(DateTime startTime, DateTime endTime)
        {
            string cacheKey = string.Format("{0}-{1}-{2}", Info[DataSourceValue.nickName], startTime, endTime);

            Func<List<Bar>> retrievalFunction = delegate ()
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
//==============================================================================
// end of file