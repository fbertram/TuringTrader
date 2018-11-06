//==============================================================================
// Project:     Trading Simulator
// Name:        DataSource
// Description: base class for instrument data
// History:     2018ix10, FUB, created
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
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// data source, providing a bar enumerator for one or more instruments
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
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(string.Format("DataSource: invalid data path {0}", value));

                GlobalSettings.DataPath = value;
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
            // check for info file
            string infoPathName = Path.Combine(DataPath, nickname + ".inf");

            if (!File.Exists(infoPathName))
                throw new Exception("failed to locate data source info for " + nickname);

            // create info structure
            Dictionary<DataSourceValue, string> infos = new Dictionary<DataSourceValue, string>();
            infos[DataSourceValue.nickName] = nickname;
            infos[DataSourceValue.ticker] = nickname;
            infos[DataSourceValue.symbol] = nickname;
            infos[DataSourceValue.infoPath] = DataPath;

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
            return new DataSourceCsv(infos);
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
                return Info.ContainsKey(DataSourceValue.optionExpiration);
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

        //----- abstract methods to be implemented by derived classes
        #region abstract public IEnumerator<Bar> BarEnumerator
        /// <summary>
        /// Enumerator for bars.
        /// </summary>
        abstract public IEnumerator<Bar> BarEnumerator
        {
            get;
        }
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
}
//==============================================================================
// end of file