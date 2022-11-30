//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceStooq
// Description: Data source for data from Stooq.
// History:     2022xi30, FUB, created
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
using System.Net.Http;
using System.Text;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        private static string _stooqConvertTicker(string ticker) => ticker.ToLower();
        private static List<BarType<OHLCV>> LoadStooqData(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadDataHelper<string>(
                algo, info,
                () =>
                {   // retrieve data from stooq
                    // NOTE: we request a static range here, to make
                    //       offline behavior as pleasant as possible
                    string url = string.Format(
                        "https://stooq.pl/q/d/l/"
                            + "?s={0}"
                            + "&d1={1:yyyy}{1:MM}{1:dd}"
                            + "&d2={2:yyyy}{2:MM}{2:dd}"
                            + "&i=d",
                        _stooqConvertTicker(info[DataSourceParam.symbolStooq]),
                        DateTime.Parse("01/01/1950", CultureInfo.InvariantCulture),
                        DateTime.Now + TimeSpan.FromDays(5));

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringData) =>
                {   // parse data and check validity
                    try
                    {
                        if (stringData == null || stringData.Length < 25)
                            return null;

                        return stringData;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (csvData) =>
                {   // extract data for TuringTrader
                    var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                    var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(csvData)))
                    using (StreamReader reader = new StreamReader(ms))
                    {
                        string header = reader.ReadLine(); // skip header line

                        var bars = new List<BarType<OHLCV>>();
                        for (string line; (line = reader.ReadLine()) != null;)
                        {
                            if (line.Length == 0)
                                continue; // to handle end of file

                            try
                            {
                                string[] items = line.Split(',');
                                DateTime exchangeClose = DateTime.Parse(items[0], CultureInfo.InvariantCulture).Date + timeOfDay;
                                var localDate = TimeZoneInfo.ConvertTimeToUtc(exchangeClose, exchangeTimeZone).ToLocalTime();

                                var open = double.Parse(items[1], CultureInfo.InvariantCulture);
                                var high = double.Parse(items[2], CultureInfo.InvariantCulture);
                                var low = double.Parse(items[3], CultureInfo.InvariantCulture);
                                var close = double.Parse(items[4], CultureInfo.InvariantCulture);
                                var volume = items.Length > 5 ? double.Parse(items[5], CultureInfo.InvariantCulture) : 0;

                                bars.Add(new BarType<OHLCV>(
                                    localDate,
                                    new OHLCV(open, high, low, close, volume)));
                            }
                            catch (Exception ex)
                            {
                                // do nothing here - resampling will fix it gracefully
                                var x = 5;
                            }
                        }

                        return bars;
                    }
                });

        private static TimeSeriesAsset.MetaType LoadStooqMeta(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
            _loadMetaHelper<string>(
                algo, info,
                () =>
                {   // retrieve meta from Yahoo
                    string url = string.Format(
                        @"https://stooq.pl/q/d/?s={0}&c=0",
                        _stooqConvertTicker(info[DataSourceParam.symbolStooq]));

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (stringWebPage) =>
                {   // parse data and check validity
                    try
                    {
                        string tmp1 = stringWebPage.Substring(stringWebPage.IndexOf("<title")).Replace("<title", "");
                        string tmp2 = tmp1.Substring(0, tmp1.IndexOf("title>"));
                        string tmp3 = tmp2.Substring(tmp2.IndexOf(">") + 1);
                        string tmp4 = tmp3.Substring(0, tmp3.IndexOf("<"));
                        return tmp4;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (stringTitle) => new TimeSeriesAsset.MetaType
                {   // extract meta for TuringTrader
                    Ticker = info[DataSourceParam.ticker],
                    Description = stringTitle.Replace("&amp;", "&"),
                });
    }
}

//==============================================================================
// end of file
