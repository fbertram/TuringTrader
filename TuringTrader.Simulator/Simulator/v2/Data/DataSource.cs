//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSource
// Description: Main entry point for data sources.
// History:     2022xi23, FUB, created
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

#define ENABLE_NORGATE
//#define ENABLE_TIINGO
//#define ENABLE_FRED
//#define ENABLE_FAKEOPTIONS
//#define ENABLE_CONSTYIELD
//#define ENABLE_ALGO
//#define ENABLE_CSV
//#define ENABLE_YAHOO
#define ENABLE_SPLICE
//#define ENABLE_STOOQ

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#endregion

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Helper class to load asset data from various data feeds.
    /// </summary>
    public static partial class DataSource
    {
        #region internal helpers
        /// <summary>
        /// Enum for tags in data source descriptor .inf file.
        /// </summary>
        private enum DataSourceParam
        {
            /// <summary>
            /// error, none of the defined values
            /// </summary>
            error,

            //----- general fields
            /// <summary>
            /// path to .inf data source descriptor
            /// </summary>
            infoPath,
            /// <summary>
            /// path to data
            /// </summary>
            dataPath,

            /// <summary>
            /// nickname
            /// </summary>
            nickName,
            /// <summary>
            /// nickname, w/o data source prefix
            /// </summary>
            nickName2,
            /// <summary>
            /// full descriptive name
            /// </summary>
            name,
            /// <summary>
            /// stock ticker
            /// </summary>
            ticker,

            /// <summary>
            /// allow synchronous execution
            /// </summary>
            allowSync,

            //----- CSV files
            /// <summary>
            /// quote date
            /// </summary>
            date,
            /// <summary>
            /// EOD quote time
            /// </summary>
            time,
            /// <summary>
            /// EOD quote time zone
            /// </summary>
            timezone,

            /// <summary>
            /// opening price
            /// </summary>
            open,
            /// <summary>
            /// high price
            /// </summary>
            high,
            /// <summary>
            /// low price
            /// </summary>
            low,
            /// <summary>
            /// closing price
            /// </summary>
            close,
            /// <summary>
            /// trading volume
            /// </summary>
            volume,

            /// <summary>
            /// bid price
            /// </summary>
            bid,
            /// <summary>
            /// ask price
            /// </summary>
            ask,
            /// <summary>
            /// bid volume
            /// </summary>
            bidSize,
            /// <summary>
            /// ask volume
            /// </summary>
            askSize,

            /// <summary>
            /// delimiter
            /// </summary>
            delim,

            //----- options contracts
            /// <summary>
            /// option expiration date
            /// </summary>
            optionExpiration,
            /// <summary>
            /// option strike price
            /// </summary>
            optionStrike,
            /// <summary>
            /// option right
            /// </summary>
            optionRight,
            /// <summary>
            /// option underlying symbol
            /// </summary>
            optionUnderlying,

            //----- futures contracts
            /// <summary>
            /// future expiration date
            /// </summary>
            futureExpiration,
            /// <summary>
            /// future underlying symbol
            /// </summary>
            futureUnderlying,

            //----- symbol mapping
            /// <summary>
            /// symbol for IQFeed/ DTN
            /// </summary>
            symbolIqfeed,
            /// <summary>
            /// symbol for Stooq.com
            /// </summary>
            symbolStooq,
            /// <summary>
            /// symbol for yahoo.com
            /// </summary>
            symbolYahoo,
            /// <summary>
            /// symbol for Interactive Brokers
            /// </summary>
            symbolInteractiveBrokers,
            /// <summary>
            /// symbol for Norgate Data
            /// </summary>
            symbolNorgate,
            /// <summary>
            /// symbol for fred.stlouisfed.org
            /// </summary>
            symbolFred,
            /// <summary>
            /// symbol for Tiingo
            /// </summary>
            symbolTiingo,
            /// <summary>
            /// data feed to use
            /// </summary>
            dataFeed,
            /// <summary>
            /// data updater to use
            /// </summary>
            dataUpdater,
            /// <summary>
            /// price multiplier for data updater
            /// </summary>
            dataUpdaterPriceMultiplier,

            /// <summary>
            /// symbol list for data splice
            /// </summary>
            symbolSplice,

            /// <summary>
            /// symbol for sub-classed algorithms
            /// </summary>
            symbolAlgo,
        };

        private static Dictionary<DataSourceParam, string> LoadIniFile(Dictionary<DataSourceParam, string> src, string nickname)
        {
            Dictionary<DataSourceParam, string> info = new Dictionary<DataSourceParam, string>(src);

            string infoPathName = Path.Combine(Simulator.GlobalSettings.DataPath, nickname + ".inf");
            if (!File.Exists(infoPathName))
                return src;

            string[] lines = File.ReadAllLines(infoPathName);

            foreach (string line in lines)
            {
                int idx = line.IndexOf('=');

                try
                {
                    DataSourceParam key = (DataSourceParam)
                        Enum.Parse(typeof(DataSourceParam), line.Substring(0, idx), true);

                    string value = line.Substring(idx + 1);

                    info[key] = value;
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("error parsing data source info {0}: line '{1}", infoPathName, line));
                }
            }

            info[DataSourceParam.nickName] = nickname; // we should never override the nickname
            info[DataSourceParam.infoPath] = infoPathName;

            return info;
        }

        private static Dictionary<DataSourceParam, string> FillInIfMissing(Dictionary<DataSourceParam, string> info, DataSourceParam field, string value)
        {
            if (info.ContainsKey(field)) return info;

            return new Dictionary<DataSourceParam, string>(info)
            {
                { field, value },
            };
        }

        private static Dictionary<DataSourceParam, string> GetInfo(Algorithm algo, string nickname)
        {
            //----- load initial settings
            var info = new Dictionary<DataSourceParam, string>
            {
                { DataSourceParam.nickName, nickname },
            };

            // set nickname2 and datafeed from nickname
            var idx = nickname.IndexOf(':');
            info[DataSourceParam.nickName2] = idx >= 0 ? nickname.Substring(idx + 1) : nickname;
            info[DataSourceParam.dataFeed] = idx >= 0 ? nickname.Substring(0, idx) : Simulator.GlobalSettings.DefaultDataFeed;

            // load ini file
            if (idx < 0) info = LoadIniFile(info, nickname);

            //----- fill in default values for any missing settings

            // default name is nickname
            info = FillInIfMissing(info, DataSourceParam.name, info[DataSourceParam.nickName]);

            // default ticker is nickname w/o data feed
            info = FillInIfMissing(info, DataSourceParam.ticker, info[DataSourceParam.nickName2]);

            // default data feed symbols are ticker
            info = FillInIfMissing(info, DataSourceParam.symbolIqfeed, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolStooq, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolYahoo, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolInteractiveBrokers, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolNorgate, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolFred, info[DataSourceParam.ticker]);
            info = FillInIfMissing(info, DataSourceParam.symbolTiingo, info[DataSourceParam.ticker]);

            // imply datafeed=csv, if parsing info is found
            if (info.ContainsKey(DataSourceParam.date)
                || info.ContainsKey(DataSourceParam.open)
                || info.ContainsKey(DataSourceParam.high)
                || info.ContainsKey(DataSourceParam.low)
                || info.ContainsKey(DataSourceParam.close)
                || info.ContainsKey(DataSourceParam.volume)
            ) info = FillInIfMissing(info, DataSourceParam.dataFeed, "csv");

            // use default csv mapping, if no mapping given
            if (info[DataSourceParam.dataFeed].ToLower().Contains("csv")
                && !info.ContainsKey(DataSourceParam.date)
                && !info.ContainsKey(DataSourceParam.open)
                && !info.ContainsKey(DataSourceParam.high)
                && !info.ContainsKey(DataSourceParam.low)
                && !info.ContainsKey(DataSourceParam.close)
                && !info.ContainsKey(DataSourceParam.volume)
            )
            {
                info = FillInIfMissing(info, DataSourceParam.date, "{1:MM/dd/yyyy}");
                info = FillInIfMissing(info, DataSourceParam.time, "16:00");
                // TODO: add timezone here
                info = FillInIfMissing(info, DataSourceParam.open, "{2:F2}");
                info = FillInIfMissing(info, DataSourceParam.high, "{3:F2}");
                info = FillInIfMissing(info, DataSourceParam.low, "{4:F2}");
                info = FillInIfMissing(info, DataSourceParam.close, "{5:F2}");
                info = FillInIfMissing(info, DataSourceParam.volume, "{6}");
                info = FillInIfMissing(info, DataSourceParam.delim, ",");
            }

            // default to timezone and time of day set by algorithm's trading calendar
            info = FillInIfMissing(info, DataSourceParam.time, algo.TradingCalendar.TimeOfClose.ToString());
            info = FillInIfMissing(info, DataSourceParam.timezone, algo.TradingCalendar.ExchangeTimeZone.Id);

            return info;
        }

        private static List<BarType<OHLCV>> ResampleToTradingCalendar(Algorithm algo, List<BarType<OHLCV>> src)
        {
            if (src.Count == 0) return src;

            var dst = new List<BarType<OHLCV>>();
            var srcIdx = 0;
            foreach (var tradingDay in algo.TradingCalendar.TradingDays)
            {
                while (srcIdx < src.Count - 1 && src[srcIdx + 1].Date <= tradingDay)
                    srcIdx++;

                var ohlcv = src[srcIdx].Value;

                dst.Add(new BarType<OHLCV>(tradingDay,
                    new OHLCV(ohlcv.Open, ohlcv.High, ohlcv.Low, ohlcv.Close, ohlcv.Volume)));
            }

            return dst;
        }

        private static HashSet<Tuple<
                string,
                Action,
                Func<Dictionary<DataSourceParam, string>, DateTime, DateTime, List<BarType<OHLCV>>>,
                Func<Dictionary<DataSourceParam, string>, TimeSeriesAsset.MetaType>>>
            _feeds
                = new HashSet<Tuple<
                    string,
                    Action,
                    Func<Dictionary<DataSourceParam, string>, DateTime, DateTime, List<BarType<OHLCV>>>,
                    Func<Dictionary<DataSourceParam, string>, TimeSeriesAsset.MetaType>>>
                {
#if ENABLE_NORGATE
                Tuple.Create("norgate", InitNorgateFeed, LoadNorgateData, LoadNorgateMeta),
#endif
#if ENABLE_SPLICE
                Tuple.Create("splice", (Action)null, LoadSpliceData, LoadSpliceMeta),
#endif
                };
        #endregion

        /// <summary>
        /// Load asset data from various data feeds.
        /// </summary>
        /// <param name="algo">parent algorithm</param>
        /// <param name="nickname">asset nickname</param>
        /// <returns>time series for asset</returns>
        /// <exception cref="Exception"></exception>
        public static TimeSeriesAsset LoadAsset(Algorithm algo, string nickname)
        {
            var data = algo.Cache(nickname, () =>
            {
                var info = GetInfo(algo, nickname);
                var days = algo.TradingCalendar.TradingDays;
                string dataSource = info[DataSourceParam.dataFeed].ToLower();

                foreach (var i in _feeds)
                    if (dataSource.Contains(i.Item1))
                    {
                        if (i.Item2 != null) i.Item2();

                        var barsRaw = i.Item3(info, days.First(), days.Last());
                        var barsResampled = ResampleToTradingCalendar(algo, barsRaw);
                        return barsResampled;
                    }

                throw new Exception(string.Format("DataSource: unknown data feed '{0}'", dataSource));
            });

            var meta = algo.Cache(nickname + ".Meta", () =>
            {
                var info = GetInfo(algo, nickname);
                string dataSource = info[DataSourceParam.dataFeed].ToLower();

                foreach (var i in _feeds)
                    if (dataSource.Contains(i.Item1))
                    {
                        if (i.Item2 != null) i.Item2();

                        var meta = i.Item4(info);
                        return (object)meta;
                    }

                throw new Exception(string.Format("DataSource: unknown data feed '{0}'", dataSource));
            });

            return new TimeSeriesAsset(
                algo,
                nickname,
                data,
                meta);
        }
    }
}

//==============================================================================
// end of file
