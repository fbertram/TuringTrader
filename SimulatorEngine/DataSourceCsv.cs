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
                line = Info[DataSourceValue.ticker] + "," + line;
                string[] items = line.Split(',');

                Bar bar = new Bar(Info, items);
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