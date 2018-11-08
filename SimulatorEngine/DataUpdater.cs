//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdater
// Description: Data updater base class
// History:     2018ix27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Abstract base class for data updater.
    /// </summary>
    abstract public class DataUpdater
    {
        //----- object factory
        #region static public DataUpdate New(SimulatorCore simulator, Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Factory method to create new data updater object, based on info dictionary.
        /// </summary>
        /// <param name="simulator">parent simulator</param>
        /// <param name="info">info dictionary</param>
        /// <returns>new data updater object</returns>
        static public DataUpdater New(SimulatorCore simulator, Dictionary<DataSourceValue, string> info)
        {
            if (!info.ContainsKey(DataSourceValue.dataUpdater))
                return null;

            string dataUpdater = info[DataSourceValue.dataUpdater].ToLower();

            if (dataUpdater.Contains("iq") 
            &&  info.ContainsKey(DataSourceValue.symbolIqfeed))
                return new DataUpdaterIQFeed(simulator, info);

            if (dataUpdater.Contains("ib")
            &&  info.ContainsKey(DataSourceValue.symbolInteractiveBrokers))
                return new DataUpdaterIBOptions(simulator, info);

            if (dataUpdater.Contains("yahoo") 
            &&  dataUpdater.Contains("opt") 
            &&  info.ContainsKey(DataSourceValue.symbolYahoo))
                return new DataUpdaterYahooOptions(simulator, info);

            if (dataUpdater.Contains("yahoo") 
            && info.ContainsKey(DataSourceValue.symbolYahoo))
                    return new DataUpdaterYahoo(simulator, info);

            if (dataUpdater.Contains("stooq")
            && info.ContainsKey(DataSourceValue.symbolStooq))
                return new DataUpdaterStooq(simulator, info);

            return null;
        }
        #endregion
        #region protected DataUpdater(Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Create and initialize generic data updater object.
        /// </summary>
        /// <param name="simulator">parent simulator</param>
        /// <param name="info">info dictionary</param>
        protected DataUpdater(SimulatorCore simulator, Dictionary<DataSourceValue, string> info)
        {
            Simulator = simulator;
            Info = info;
        }
        #endregion

        #region public readonly Dictionary<DataSourceValue, string> Info;
        /// <summary>
        /// Info dictionary, holding data source description.
        /// </summary>
        public readonly Dictionary<DataSourceValue, string> Info;
        #endregion
        #region public readonly SimulatorCore Simulator
        /// <summary>
        /// Parent simulator object.
        /// </summary>
        public readonly SimulatorCore Simulator;
        #endregion

        #region abstract public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime);
        /// <summary>
        /// Run data update.
        /// </summary>
        /// <param name="startTime">start of update range</param>
        /// <param name="endTime">end of update range</param>
        /// <returns>enumerable with updated bars</returns>
        abstract public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime);
        #endregion
        #region abstract public string Name { get; }
        /// <summary>
        /// Name of data updater.
        /// </summary>
        abstract public string Name { get; }
        #endregion
    }
}

//==============================================================================
// end of file