//==============================================================================
// Project:     Trading Simulator
// Name:        InstrumentDataCsv
// Description: Csv instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression; // requires reference to System.IO.Compression.dll

namespace FUB_TradingSim
{
    public class DataSourceCsv : DataSource
    {
        private List<Bar> _data;

        private IEnumerator<Bar> _barEnumerator;

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
        }

        override public IEnumerator<Bar> BarEnumerator
        {
            get
            {
                if (_barEnumerator == null)
                    _barEnumerator = _data.GetEnumerator();
                return _barEnumerator;
            }
        }

        private void LoadDir(string path, DateTime startTime)
        {
            DirectoryInfo d = new DirectoryInfo(path);
            FileInfo[] Files = d.GetFiles("*.*");

            if (Files.Count() == 0)
                throw new Exception(string.Format("no files to load for {0}", Info[DataSourceValue.ticker]));

            foreach (FileInfo file in Files)
            {
                LoadFile(file.FullName, startTime);
            }
        }

        private void LoadFile(string filePath, DateTime startTime)
        {
            if (Path.GetExtension(filePath).Equals(".zip"))
            {
                //throw new Exception("zip archive supported for " + Info[InstrumentDataField.symbol]);
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
                                LoadCsv(reader, startTime);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("failed to load zipped data file {0}", filePath));
                }
            }
            else
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    LoadCsv(sr, startTime);
                }
            }
        }

        private void LoadCsv(StreamReader sr, DateTime startTime)
        {
            sr.ReadLine(); // skip header line

            for (string line; (line = sr.ReadLine()) != null;)
            {
                line = Info[DataSourceValue.ticker] + "," + line;
                string[] items = line.Split(',');

                string symbol = Info[DataSourceValue.ticker];
                DateTime date = default(DateTime);
                DateTime time = default(DateTime);

                Dictionary<DataSourceValue, double> values = new Dictionary<DataSourceValue, double>();
                foreach (var mapping in Info)
                {
                    switch (mapping.Key)
                    {
                        // for stocks, symbol matches ticker
                        // for options, the symbol adds expiry, right, and strike to the ticker
                        case DataSourceValue.symbol:
                            symbol = string.Format(mapping.Value, items);
                            break;

                        case DataSourceValue.date:
                            date = DateTime.Parse(string.Format(mapping.Value, items));
                            break;

                        case DataSourceValue.time:
                            time = DateTime.Parse(string.Format(mapping.Value, items));
                            break;

                        case DataSourceValue.open:
                        case DataSourceValue.high:
                        case DataSourceValue.low:
                        case DataSourceValue.close:
                        case DataSourceValue.volume:
                            values[mapping.Key] = double.Parse(string.Format(mapping.Value, items));
                            break;
                    }
                }

                Bar bar = new Bar(
                        symbol,
                        date.Date + time.TimeOfDay,
                        values);

                _data.Add(bar);
            }
        }

        override public void LoadData(DateTime startTime)
        {
            _data = new List<Bar>();

            if (File.Exists(Info[DataSourceValue.dataPath]))
                LoadFile(Info[DataSourceValue.dataPath], startTime);
            else
                LoadDir(Info[DataSourceValue.dataPath], startTime);
        }
    }
}
//==============================================================================
// end of file