//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterStooq
// Description: Web data updater, stooq.com
// History:     2018x05, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class DataUpdaterStooq : DataUpdater
    {
        #region internal data
        private static readonly string _urlTemplate = @"https://stooq.com/q/d/l/?s={0}&d1={1:yyyy}{1:MM}{1:dd}&d2={2:yyyy}{2:MM}{2:dd}&i=d";
        private static readonly Dictionary<DataSourceValue, string> _parseInfo = new Dictionary<DataSourceValue, string>()
        {
            { DataSourceValue.dataPath, "{1}" },
            { DataSourceValue.time,     "16:00" },
            { DataSourceValue.open,     "{2}" },
            { DataSourceValue.high,     "{3}" },
            { DataSourceValue.low,      "{4}" },
            { DataSourceValue.close,    "{5}" },
            { DataSourceValue.volume,   "{6}" }
        };
        #endregion

        #region public DataUpdaterStooq(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
        public DataUpdaterStooq(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
        {
        }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
        override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
        {
            string url = string.Format(_urlTemplate,
                Info[DataSourceValue.symbolStooq], startTime, endTime);

            using (var client = new WebClient())
            {
                string rawData = client.DownloadString(url);

                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(rawData)))
                using (StreamReader reader = new StreamReader(ms))
                {
                    string header = reader.ReadLine(); // skip header line

                    for (string line; (line = reader.ReadLine()) != null;)
                    {
                        if (line.Length == 0)
                            continue; // to handle end of file

                        string[] items = line.Split(',');

                        DateTime time = DateTime.Parse(items[0]).Date + DateTime.Parse("16:00").TimeOfDay;
                        double open = double.Parse(items[1]);
                        double high = double.Parse(items[2]);
                        double low = double.Parse(items[3]);
                        double close = double.Parse(items[4]);
                        long volume = long.Parse(items[5]);

                        Bar bar = new Bar(
                            Info[DataSourceValue.symbol], time,
                            open, high, low, close, volume, true,
                            default(double), default(double), default(long), default(long), false,
                            default(DateTime), default(double), false);

                        if (bar.Time >= startTime
                        &&  bar.Time <= endTime)
                            yield return bar;
                    }

                    yield break;
                }
            }
        }
        #endregion

        #region public override string Name
        public override string Name
        {
            get
            {
                return "Stooq";
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file