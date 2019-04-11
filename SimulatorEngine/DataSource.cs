//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSource
// Description: base class for instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
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
            // check for info file
            string infoPathName = Path.Combine(DataPath, nickname + ".inf");

            if (!File.Exists(infoPathName))
                throw new Exception("failed to locate data source info for " + nickname);

            // create info structure
            Dictionary<DataSourceValue, string> infos = new Dictionary<DataSourceValue, string>
            {
                { DataSourceValue.nickName, nickname },
                { DataSourceValue.infoPath, DataPath },
                { DataSourceValue.ticker, nickname },   // default value, expected to be overwritten
            };

            // load info file
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
                    throw new Exception(string.Format("error parsing data source info for {0}: line '{1}", nickname, line));
                }
            }

            // instantiate data source
            if (infos.ContainsKey(DataSourceValue.dataSource)
            && infos[DataSourceValue.dataSource].ToLower().Contains("norgate"))
            {
                return new DataSourceNorgate(infos);
            }
            else if (infos.ContainsKey(DataSourceValue.dataSource)
            && infos[DataSourceValue.dataSource].ToLower().Contains("fakeoptions"))
            {
                return new DataSourceFakeOptions(infos);
            }
            else if (infos.ContainsKey(DataSourceValue.dataSource)
            && infos[DataSourceValue.dataSource].ToLower().Contains("constantyield"))
            {
                return new DataSourceConstantYield(infos);
            }
            else if (infos.ContainsKey(DataSourceValue.dataSource)
            && infos[DataSourceValue.dataSource].ToLower().Contains("algorithm"))
            {
                return new DataSourceAlgorithm(infos);
            }
            else
            {
                return new DataSourceCsv(infos);
            }
        }
        #endregion
    }
}
//==============================================================================
// end of file