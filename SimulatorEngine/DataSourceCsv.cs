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
        public static int TotalRecordsRead = 0;
        #endregion
        #region internal helpers
        private Bar CreateBar(string[] items)
        {
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
                string mappedString = string.Format(mapping.Value, items);

                switch (mapping.Key)
                {
                    // for stocks, symbol matches ticker
                    // for options, the symbol adds expiry, right, and strike to the ticker
                    //case DataSourceValue.symbol: Symbol = mappedString;                break;
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

            if (Files.Count() == 0)
                throw new Exception(string.Format("no files to load for {0}", Info[DataSourceValue.ticker]));

            foreach (FileInfo file in Files)
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
                    throw new Exception(string.Format("failed to load zipped data file {0}, {1}", filePath, e.Message));
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
        private void LoadCsv(List<Bar> data, StreamReader sr, DateTime startTime, DateTime endTime)
        {
            sr.ReadLine(); // skip header line

            for (string line; (line = sr.ReadLine()) != null;)
            {
                if (line.Length == 0)
                    continue; // to handle end of file

                line = Info[DataSourceValue.ticker] + "," + line;
                string[] items = line.Split(',');

                Bar bar = CreateBar(items);
                if (bar.Time >= startTime
                &&  bar.Time <= endTime)
                    data.Add(bar);

                TotalRecordsRead++;
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

            // dataPath is either a file name, or a directory
            // throw, if it's neither
            if (!File.Exists(Info[DataSourceValue.dataPath]))
                if (!Directory.Exists(Info[DataSourceValue.dataPath]))
                    throw new Exception(string.Format("data location for {0} not found", Info[DataSourceValue.symbol]));

            // TODO: not sure what proper error handling should be.
            // IDEA: if no data found, trigger UpdateData?
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
                else
                    LoadDir(data, Info[DataSourceValue.dataPath], startTime, endTime);

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                return data;
            };

            _data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction);
        }
        #endregion
    }
}
//==============================================================================
// end of file