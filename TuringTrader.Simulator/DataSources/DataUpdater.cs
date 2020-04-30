//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdater
// Description: Data updater base class
// History:     2018ix27, FUB, created
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
    /// Abstract base class for data updater. Data updaters are instantiated
    /// as required by the data source, and there is no need for application
    /// developers to interact with them directly.
    /// </summary>
    abstract public class DataUpdater
    {
        //----- object factory
        #region static public DataUpdater New(SimulatorCore simulator, Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Factory method to create new data updater object, based on info dictionary.
        /// </summary>
        /// <param name="simulator">parent simulator</param>
        /// <param name="info">info dictionary</param>
        /// <returns>new data updater object</returns>
        static public DataUpdater New(SimulatorCore simulator, Dictionary<DataSourceParam, string> info)
        {
            return DataUpdaterCollection.New(simulator, info);
        }
        #endregion
        #region protected DataUpdater(Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Create and initialize generic data updater object.
        /// </summary>
        /// <param name="simulator">parent simulator</param>
        /// <param name="info">info dictionary</param>
        protected DataUpdater(SimulatorCore simulator, Dictionary<DataSourceParam, string> info)
        {
            Simulator = simulator;
            Info = info;
        }
        #endregion

        #region public readonly Dictionary<DataSourceValue, string> Info;
        /// <summary>
        /// Info dictionary, holding data source description.
        /// </summary>
        public readonly Dictionary<DataSourceParam, string> Info;
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

    /// <summary>
    /// Collection of data updater implementations. There is no need for
    /// application developers to interact with this class directly.
    /// </summary>
    public partial class DataUpdaterCollection
    {
        #region static public DataUpdater New(SimulatorCore simulator, Dictionary<DataSourceValue, string> info)
        /// <summary>
        /// Factory method to create new data updater object, based on info dictionary.
        /// </summary>
        /// <param name="simulator">parent simulator</param>
        /// <param name="info">info dictionary</param>
        /// <returns>new data updater object</returns>
        static public DataUpdater New(SimulatorCore simulator, Dictionary<DataSourceParam, string> info)
        {
            if (!info.ContainsKey(DataSourceParam.dataUpdater))
                return null;

            string dataUpdater = info[DataSourceParam.dataUpdater].ToLower();

            if (dataUpdater.Contains("iq")
            && info.ContainsKey(DataSourceParam.symbolIqfeed))
                return new DataUpdaterIQFeed(simulator, info);

            if (dataUpdater.Contains("ib")
            && info.ContainsKey(DataSourceParam.symbolInteractiveBrokers))
                return new DataUpdaterIBOptions(simulator, info);

            if (dataUpdater.Contains("yahoo")
            && dataUpdater.Contains("opt")
            && info.ContainsKey(DataSourceParam.symbolYahoo))
                return new DataUpdaterYahooOptions(simulator, info);

            if (dataUpdater.Contains("yahoo")
            && info.ContainsKey(DataSourceParam.symbolYahoo))
                return new DataUpdaterYahoo(simulator, info);

            if (dataUpdater.Contains("stooq")
            && info.ContainsKey(DataSourceParam.symbolStooq))
                return new DataUpdaterStooq(simulator, info);

            if (dataUpdater.Contains("fred")
            && info.ContainsKey(DataSourceParam.symbolFred))
                return new DataUpdaterFred(simulator, info);

            return null;
        }
        #endregion
    }
}

//==============================================================================
// end of file