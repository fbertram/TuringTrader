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
#define ENABLE_TIINGO
#define ENABLE_FRED
#define ENABLE_CSV
#define ENABLE_YAHOO
#define ENABLE_STOOQ
#define ENABLE_ALGO
#define ENABLE_SPLICE

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

        private static Dictionary<DataSourceParam, string> _loadIniFile(Dictionary<DataSourceParam, string> src, string nickname)
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

        private static Dictionary<DataSourceParam, string> _fillInIfMissing(Dictionary<DataSourceParam, string> info, DataSourceParam field, string value)
        {
            if (info.ContainsKey(field)) return info;

            return new Dictionary<DataSourceParam, string>(info)
            {
                { field, value },
            };
        }

        private static Dictionary<DataSourceParam, string> _getInfo(Algorithm algo, string nickname)
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
            if (idx < 0) info = _loadIniFile(info, nickname);

            //----- fill in default values for any missing settings

            // default name is nickname
            info = _fillInIfMissing(info, DataSourceParam.name, info[DataSourceParam.nickName]);

            // default ticker is nickname w/o data feed
            info = _fillInIfMissing(info, DataSourceParam.ticker, info[DataSourceParam.nickName2]);

            // default data feed symbols are ticker
            info = _fillInIfMissing(info, DataSourceParam.symbolIqfeed, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolStooq, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolYahoo, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolInteractiveBrokers, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolNorgate, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolFred, info[DataSourceParam.ticker]);
            info = _fillInIfMissing(info, DataSourceParam.symbolTiingo, info[DataSourceParam.ticker]);

            // imply datafeed=csv, if parsing info is found
            if (info.ContainsKey(DataSourceParam.date)
                || info.ContainsKey(DataSourceParam.open)
                || info.ContainsKey(DataSourceParam.high)
                || info.ContainsKey(DataSourceParam.low)
                || info.ContainsKey(DataSourceParam.close)
                || info.ContainsKey(DataSourceParam.volume)
            ) info = _fillInIfMissing(info, DataSourceParam.dataFeed, "csv");

            // fill in default mappings, if source is csv
            if (info[DataSourceParam.dataFeed].ToLower().Contains("csv"))
            {
                info = _fillInIfMissing(info, DataSourceParam.dataPath, info[DataSourceParam.nickName2]);
                info = _fillInIfMissing(info, DataSourceParam.date, "{1:MM/dd/yyyy}");
                info = _fillInIfMissing(info, DataSourceParam.open, "{2:F}");
                info = _fillInIfMissing(info, DataSourceParam.high, "{3:F}");
                info = _fillInIfMissing(info, DataSourceParam.low, "{4:F}");
                info = _fillInIfMissing(info, DataSourceParam.close, "{5:F}");
                info = _fillInIfMissing(info, DataSourceParam.volume, "{6}");
                info = _fillInIfMissing(info, DataSourceParam.delim, ",");
            }

            // default to timezone and time of day set by algorithm's trading calendar
            info = _fillInIfMissing(info, DataSourceParam.time, algo.TradingCalendar.TimeOfClose.ToString());
            info = _fillInIfMissing(info, DataSourceParam.timezone, algo.TradingCalendar.ExchangeTimeZone.Id);

            return info;
        }

        private static HashSet<Tuple<
                string,
                Action,
                Func<Algorithm, Dictionary<DataSourceParam, string>, List<BarType<OHLCV>>>,
                Func<Algorithm, Dictionary<DataSourceParam, string>, TimeSeriesAsset.MetaType>>>
            _feeds
                = new HashSet<Tuple<
                    string,
                    Action,
                    Func<Algorithm, Dictionary<DataSourceParam, string>, List<BarType<OHLCV>>>,
                    Func<Algorithm, Dictionary<DataSourceParam, string>, TimeSeriesAsset.MetaType>>>
                {
#if ENABLE_NORGATE
                Tuple.Create("norgate", InitNorgateFeed, LoadNorgateData, LoadNorgateMeta),
#endif
#if ENABLE_TIINGO
                Tuple.Create("tiingo", (Action)null, LoadTiingoData, LoadTiingoMeta),
#endif
#if ENABLE_FRED
                Tuple.Create("fred", (Action)null, LoadFredData, LoadFredMeta),
#endif
#if ENABLE_CSV
                Tuple.Create("csv", (Action)null, LoadCsvData, LoadCsvMeta),
#endif
#if ENABLE_YAHOO
                Tuple.Create("yahoo", (Action)null, LoadYahooData, LoadYahooMeta),
#endif
#if ENABLE_STOOQ
                Tuple.Create("stooq", (Action)null, LoadStooqData, LoadStooqMeta),
#endif
#if ENABLE_SPLICE
                Tuple.Create("splice", (Action)null, LoadSpliceData, LoadSpliceMeta),
                Tuple.Create("join", (Action)null, LoadJoinData, LoadJoinMeta),
#endif
#if ENABLE_ALGO
                Tuple.Create("algo", (Action)null, LoadAlgoData, LoadAlgoMeta),
#endif
                };

        private static List<BarType<OHLCV>> _loadData(Algorithm algo, string nickname, bool fillPrior = true)
        {
            var info = _getInfo(algo, nickname);
            string dataSource = info[DataSourceParam.dataFeed].ToLower();

            foreach (var i in _feeds)
                if (dataSource.Contains(i.Item1))
                {
                    if (i.Item2 != null) i.Item2();

                    var barsRaw = i.Item3(algo, info);
                    var barsResampled = ResampleToTradingCalendar(algo, barsRaw, fillPrior);
                    return barsResampled;
                }

            throw new Exception(string.Format("DataSource: unknown data feed '{0}'", dataSource));
        }
        private static TimeSeriesAsset.MetaType _loadMeta(Algorithm algo, string nickname)
        {
            var info = _getInfo(algo, nickname);
            string dataSource = info[DataSourceParam.dataFeed].ToLower();

            foreach (var i in _feeds)
                if (dataSource.Contains(i.Item1))
                {
                    if (i.Item2 != null) i.Item2();

                    var meta = i.Item4(algo, info);
                    return meta;
                }

            throw new Exception(string.Format("DataSource: unknown data feed '{0}'", dataSource));
        }
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
            var data = algo.Cache(nickname, () => _loadData(algo, nickname));
            var meta = algo.Cache(nickname + ".Meta", () => (object)_loadMeta(algo, nickname));

            return new TimeSeriesAsset(
                algo,
                nickname,
                data,
                meta);
        }

        /// <summary>
        /// Resample OHLCV data to algorithm's trading calendar.
        /// </summary>
        /// <param name="algo">parent algorithm</param>
        /// <param name="src">source data</param>
        /// <returns>resampled data</returns>
        public static List<BarType<OHLCV>> ResampleToTradingCalendar(Algorithm algo, List<BarType<OHLCV>> src, bool fillPrior = true)
        {
            if (src.Count == 0) return src;

            var dst = new List<BarType<OHLCV>>();
            var srcIdx = -1;

            foreach (var tradingDay in algo.TradingCalendar.TradingDays)
            {
                var freshBar = false;

                while (srcIdx < src.Count - 1 && src[srcIdx + 1].Date <= tradingDay)
                {
                    srcIdx++;
                    freshBar = true;
                }

                if (srcIdx < 0)
                {
                    // prior to source data: repeat opening price
                    var value = src.First().Value.Open;

                    if (fillPrior)
                        dst.Add(new BarType<OHLCV>(tradingDay,
                            new OHLCV(value, value, value, value, 0)));
                }
                else if (srcIdx < src.Count - 1 || freshBar)
                {
                    // within source data range: use bars as-is
                    var ohlcv = src[srcIdx].Value;

                    dst.Add(new BarType<OHLCV>(tradingDay,
                        new OHLCV(ohlcv.Open, ohlcv.High, ohlcv.Low, ohlcv.Close, ohlcv.Volume)));
                }
                else
                {
                    // after source data: repeat closing price
                    var value = src.Last().Value.Close;

                    dst.Add(new BarType<OHLCV>(tradingDay,
                        new OHLCV(value, value, value, value, 0)));
                }
            }

            return dst;
        }
    }
}

//==============================================================================
// end of file
