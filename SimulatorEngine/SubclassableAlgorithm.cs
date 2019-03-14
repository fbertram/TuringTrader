//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SubclassableAlgorithm
// Description: Sub-classable Algorithm, for use by DataSourceAlgorithm.
// History:     2019iii13, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Base class for sub-classable algorithm
    /// </summary>
    public class SubclassableAlgorithm : Algorithm
    {
        /// <summary>
        /// Simulation start time, when sub-classed
        /// </summary>
        public DateTime? SubclassedStartTime = null;

        /// <summary>
        /// Simulation end time, when sub-classed
        /// </summary>
        public DateTime? SubclassedEndTime = null;

        /// <summary>
        /// Simulation output, when sub-classed
        /// </summary>
        public List<Bar> SubclassedData = null;

        /// <summary>
        /// Parent data source, when sub-classed
        /// </summary>
        public DataSource ParentDataSource = null;

        /// <summary>
        /// Copy current simulator status to sub-class Bar
        /// </summary>
        protected void CreateSubclassedBar()
        {
            if (SubclassedData != null)
            {
                SubclassedData.Add(new Bar(
                    ParentDataSource.Info[DataSourceValue.ticker],
                    SimTime[0],
                    NetAssetValue[0], NetAssetValue[0], NetAssetValue[0], NetAssetValue[0], 0, true,
                    default(double), default(double), default(long), default(long), false,
                    default(DateTime), default(double), false));
            }
        }
    }
}

//==============================================================================
// end of file