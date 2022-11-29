//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceCsv
// Description: Virtual data source to use data from algorithms.
// History:     2022xi25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region internal helpers
        private static DateTime _parseDate(string value, string mapping)
        {
            var mapping2 = Regex.Replace(mapping, "{[0-9]+:", "").Replace("}", "");
            var parsed = DateTime.ParseExact(value, mapping2, CultureInfo.InvariantCulture);
            return parsed;
        }
        private static double _parseDouble(string value, string mapping)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
        private static double _parseLong(string value, string mapping)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
        private static List<BarType<OHLCV>> _loadCsv(Algorithm algo, Dictionary<DataSourceParam, string> info, StreamReader reader)
        {
            var bars = new List<BarType<OHLCV>>();

            var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
            var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

            string header = reader.ReadLine(); // skip header line

            for (string line; (line = reader.ReadLine()) != null;)
            {
                // handle end of file gracefully
                if (line.Length == 0)
                    continue;

                // split line at delimiter
                // NOTE: we also add ticker to the front of the line
                //       to preserve compatibility with the v1 engine
                string[] items = (info[DataSourceParam.ticker] + info[DataSourceParam.delim] + line)
                    .Split(info[DataSourceParam.delim]);

                string ticker = info[DataSourceParam.ticker];
                DateTime date = default(DateTime);
                double open = default(double);
                double high = default(double);
                double low = default(double);
                double close = default(double);
                double volume = default(double);

                foreach (var field in new List<DataSourceParam>
                {
                    DataSourceParam.date,
                    DataSourceParam.open,
                    DataSourceParam.high,
                    DataSourceParam.low,
                    DataSourceParam.close,
                    DataSourceParam.volume
                })
                {
                    try
                    {
                        // extract the relevant sub-string
                        // also: drop leading spaces, introduced by a space after the delimiting comma
                        string mappedString = string.Format(info[field], items)
                            .Trim(' ');

                        // assign sub-string to field
                        switch (field)
                        {
                            case DataSourceParam.date: date = _parseDate(mappedString, info[field]); break;
                            case DataSourceParam.open: open = _parseDouble(mappedString, info[field]); break;
                            case DataSourceParam.high: high = _parseDouble(mappedString, info[field]); break;
                            case DataSourceParam.low: low = _parseDouble(mappedString, info[field]); break;
                            case DataSourceParam.close: close = _parseDouble(mappedString, info[field]); break;
                            case DataSourceParam.volume: volume = _parseLong(mappedString, info[field]); break;
                        }
                    }
                    catch
                    {
                        // there are a lot of things that can go wrong here
                        // we try to move on for as long as we can and
                        // parse at least some if not most of the fields
                        Output.WriteLine("Failed to parse file {0}, field {1}: {2}",
                            info[DataSourceParam.dataPath], info[field], line);
                    }
                }

                var dateTimeAtExchange = date.Date + timeOfDay;
                var dateTimeLocal = TimeZoneInfo.ConvertTimeToUtc(dateTimeAtExchange, exchangeTimeZone).ToLocalTime();

                bars.Add(new BarType<OHLCV>(
                    dateTimeLocal,
                    new OHLCV(open, high, low, close, volume)));
            }
            return bars;
        }
        private static List<BarType<OHLCV>> _loadCsvFile(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var filePath = info[DataSourceParam.dataPath];

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
                                return _loadCsv(algo, info, reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Failed to load zipped data file {0}: {1}", filePath, e.Message));
                }
            }

            //else
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    return _loadCsv(algo, info, sr);
                }
            }
        }

        private static List<BarType<OHLCV>> _loadCsvDir(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            DirectoryInfo d = new DirectoryInfo(info[DataSourceParam.dataPath]);
            FileInfo[] Files = d.GetFiles("*.*");

            var data = new List<BarType<OHLCV>>();
            foreach (FileInfo file in Files.OrderBy(f => f.Name))
            {
                var info2 = new Dictionary<DataSourceParam, string>(info)
                {
                    { DataSourceParam.dataPath, file.FullName }
                };

                data = data.Concat(_loadCsvFile(algo, info2))
                    .ToList();
            }

            return data;
        }
        #endregion

        private static List<BarType<OHLCV>> LoadCsvData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var dataPath = info[DataSourceParam.dataPath].Replace('/', Path.DirectorySeparatorChar); // fix dir separator

            // expand relative paths, if required
            var info2 = new Dictionary<DataSourceParam, string>(info);
            if (!dataPath.Substring(1, 1).Equals(":")  // drive letter
            && !dataPath.Substring(0, 1).Equals(Path.DirectorySeparatorChar)) // absolute path
                info2[DataSourceParam.dataPath] = Path.Combine(Simulator.GlobalSettings.DataPath, dataPath);

            if (File.Exists(info2[DataSourceParam.dataPath]))
                return _loadCsvFile(algo, info2);
            else if (Directory.Exists(info2[DataSourceParam.dataPath]))
                return _loadCsvDir(algo, info2);

            throw new Exception(string.Format("Failed to locate csv data for {0} at {1}", info2[DataSourceParam.nickName2], info2[DataSourceParam.dataPath]));

        }
        private static TimeSeriesAsset.MetaType LoadCsvMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            return new TimeSeriesAsset.MetaType
            {
                Ticker = info[DataSourceParam.nickName2],
                Description = info[DataSourceParam.nickName2],
            };
        }
    }
}

//==============================================================================
// end of file
