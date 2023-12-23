//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceYahoo
// Description: Data source for Yahoo! finance.
// History:     2022xi29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region internal helpers
        private static readonly DateTime _yahooEpochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static long _yahooToUnixTime(DateTime timestamp) => Convert.ToInt64((timestamp - _yahooEpochOrigin).TotalSeconds);
        private static DateTime _yahooFromUnixTime(long unixTime) => _yahooEpochOrigin.AddSeconds(unixTime);
        private static string _yahooConvertTicker(string ticker) => ticker.Replace('.', '-');
        #endregion
        private static List<BarType<OHLCV>> YahooLoadData(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadDataHelper<JObject>(
                algo, info,
                () =>
                {   // retrieve data from Yahoo
                    string url = string.Format(
                        @"http://query1.finance.yahoo.com/v8/finance/chart/"
                        + "{0}"
                        + "?interval=1d"
                        + "&period1={1}"
                        + "&period2={2}",
                        _yahooConvertTicker(info[DataSourceParam.symbolYahoo]),
                        0, // epoch origin 01/01/1970
                        _yahooToUnixTime(DateTime.Now + TimeSpan.FromDays(5)));

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringData) =>
                {   // parse data and check validity
                    try
                    {
                        if (stringData == null || stringData.Length < 25)
                            return null;

                        var json = JObject.Parse(stringData);

                        if (!json.HasValues)
                            return null;

                        return json;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                },
                (jsonData) =>
                {   // extract data for TuringTrader
                    var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                    var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                    var eT = ((JArray)jsonData["chart"]["result"][0]["timestamp"]).GetEnumerator();
                    var eO = ((JArray)jsonData["chart"]["result"][0]["indicators"]["quote"][0]["open"]).GetEnumerator();
                    var eH = ((JArray)jsonData["chart"]["result"][0]["indicators"]["quote"][0]["high"]).GetEnumerator();
                    var eL = ((JArray)jsonData["chart"]["result"][0]["indicators"]["quote"][0]["low"]).GetEnumerator();
                    var eC = ((JArray)jsonData["chart"]["result"][0]["indicators"]["quote"][0]["close"]).GetEnumerator();
                    var eV = ((JArray)jsonData["chart"]["result"][0]["indicators"]["quote"][0]["volume"]).GetEnumerator();
                    var eAC = ((JArray)jsonData["chart"]["result"][0]["indicators"]["adjclose"][0]["adjclose"]).GetEnumerator();

                    var bars = new List<BarType<OHLCV>>();
                    while (eT.MoveNext() && eO.MoveNext() && eH.MoveNext()
                        && eL.MoveNext() && eC.MoveNext() && eV.MoveNext() && eAC.MoveNext())
                    {
                        var utcOpen = _yahooFromUnixTime((long)eT.Current);
                        var exchangeClose = TimeZoneInfo.ConvertTime(utcOpen.ToLocalTime(), exchangeTimeZone).Date + timeOfDay;
                        var localDate = TimeZoneInfo.ConvertTimeToUtc(exchangeClose, exchangeTimeZone).ToLocalTime();

                        try
                        {
                            double o = (double)eO.Current;
                            double h = (double)eH.Current;
                            double l = (double)eL.Current;
                            double c = (double)eC.Current;
                            long v = (long)eV.Current;
                            double ac = (double)eAC.Current;

                            // adjust prices according to the adjusted close.
                            // note the volume is adjusted the opposite way.
                            double ao = o * ac / c;
                            double ah = h * ac / c;
                            double al = l * ac / c;
                            long av = (long)(v * c / ac);

                            bars.Add(new BarType<OHLCV>(
                                localDate,
                                new OHLCV(ao, ah, al, ac, av)));
                        }
                        catch
                        {
                            // Yahoo taints the results by filling in null values
                            // we try to handle this gracefully here
                            if (bars.Count < 1)
                                continue;

                            var prevBar = bars.Last();

                            bars.Add(new BarType<OHLCV>(
                                localDate,
                                new OHLCV(
                                    prevBar.Value.Open,
                                    prevBar.Value.High,
                                    prevBar.Value.Low,
                                    prevBar.Value.Close,
                                    prevBar.Value.Volume)));
                        }
                    }

                    return bars;
                });

        private static TimeSeriesAsset.MetaType YahooLoadMeta(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadMetaHelper<string>(
                algo, info,
                () =>
                {   // retrieve meta from Yahoo
                    string url = string.Format(
                        @"http://finance.yahoo.com/quote/"
                        + "{0}",
                        _yahooConvertTicker(info[DataSourceParam.symbolYahoo]));

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringWebPage) =>
                {   // parse data and check validity
                    try
                    {
                        string tmp1 = stringWebPage.Substring(stringWebPage.IndexOf("<h1"));
                        string tmp2 = tmp1.Substring(0, tmp1.IndexOf("h1>"));
                        string tmp3 = tmp2.Substring(tmp2.IndexOf(">") + 1);
                        string tmp4 = tmp3.Substring(0, tmp3.IndexOf("<"));
                        return tmp4;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (stringH1) => new TimeSeriesAsset.MetaType
                {   // extract meta for TuringTrader
                    Ticker = info[DataSourceParam.ticker],
                    Description = stringH1.Replace("&amp;", "&"),
                });

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> YahooGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            return Tuple.Create(
                YahooLoadData(owner, info),
                YahooLoadMeta(owner, info));
        }
    }
}

//==============================================================================
// end of file
