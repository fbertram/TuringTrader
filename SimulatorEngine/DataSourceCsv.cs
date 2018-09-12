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
        //---------- internal data
        private List<Bar> _data;
        private IEnumerator<Bar> _barEnumerator;
        public static int TotalRecordsRead = 0;

        //---------- internal helpers
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
                catch (Exception e)
                {
                    throw new Exception(string.Format("failed to load zipped data file {0}, {1}", filePath, e.Message));
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

                _data.Add(new Bar(Info, items));

                TotalRecordsRead++;
            }
        }

        //---------- API
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