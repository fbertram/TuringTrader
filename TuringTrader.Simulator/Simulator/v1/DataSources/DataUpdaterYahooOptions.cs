//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdaterYahooOptions
// Description: Option data updater, Yahoo! finance
// History:     2018x15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
//              https://www.bertram.solutions
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

/*
{
  "optionChain": {
    "result": [
      {
        "underlyingSymbol": "^XSP",
        "expirationDates": [
          1539734400,
		...
          1576800000
        ],
        "strikes": [
          260.0,
		...
          320.0
        ],
        "hasMiniOptions": false,
        "quote": {
          "language": "en-US",
          "region": "US",
          "quoteType": "INDEX",
          "currency": "USD",
          "exchangeDataDelayedBy": 20,
          "market": "us_market",
          "marketState": "POST",
          "shortName": "S&P 500 MINI SPX OPTIONS INDEX",
          "sourceInterval": 15,
          "exchangeTimezoneName": "America/New_York",
          "exchangeTimezoneShortName": "EDT",
          "gmtOffSetMilliseconds": -14400000,
          "fiftyTwoWeekLowChange": 0.17999268,
          "fiftyTwoWeekLowChangePercent": 0.0006547569,
          "fiftyTwoWeekRange": "274.9 - 277.6",
          "fiftyTwoWeekHighChange": -2.5200195,
          "fiftyTwoWeekHighChangePercent": -0.00907788,
          "fiftyTwoWeekLow": 274.9,
          "fiftyTwoWeekHigh": 277.6,
          "priceHint": 2,
          "exchange": "WCB",
          "regularMarketPrice": 275.08,
          "regularMarketTime": 1539636775,
          "regularMarketChange": -1.6300049,
          "regularMarketOpen": 276.38,
          "regularMarketDayHigh": 277.6,
          "regularMarketDayLow": 274.9,
          "regularMarketVolume": 0,
          "esgPopulated": false,
          "tradeable": false,
          "regularMarketChangePercent": -0.58906615,
          "regularMarketDayRange": "274.9 - 277.6",
          "regularMarketPreviousClose": 276.71,
          "bid": 0.0,
          "ask": 0.0,
          "bidSize": 0,
          "askSize": 0,
          "messageBoardId": "finmb_INDEXXSP",
          "fullExchangeName": "Chicago Options",
          "symbol": "^XSP"
        },
        "options": [
          {
            "expirationDate": 1539734400,
            "hasMiniOptions": false,
            "calls": [
              {
                "contractSymbol": "XSP181017C00273000",
                "strike": 273.0,
                "currency": "USD",
                "lastPrice": 4.08,
                "change": 0.0,
                "percentChange": 0.0,
                "volume": 1,
                "openInterest": 0,
                "bid": 5.0,
                "ask": 5.38,
                "contractSize": "REGULAR",
                "expiration": 1539734400,
                "lastTradeDate": 1539402632,
                "impliedVolatility": 0.42969320312500003,
                "inTheMoney": true
              },
		...
            ],
            "puts": [
              {
                "contractSymbol": "XSP181017P00260000",
                "strike": 260.0,
                "currency": "USD",
                "lastPrice": 1.01,
                "change": 1.01,
                "percentChange": 100.0,
                "volume": 1,
                "openInterest": 1,
                "bid": 0.18,
                "ask": 0.31,
                "contractSize": "REGULAR",
                "expiration": 1539734400,
                "lastTradeDate": 1539402682,
                "impliedVolatility": 0.4194393994140625,
                "inTheMoney": false
              },
		...

            ]
          }
        ]
      }
    ],
    "error": null
  }
}
*/

