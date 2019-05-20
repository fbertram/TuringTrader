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
    public abstract class SubclassableAlgorithm : Algorithm
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
        /// True, if algorithm is sub-classed
        /// </summary>
        public bool IsSubclassed
        {
            get
            {
                return SubclassedData != null;
            }
        }

        /// <summary>
        /// Add sub-classed bar: net asset value
        /// </summary>
        protected void AddSubclassedBar()
        {
            AddSubclassedBar(NetAssetValue[0]);
        }

        /// <summary>
        /// Add sub-classed bar: arbitrary value
        /// </summary>
        /// <param name="value">value to copy bar's OHLC</param>
        protected void AddSubclassedBar(double value)
        {
            Bar bar = Bar.NewOHLC(
                ParentDataSource.Info[DataSourceValue.ticker],
                SimTime[0],
                value, value, value, value, 0);

            AddSubclassedBar(bar);
        }

        /// <summary>
        /// Add sub-classed bar: pre-constructed bar
        /// </summary>
        /// <param name="bar">bar to add</param>
        protected void AddSubclassedBar(Bar bar)
        {
            if (SubclassedData != null)
                SubclassedData.Add(bar);
        }
    }
}

//==============================================================================
// end of file