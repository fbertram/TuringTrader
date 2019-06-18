//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSource
// Description: base class for instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#define ENABLE_NORGATE
//#define ENABLE_TIINGO
//#define ENABLE_FRED
//#define ENABLE_FAKEOPTIONS
//#define ENABLE_CONSTYIELD
//#define ENABLE_ALGO
//#define ENABLE_CSV
//#define ENABLE_YAHOO


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
        protected DataSource(Dictionary<DataSourceValue, string> info)
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
        public Dictionary<DataSourceValue, string> Info
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
                return Info.ContainsKey(DataSourceValue.optionUnderlying);
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
                return Info[DataSourceValue.optionUnderlying];
            }
        }
        #endregion
        #region public DateTime? FirstTime
        /// <summary>
        /// First time stamp available in database.
        /// </summary>
        public DateTime? FirstTime
        {
            get;
            protected set;
        }
        #endregion
        #region public DateTime? LastTime
        /// <summary>
        /// Last time stamp available in database.
        /// </summary>
        public DateTime? LastTime
        {
            get;
            protected set;
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
        private static void LoadInfoFile(string infoPathName, Dictionary<DataSourceValue, string> infos)
        {
            string[] lines = File.ReadAllLines(infoPathName);
            foreach (string line in lines)
            {
                int idx = line.IndexOf('=');

                try
                {
                    DataSourceValue key = (DataSourceValue)
                        Enum.Parse(typeof(DataSourceValue), line.Substring(0, idx), true);

                    string value = line.Substring(idx + 1);

                    infos[key] = value;
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("error parsing data source info {0}: line '{1}", infoPathName, line));
                }
            }
        }

        private static Dictionary<DataSourceValue, string> _defaultInfo = null;
        /// <summary>
        /// Datasource default info container.
        /// </summary>
        private static Dictionary<DataSourceValue, string> GetDefaultInfo(Dictionary<DataSourceValue, string> infos)
        {
            string nickName = infos[DataSourceValue.nickName];
            string nickName2 = infos[DataSourceValue.nickName2];
            string ticker = infos.ContainsKey(DataSourceValue.ticker)
                ? infos[DataSourceValue.ticker]
                : nickName2;

            //--- load defaults file, create copy
            if (_defaultInfo == null)
            {
                _defaultInfo = new Dictionary<DataSourceValue, string>()
                {
                    // general info
                    { DataSourceValue.nickName, "{0}" },
                    { DataSourceValue.name, "{0}" },
                    { DataSourceValue.ticker, "{0}" },
                    //{ DataSourceValue.dataSource, "csv" },
                    { DataSourceValue.dataSource, GlobalSettings.DefaultDataFeed },
                    // csv file defaults
                    { DataSourceValue.dataPath, "Data\\{0}" },
                    { DataSourceValue.date, "{1:MM/dd/yyyy}" },
                    { DataSourceValue.time, "16:00"},
                    { DataSourceValue.open, "{2:F2}" },
                    { DataSourceValue.high, "{3:F2}" },
                    { DataSourceValue.low, "{4:F2}" },
                    { DataSourceValue.close, "{5:F2}" },
                    { DataSourceValue.volume, "{6}" },
                    // symbol mapping
                    { DataSourceValue.symbolYahoo, "{0}"},
                    { DataSourceValue.symbolFred, "{0}"},
                    { DataSourceValue.symbolNorgate, "{0}"},
                    { DataSourceValue.symbolIqfeed, "{0}"},
                    { DataSourceValue.symbolStooq, "{0}"},
                    { DataSourceValue.symbolTiingo, "{0}"},
                    { DataSourceValue.symbolInteractiveBrokers, "{0}"},
                };

                string infoPathName = Path.Combine(DataPath, "_defaults_.inf");

                if (File.Exists(infoPathName))
                    LoadInfoFile(infoPathName, _defaultInfo);
            }

            var defaultInfo = new Dictionary<DataSourceValue, string>(_defaultInfo);

            //--- fill in nickname, as required
            List<DataSourceValue> updateWithNickname = new List<DataSourceValue>
            {
                DataSourceValue.nickName,
                DataSourceValue.name,
            };

            foreach (var field in updateWithNickname)
            {
                defaultInfo[field] = string.Format(_defaultInfo[field], nickName);
            }

            //--- fill in nickname w/o source, as required
            List<DataSourceValue> updateWithNickname2 = new List<DataSourceValue>
            {
                DataSourceValue.dataPath,
            };

            foreach (var field in updateWithNickname2)
            {
                defaultInfo[field] = string.Format(_defaultInfo[field], nickName2);
            }

            //--- fill in ticker, as required
            List<DataSourceValue> updateWithTicker = new List<DataSourceValue>
            {
                DataSourceValue.ticker,
                DataSourceValue.symbolYahoo,
                DataSourceValue.symbolNorgate,
                DataSourceValue.symbolIqfeed,
                DataSourceValue.symbolStooq,
                DataSourceValue.symbolInteractiveBrokers,
                DataSourceValue.symbolFred,
                DataSourceValue.symbolTiingo,
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
            Dictionary<DataSourceValue, string> infos = new Dictionary<DataSourceValue, string>();

            // we know our nickname
            // nickname2, w/o data source is preliminary
            infos[DataSourceValue.nickName] = nickname;
            infos[DataSourceValue.nickName2] = nickname;

            //===== load from .inf file
            if (!nickname.Contains(":"))
            {
                string infoPathName = Path.Combine(DataPath, nickname + ".inf");

                if (File.Exists(infoPathName))
                {
                    LoadInfoFile(infoPathName, infos);
                    infos[DataSourceValue.infoPath] = infoPathName;
                }
            }

            //===== optional: data source specified as part of nickname
            else
            {
                string[] tmp = nickname.Split(':');

                infos[DataSourceValue.dataSource] = nickname; // TODO: do we need to use a substring here?
                infos[DataSourceValue.nickName2] = tmp[1];
            }

            //===== fill in defaults, as required
            Dictionary<DataSourceValue, string> defaults = GetDefaultInfo(infos);

            void defaultIfUndefined(DataSourceValue value)
            {
                if (!infos.ContainsKey(value))
                    infos[value] = defaults[value];
            }

            //--- name, ticker
            defaultIfUndefined(DataSourceValue.name);
            defaultIfUndefined(DataSourceValue.ticker);

            //--- data source
            // any mapping field (other than time) implies
            // that the data source is csv
            if (!infos.ContainsKey(DataSourceValue.dataSource)
            && (infos.ContainsKey(DataSourceValue.date)
                || infos.ContainsKey(DataSourceValue.open)
                || infos.ContainsKey(DataSourceValue.high)
                || infos.ContainsKey(DataSourceValue.low)
                || infos.ContainsKey(DataSourceValue.close)
                || infos.ContainsKey(DataSourceValue.volume)
                || infos.ContainsKey(DataSourceValue.bid)
                || infos.ContainsKey(DataSourceValue.ask)
                || infos.ContainsKey(DataSourceValue.bidSize)
                || infos.ContainsKey(DataSourceValue.askSize)
                || infos.ContainsKey(DataSourceValue.dataUpdater)))
            {
                infos[DataSourceValue.dataSource] = "csv";
            }
            else
            {
                defaultIfUndefined(DataSourceValue.dataSource);
            }

            //--- parse info for csv
            defaultIfUndefined(DataSourceValue.time);

            // if the data source is csv, and none of the mapping
            // fields are set, we use a default mapping
            if (infos[DataSourceValue.dataSource].ToLower().Contains("csv")
            && !infos.ContainsKey(DataSourceValue.date)
            && !infos.ContainsKey(DataSourceValue.open)
            && !infos.ContainsKey(DataSourceValue.high)
            && !infos.ContainsKey(DataSourceValue.low)
            && !infos.ContainsKey(DataSourceValue.close)
            && !infos.ContainsKey(DataSourceValue.volume)
            && !infos.ContainsKey(DataSourceValue.bid)
            && !infos.ContainsKey(DataSourceValue.ask)
            && !infos.ContainsKey(DataSourceValue.bidSize)
            && !infos.ContainsKey(DataSourceValue.askSize))
            {
                infos[DataSourceValue.date] = defaults[DataSourceValue.date];
                infos[DataSourceValue.open] = defaults[DataSourceValue.open];
                infos[DataSourceValue.high] = defaults[DataSourceValue.high];
                infos[DataSourceValue.low] = defaults[DataSourceValue.low];
                infos[DataSourceValue.close] = defaults[DataSourceValue.close];
                infos[DataSourceValue.volume] = defaults[DataSourceValue.volume];
            }

            // if data source is csv, datapath must be set
            if (infos[DataSourceValue.dataSource].ToLower().Contains("csv"))
            {
                defaultIfUndefined(DataSourceValue.dataPath);
            }

            //--- symbol mapping
                defaultIfUndefined(DataSourceValue.symbolNorgate);
            defaultIfUndefined(DataSourceValue.symbolStooq);
            defaultIfUndefined(DataSourceValue.symbolYahoo);
            defaultIfUndefined(DataSourceValue.symbolFred);
            defaultIfUndefined(DataSourceValue.symbolIqfeed);
            defaultIfUndefined(DataSourceValue.symbolTiingo);
            defaultIfUndefined(DataSourceValue.symbolInteractiveBrokers);

            //===== instantiate data source
            string dataSource = infos[DataSourceValue.dataSource].ToLower();

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