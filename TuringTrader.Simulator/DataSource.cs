//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSource
// Description: base class for instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

#define ENABLE_NORGATE
#define ENABLE_TIINGO
#define ENABLE_FRED
#define ENABLE_FAKEOPTIONS
#define ENABLE_CONSTYIELD
#define ENABLE_ALGO
#define ENABLE_CSV
#define ENABLE_YAHOO

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Base class for data sources, providing a bar enumerator for one or more
    /// instruments. Other than instantiating data sources through the factory
    /// method New(), and adding them to the Algorithm's DataSources property,
    /// application developers do not need to interact with data sources directly.
    /// </summary>
    public abstract class DataSource
    {
        #region public static string DataPath
        /// <summary>
        /// Path to data base.
        /// </summary>
        public static string DataPath
        {
            get
            {
                return GlobalSettings.DataPath;
            }
        }
        #endregion
        #region public SimulatorCore Simulator
        /// <summary>
        /// Reference to simulator this instance is associated with.
        /// </summary>
        public SimulatorCore Simulator = null;
        #endregion

        //----- object factory
        #region static public DataSource New(string nickname)
        /// <summary>
        /// Factory function to instantiate new data source.
        /// </summary>
        /// <param name="nickname">nickname</param>
        /// <returns>data source object</returns>
        static public DataSource New(string nickname)
        {
            return DataSourceCollection.New(nickname);
        }
        #endregion
        #region protected DataSource(Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Create and initialize data source object.
        /// </summary>
        /// <param name="info">data source info</param>
        protected DataSource(Dictionary<DataSourceParam, string> info)
        {
            Info = info;
            FirstTime = null;
            LastTime = null;
        }
        #endregion

        //----- data source info
        #region public Dictionary<DataSourceValue, string> Info
        /// <summary>
        /// Data source info container.
        /// </summary>
        public Dictionary<DataSourceParam, string> Info
        {
            get;
            protected set;
        }
        #endregion
        #region public bool IsOption
        /// <summary>
        /// True, if this data source describes option contracts.
        /// </summary>
        public bool IsOption
        {
            get
            {
                //return Info.ContainsKey(DataSourceValue.optionExpiration);
                return Info.ContainsKey(DataSourceParam.optionUnderlying);
            }
        }
        #endregion
        #region public string OptionUnderlying
        /// <summary>
        /// Options only: Underlying symbol.
        /// </summary>
        public string OptionUnderlying
        {
            get
            {
                return Info[DataSourceParam.optionUnderlying];
            }
        }
        #endregion
        #region public DateTime? FirstTime
        private DateTime? _firstTime = null;
        /// <summary>
        /// First time stamp available in database.
        /// </summary>
        public DateTime? FirstTime
        {
            get
            {
                if (_firstTime == null && Data != null && Data.Count() > 0)
                    _firstTime = Data.First().Time;
                return _firstTime;
            }
            protected set
            {
                _firstTime = value;
            }
        }
        #endregion
        #region public DateTime? LastTime
        private DateTime? _lastTime = null;
        /// <summary>
        /// Last time stamp available in database.
        /// </summary>
        public DateTime? LastTime
        {
            get
            {
                if (_lastTime == null && Data != null && Data.Count() > 0)
                    _lastTime = Data.Last().Time;
                return _lastTime;
            }
            protected set
            {
                _lastTime = value;
            }
        }
        #endregion

        #region protected Bar ValidateBar(Bar bar)
        /// <summary>
        /// Validate bar. This function will either return a, possibly adjusted
        /// bar, or null if this bar should be dropped.
        /// </summary>
        /// <param name="bar">input bar</param>
        /// <returns>adjusted bar, or null</returns>
        protected Bar ValidateBar(Bar bar)
        {
            DateTime barTime = bar.Time;
            if (barTime.DayOfWeek >= DayOfWeek.Monday
            && barTime.DayOfWeek <= DayOfWeek.Friday)
            {
                if (barTime.TimeOfDay.TotalHours >= 9.5
                && barTime.TimeOfDay.TotalHours <= 16.0)
                {
                    return bar;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        //----- fields to fill/ methods to override by actual implementation
        #region public IEnumerable<Bar> Data
        /// <summary>
        /// Enumerable with Bar data.
        /// </summary>
        public IEnumerable<Bar> Data
        {
            get;
            protected set;
        } = null;
        #endregion
        #region abstract public void LoadData(DateTime startTime, DateTime endTime)
        /// <summary>
        /// Load data between time stamps into memory.
        /// </summary>
        /// <param name="startTime">beginning time stamp</param>
        /// <param name="endTime">end time stamp</param>
        abstract public void LoadData(DateTime startTime, DateTime endTime);
        #endregion
    }

    /// <summary>
    /// Collection of data source implementations. There is no need for
    /// application developers to interact with this class directly.
    /// </summary>
    public partial class DataSourceCollection
    {
        #region internal helpers
        private static void LoadInfoFile(string infoPathName, Dictionary<DataSourceParam, string> infos)
        {
            string[] lines = File.ReadAllLines(infoPathName);
            foreach (string line in lines)
            {
                int idx = line.IndexOf('=');

                try
                {
                    DataSourceParam key = (DataSourceParam)
                        Enum.Parse(typeof(DataSourceParam), line.Substring(0, idx), true);

                    string value = line.Substring(idx + 1);

                    infos[key] = value;
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("error parsing data source info {0}: line '{1}", infoPathName, line));
                }
            }
        }

        private static Dictionary<DataSourceParam, string> _defaultInfo = null;
        /// <summary>
        /// Retrieve data source defaults
        /// </summary>
        /// <param name="infos">current infos</param>
        /// <returns>default infos</returns>
        private static Dictionary<DataSourceParam, string> GetDefaultInfo(Dictionary<DataSourceParam, string> infos)
        {
            string nickName = infos[DataSourceParam.nickName];
            string nickName2 = infos[DataSourceParam.nickName2];
            string ticker = infos.ContainsKey(DataSourceParam.ticker)
                ? infos[DataSourceParam.ticker]
                : nickName2;

            //--- load defaults file, create copy
            //if (_defaultInfo == null)
            {
                _defaultInfo = new Dictionary<DataSourceParam, string>()
                {
                    // general info
                    { DataSourceParam.nickName, "{0}" },
                    { DataSourceParam.name, "{0}" },
                    { DataSourceParam.ticker, "{0}" },
                    //{ DataSourceValue.dataSource, "csv" },
                    { DataSourceParam.dataFeed, GlobalSettings.DefaultDataFeed },
                    // csv file defaults
                    { DataSourceParam.dataPath, "Data\\{0}" },
                    { DataSourceParam.date, "{1:MM/dd/yyyy}" },
                    { DataSourceParam.time, "16:00"},
                    { DataSourceParam.open, "{2:F2}" },
                    { DataSourceParam.high, "{3:F2}" },
                    { DataSourceParam.low, "{4:F2}" },
                    { DataSourceParam.close, "{5:F2}" },
                    { DataSourceParam.volume, "{6}" },
                    // symbol mapping
                    { DataSourceParam.symbolYahoo, "{0}"},
                    { DataSourceParam.symbolFred, "{0}"},
                    { DataSourceParam.symbolNorgate, "{0}"},
                    { DataSourceParam.symbolIqfeed, "{0}"},
                    { DataSourceParam.symbolStooq, "{0}"},
                    { DataSourceParam.symbolTiingo, "{0}"},
                    { DataSourceParam.symbolInteractiveBrokers, "{0}"},
                };

                string infoPathName = Path.Combine(DataPath, "_defaults_.inf");

                if (File.Exists(infoPathName))
                    LoadInfoFile(infoPathName, _defaultInfo);
            }

            var defaultInfo = new Dictionary<DataSourceParam, string>(_defaultInfo);

            //--- fill in nickname, as required
            List<DataSourceParam> updateWithNickname = new List<DataSourceParam>
            {
                DataSourceParam.nickName,
                DataSourceParam.name,
            };

            foreach (var field in updateWithNickname)
            {
                defaultInfo[field] = string.Format(_defaultInfo[field], nickName);
            }

            //--- fill in nickname w/o source, as required
            List<DataSourceParam> updateWithNickname2 = new List<DataSourceParam>
            {
                DataSourceParam.dataPath,
            };

            foreach (var field in updateWithNickname2)
            {
                defaultInfo[field] = string.Format(_defaultInfo[field], nickName2);
            }

            //--- fill in ticker, as required
            List<DataSourceParam> updateWithTicker = new List<DataSourceParam>
            {
                DataSourceParam.ticker,
                DataSourceParam.symbolYahoo,
                DataSourceParam.symbolNorgate,
                DataSourceParam.symbolIqfeed,
                DataSourceParam.symbolStooq,
                DataSourceParam.symbolInteractiveBrokers,
                DataSourceParam.symbolFred,
                DataSourceParam.symbolTiingo,
            };

            foreach (var field in updateWithTicker)
            {
                defaultInfo[field] = string.Format(_defaultInfo[field], ticker);
            }

            return defaultInfo;
        }


        #endregion

        #region private static string DataPath
        /// <summary>
        /// Path to data base.
        /// </summary>
        private static string DataPath
        {
            get
            {
                return GlobalSettings.DataPath;
            }
        }
        #endregion
        #region static public DataSource New(string nickname)
        /// <summary>
        /// Factory function to instantiate new data source.
        /// </summary>
        /// <param name="nickname">nickname</param>
        /// <returns>data source object</returns>
        static public DataSource New(string nickname)
        {
            //===== setup info structure
            Dictionary<DataSourceParam, string> infos = new Dictionary<DataSourceParam, string>();

            // we know our nickname
            // nickname2, w/o data source is preliminary
            infos[DataSourceParam.nickName] = nickname;
            infos[DataSourceParam.nickName2] = nickname;

            //===== load from .inf file
            if (!nickname.Contains(":"))
            {
                string infoPathName = Path.Combine(DataPath, nickname + ".inf");

                if (File.Exists(infoPathName))
                {
                    LoadInfoFile(infoPathName, infos);
                    infos[DataSourceParam.infoPath] = infoPathName;
                }
            }

            //===== optional: data source specified as part of nickname
            else
            {
                string[] tmp = nickname.Split(':');

                infos[DataSourceParam.dataFeed] = nickname; // TODO: do we need to use a substring here?
                infos[DataSourceParam.nickName2] = tmp[1];
            }

            //===== fill in defaults, as required
            Dictionary<DataSourceParam, string> defaults = GetDefaultInfo(infos);

            void defaultIfUndefined(DataSourceParam value)
            {
                if (!infos.ContainsKey(value))
                    infos[value] = defaults[value];
            }

            //--- name, ticker
            defaultIfUndefined(DataSourceParam.name);
            defaultIfUndefined(DataSourceParam.ticker);

            //--- data source
            // any mapping field (other than time) implies
            // that the data source is csv
            if (!infos.ContainsKey(DataSourceParam.dataFeed)
            && (infos.ContainsKey(DataSourceParam.date)
                || infos.ContainsKey(DataSourceParam.open)
                || infos.ContainsKey(DataSourceParam.high)
                || infos.ContainsKey(DataSourceParam.low)
                || infos.ContainsKey(DataSourceParam.close)
                || infos.ContainsKey(DataSourceParam.volume)
                || infos.ContainsKey(DataSourceParam.bid)
                || infos.ContainsKey(DataSourceParam.ask)
                || infos.ContainsKey(DataSourceParam.bidSize)
                || infos.ContainsKey(DataSourceParam.askSize)
                || infos.ContainsKey(DataSourceParam.dataUpdater)))
            {
                infos[DataSourceParam.dataFeed] = "csv";
            }
            else
            {
                defaultIfUndefined(DataSourceParam.dataFeed);
            }

            //--- parse info for csv
            defaultIfUndefined(DataSourceParam.time);

            // if the data source is csv, and none of the mapping
            // fields are set, we use a default mapping
            if (infos[DataSourceParam.dataFeed].ToLower().Contains("csv")
            && !infos.ContainsKey(DataSourceParam.date)
            && !infos.ContainsKey(DataSourceParam.open)
            && !infos.ContainsKey(DataSourceParam.high)
            && !infos.ContainsKey(DataSourceParam.low)
            && !infos.ContainsKey(DataSourceParam.close)
            && !infos.ContainsKey(DataSourceParam.volume)
            && !infos.ContainsKey(DataSourceParam.bid)
            && !infos.ContainsKey(DataSourceParam.ask)
            && !infos.ContainsKey(DataSourceParam.bidSize)
            && !infos.ContainsKey(DataSourceParam.askSize))
            {
                infos[DataSourceParam.date] = defaults[DataSourceParam.date];
                infos[DataSourceParam.open] = defaults[DataSourceParam.open];
                infos[DataSourceParam.high] = defaults[DataSourceParam.high];
                infos[DataSourceParam.low] = defaults[DataSourceParam.low];
                infos[DataSourceParam.close] = defaults[DataSourceParam.close];
                infos[DataSourceParam.volume] = defaults[DataSourceParam.volume];
            }

            // if data source is csv, datapath must be set
            if (infos[DataSourceParam.dataFeed].ToLower().Contains("csv"))
            {
                defaultIfUndefined(DataSourceParam.dataPath);
            }

            //--- symbol mapping
            defaultIfUndefined(DataSourceParam.symbolNorgate);
            defaultIfUndefined(DataSourceParam.symbolStooq);
            defaultIfUndefined(DataSourceParam.symbolYahoo);
            defaultIfUndefined(DataSourceParam.symbolFred);
            defaultIfUndefined(DataSourceParam.symbolIqfeed);
            defaultIfUndefined(DataSourceParam.symbolTiingo);
            defaultIfUndefined(DataSourceParam.symbolInteractiveBrokers);

            //===== instantiate data source
            string dataSource = infos[DataSourceParam.dataFeed].ToLower();

#if ENABLE_NORGATE
            if (dataSource.Contains("norgate"))
            {
                return new DataSourceNorgate(infos);
            }
            else
#endif
#if ENABLE_TIINGO
            if (dataSource.Contains("tiingo"))
            {
                return new DataSourceTiingo(infos);
            }
            else
#endif
#if ENABLE_FRED
            if (dataSource.Contains("fred"))
            {
                return new DataSourceFred(infos);
            }
            else
#endif
#if ENABLE_FAKEOPTIONS
            if (dataSource.Contains("fakeoptions"))
            {
                return new DataSourceFakeOptions(infos);
            }
            else
#endif
#if ENABLE_CONSTYIELD
            if (dataSource.Contains("constantyield"))
            {
                return new DataSourceConstantYield(infos);
            }
            else
#endif
#if ENABLE_ALGO
            if (dataSource.Contains("algo"))
            {
                return new DataSourceAlgorithm(infos);
            }
            else
#endif
#if ENABLE_CSV
            if (dataSource.Contains("csv"))
            {
                return new DataSourceCsv(infos);
            }
            else
#endif
#if ENABLE_YAHOO
            if (dataSource.Contains("yahoo"))
            {
                return new DataSourceYahoo(infos);
            }
            else
#endif

            throw new Exception("DataSource: can't instantiate data source");
        }
        #endregion
    }
}
//==============================================================================
// end of file
