//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        ReportTemplate
// Description: Base class for C# report templates.
// History:     2019v28, FUB, created
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
using OxyPlot;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Base class for C# plotter templates.
    /// </summary>
    public abstract class ReportTemplate
    {
        #region public Dictionary<string, List<Dictionary<string, object>>> PlotData
        /// <summary>
        /// Property holding PlotData from Plotter object
        /// </summary>
        public Dictionary<string, List<Dictionary<string, object>>> PlotData
        {
            get;
            set;
        }
        #endregion
        #region public virtual IEnumerable<string> AvailableCharts
        /// <summary>
        /// Property providing list of available charts
        /// </summary>
        public virtual IEnumerable<string> AvailableCharts
        {
            get
            {
                return PlotData.Keys;
            }
        }
        #endregion
        #region public abstract PlotModel RenderChart(string selectedChart)
        /// <summary>
        /// Abstract method to render chart to OxyPlot model.
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        public abstract PlotModel RenderChart(string selectedChart);
        #endregion
    }
}

//==============================================================================
// end of file