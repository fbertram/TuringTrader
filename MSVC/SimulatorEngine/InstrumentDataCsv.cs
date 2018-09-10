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
    public class InstrumentDataCsv : InstrumentDataBase
    {
        private List<BarType> _data;

        private IEnumerator<BarType> _barEnumerator;

        public InstrumentDataCsv(Dictionary<InstrumentDataField, string> info) : base(info)
        {
            // expand relative paths, if required
            if (!Info[InstrumentDataField.dataPath].Substring(1, 1).Equals(":")   // drive letter
            &&  !Info[InstrumentDataField.dataPath].Substring(0, 1).Equals(@"\")) // absolute path
            {
                Info[InstrumentDataField.dataPath] = string.Format(@"{0}\{1}",
                    Info[InstrumentDataField.infoPath], Info[InstrumentDataField.dataPath]);
            }

            // dataPath is either a file name, or a directory
            // throw, if it's neither
            if (!File.Exists(Info[InstrumentDataField.dataPath]))
                if (!Directory.Exists(Info[InstrumentDataField.dataPath]))
                    throw new Exception(string.Format("data location for {0} not found", Info[InstrumentDataField.symbol]));
        }

        override public IEnumerator<BarType> BarEnumerator
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
                throw new Exception(string.Format("no files to load for {0}", Info[InstrumentDataField.ticker]));

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
                line = Info[InstrumentDataField.ticker] + "," + line;
                string[] items = line.Split(',');

                string ticker = Info[InstrumentDataField.symbol];
                DateTime date = default(DateTime);
                DateTime time = default(DateTime);
                double open = default(double);
                double high = default(double);
                double low = default(double);
                double close = default(double);
                double volume = default(double);

                foreach (var mapping in Info)
                {
                    switch (mapping.Key)
                    {
                        case InstrumentDataField.symbol:
                            ticker = string.Format(mapping.Value, items);
                            break;
                        case InstrumentDataField.date:
                            date = DateTime.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.time:
                            time = DateTime.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.open:
                            open = double.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.high:
                            high = double.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.low:
                            low = double.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.close:
                            close = double.Parse(string.Format(mapping.Value, items));
                            break;
                        case InstrumentDataField.volume:
                            volume = double.Parse(string.Format(mapping.Value, items));
                            break;
                    }
                }

                BarType bar = new BarType(
                        ticker,
                        date.Date + time.TimeOfDay,
                        open,
                        high,
                        low,
                        close,
                        volume);

                _data.Add(bar);
            }
        }

        override public void LoadData(DateTime startTime)
        {
            _data = new List<BarType>();

            if (File.Exists(Info[InstrumentDataField.dataPath]))
                LoadFile(Info[InstrumentDataField.dataPath], startTime);
            else
                LoadDir(Info[InstrumentDataField.dataPath], startTime);
        }
    }
}
//==============================================================================
// end of file