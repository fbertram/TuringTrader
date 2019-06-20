//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdaterStooq
// Description: Web data updater, stooq.com
// History:     2018x05, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
#if true
        private class DataUpdaterStooq : DataUpdater
        {
            public override string Name => "Stooq";

            public DataUpdaterStooq(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            {

            }
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                throw new Exception("Stooq download currently broken, we're working on it. Use Tiingo instead.");
            }
        }
#else
        private class DataUpdaterStooq : DataUpdater
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
                throw new Exception("Stooq download currently broken, we're working on it. Use Tiingo instead.");

                string url = string.Format(_urlTemplate,
                    Info[DataSourceValue.symbolStooq], startTime, endTime);

#if false
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {

                }
#endif
#if false
                WebClient wc = new WebClient();
                wc.DownloadFileAsync(new Uri(url), @"C:\Users\Felix\Desktop\stooq.txt");
                while (wc.IsBusy) { }
#endif

                using (var client = new WebClient())
                {
                    //client.Headers.Add("Referer", "https://stooq.com/");
                    //client.Headers.Add(@"User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
                    //var xxx = client.DownloadData(url);
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    client.Headers[HttpRequestHeader.Referer] = "https://stooq.com/";
                    string page = client.DownloadString("https://stooq.com/q/d/?s=^spx");
                    client.Headers[HttpRequestHeader.Referer] = "https://stooq.com/q/d/?s=^spx";
                    string csv = client.DownloadString("https://stooq.com/q/d/l/?s=^spx&i=d");
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

                            // TODO: we should use _parseInfo here!
                            DateTime time = DateTime.Parse(items[0]).Date + DateTime.Parse("16:00").TimeOfDay;
                            double open = double.Parse(items[1]);
                            double high = double.Parse(items[2]);
                            double low = double.Parse(items[3]);
                            double close = double.Parse(items[4]);
                            long volume; try { volume = long.Parse(items[5]); } catch { volume = 0; }

                            Bar bar = new Bar(
                                Info[DataSourceValue.ticker], time,
                                open, high, low, close, volume, true,
                                default(double), default(double), default(long), default(long), false,
                                default(DateTime), default(double), false);

                            if (bar.Time >= startTime
                            && bar.Time <= endTime)
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
#endif
    }
}

//==============================================================================
// end of file