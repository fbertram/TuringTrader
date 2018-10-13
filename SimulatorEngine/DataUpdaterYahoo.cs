//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterYahoo
// Description: Web data updater, Yahoo! finance
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
using Newtonsoft.Json.Linq;
#endregion

namespace FUB_TradingSim
{
    class DataUpdaterYahoo : DataUpdater
    {
        #region internal data & helpers
        // URL discovered with MultiCharts' QuoteManager
        private static readonly string _urlTemplate = @"http://l1-query.finance.yahoo.com/v8/finance/chart/{0}?interval=1d&period1={1}&period2={2}";
        #endregion
        #region internal helpers
        private static readonly DateTime _epochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnixTime(long unixTime)
        {
            return _epochOrigin.AddSeconds(unixTime);
        }

        public static long ToUnixTime(DateTime date)
        {
            return Convert.ToInt64((date - _epochOrigin).TotalSeconds);
        }
        #endregion

        #region public DataUpdaterYahoo(Dictionary<DataSourceValue, string> info) : base(info)
        public DataUpdaterYahoo(Dictionary<DataSourceValue, string> info) : base(info)
        {
        }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
        override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
        {
            string url = string.Format(_urlTemplate,
                Info[DataSourceValue.symbolYahoo], ToUnixTime(startTime), ToUnixTime(endTime));

            using (var client = new WebClient())
            {
                string rawData = client.DownloadString(url);

                JObject jsonData = JObject.Parse(rawData);

                IEnumerator<Int64> timeStamp = jsonData["chart"]["result"][0]["timestamp"]
                    .Select(x => Convert.ToInt64(x))
                    .GetEnumerator();

                IEnumerator<double> open = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["open"]
                    .Select(x => Convert.ToDouble(x))
                    .GetEnumerator();

                IEnumerator<double> high = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["high"]
                    .Select(x => Convert.ToDouble(x))
                    .GetEnumerator();

                IEnumerator<double> low = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["low"]
                    .Select(x => Convert.ToDouble(x))
                    .GetEnumerator();

                IEnumerator<double> close = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["close"]
                    .Select(x => Convert.ToDouble(x))
                    .GetEnumerator();

                IEnumerator<double> adjClose = jsonData["chart"]["result"][0]["indicators"]["adjclose"][0]["adjclose"]
                    .Select(x => Convert.ToDouble(x))
                    .GetEnumerator();

                IEnumerator<Int64> volume = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["volume"]
                    .Select(x => Convert.ToInt64(x))
                    .GetEnumerator();

                while (timeStamp.MoveNext())
                {
                    open.MoveNext();
                    high.MoveNext();
                    low.MoveNext();
                    close.MoveNext();
                    adjClose.MoveNext();
                    volume.MoveNext();

                    Bar newBar = new Bar(Info[DataSourceValue.symbol],
                        FromUnixTime(timeStamp.Current),
                        open.Current,
                        high.Current,
                        low.Current,
                        close.Current,
                        volume.Current,
                        true,
                        0.0, 0.0, 0, 0, false,
                        default(DateTime), 0.00, false);

                    if (newBar.Time >= startTime
                    &&  newBar.Time <= endTime)
                        yield return newBar;
                }

                yield break;
            }
        }
        #endregion

        #region public override string Name
        public override string Name
        {
            get
            {
                return "Yahoo";
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file