#region libraries
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
        /// <summary>
        /// Data updater for yahoo.com option prices.
        /// </summary>
        private class DataUpdaterYahooOptions : DataUpdater
        {
            #region internal data
            // URL discovered with MultiCharts' QuoteManager
            private static readonly string _urlTemplate1 = @"http://l1-query.finance.yahoo.com/v7/finance/options/{0}";
            private static readonly string _urlTemplate2 = @"http://l1-query.finance.yahoo.com/v7/finance/options/{0}?&date={1}";
            #endregion
            #region internal helpers
            private static readonly DateTime _epochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            private static DateTime FromUnixTime(long unixTime)
            {
                return _epochOrigin.AddSeconds(unixTime);
            }

            private static long ToUnixTime(DateTime date)
            {
                return Convert.ToInt64((date - _epochOrigin).TotalSeconds);
            }

            private JToken LoadJson(string url)
            {
                using (var client = new WebClient())
                {
                    string rawData = client.DownloadString(url);

                    JObject jsonData = JObject.Parse(rawData);

                    return jsonData["optionChain"]["result"][0];
                }
            }

            private IEnumerable<Bar> LoadOptions(JToken options, bool isPut)
            {
                foreach (JToken option in options)
                {
                    string symbol = Convert.ToString(option["contractSymbol"]);
                    double strike = Convert.ToDouble(option["strike"]);
                    double lastPrice = Convert.ToDouble(option["lastPrice"]);
                    int volume = Convert.ToInt32(option["volume"]);
                    double bid = Convert.ToDouble(option["bid"]);
                    double ask = Convert.ToDouble(option["ask"]);
                    DateTime expiration = FromUnixTime(Convert.ToInt64(option["expiration"]));
                    DateTime lastTradeDate = FromUnixTime(Convert.ToInt64(option["lastTradeDate"]));
                    double impliedVolatility = Convert.ToDouble(option["impliedVolatility"]);

                    DateTime time = DateTime.Now;

                    Debug.WriteLine("{0}", symbol);

                    Bar newBar = new Bar(
                        symbol, time,
                        default(double), default(double), default(double), default(double), default(long), false,
                        bid, ask, default(long), default(long), true,
                        expiration, strike, isPut);

                    yield return newBar;
                }

                yield break;
            }
            #endregion

            #region public DataUpdaterYahooOptions(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            /// <summary>
            /// Create and initialize data updater.
            /// </summary>
            /// <param name="simulator">parent simulator</param>
            /// <param name="info">info dictionary</param>
            public DataUpdaterYahooOptions(SimulatorCore simulator, Dictionary<DataSourceParam, string> info) : base(simulator, info)
            {
            }
            #endregion

            #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Run data update.
            /// </summary>
            /// <param name="startTime">start of update range</param>
            /// <param name="endTime">end of update range</param>
            /// <returns>enumerable with updated bars</returns>
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                string url = string.Format(_urlTemplate1,
                    Info[DataSourceParam.symbolYahoo]);

                JToken result0 = LoadJson(url);

                List<DateTime> expirationDates = result0["expirationDates"]
                    .Select(x => FromUnixTime(Convert.ToInt64(x)))
                    .ToList();

                foreach (DateTime expirationDate in expirationDates)
                {
                    if (expirationDate != expirationDates.First())
                    {
                        url = string.Format(_urlTemplate2,
                            Info[DataSourceParam.symbolYahoo], ToUnixTime(expirationDate));
                        result0 = LoadJson(url);
                    }

                    //IEnumerable<double> strikes = result0["strikes"]
                    //    .Select(x => Convert.ToDouble(x));

                    JToken options0 = result0["options"][0];

                    //JToken optionsExpirationDate = FromUnixTime(Convert.ToInt64(options0["expirationDate"]));

                    IEnumerable<Bar> calls = LoadOptions(options0["calls"], false);
                    IEnumerable<Bar> puts = LoadOptions(options0["puts"], true);
                    IEnumerable<Bar> all = calls.Concat(puts);

                    foreach (Bar bar in all)
                        yield return bar;
                }

                yield break;
            }
            #endregion

            #region public override string Name
            /// <summary>
            /// Name of data updater.
            /// </summary>
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
}

//==============================================================================
// end of file