//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Plotter
// Description: logging class to connect w/ CSV Files, Excel, R, C#
// History:     2017xii08, FUB, created
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class to log data, and save as CSV, or present with Excel or R.
    /// </summary>
    public class Plotter
    {
        #region internal data
        /// <summary>
        /// title of current plot
        /// </summary>
        private string CurrentPlot;

        /// <summary>
        /// x-axis label for given plot title
        /// </summary>
        private Dictionary<string, List<string>> AllLabels = new Dictionary<string, List<string>>();
        private List<string> CurrentLabels = null;
        private List<Dictionary<string, object>> CurrentData = null;
        #endregion
        #region internal helpers
        private string AddQuotesAsRequired(string value)
        {
            char[] needsQuotes = " ,".ToCharArray();

            if (value.IndexOfAny(needsQuotes) >= 0)
                return string.Format("\"{0}\"", value);
            else
                return value;
        }
        #endregion
        #region renderer
        /// <summary>
        /// Delegate for renderer event.
        /// </summary>
        /// <param name="plotter">plotter requesting rendering</param>
        /// <param name="template">name of rendering template</param>
        public delegate void renderer(Plotter plotter, string template);
        /// <summary>
        /// Renderer event. Renderers register with this event, to be invoked
        /// whenever rendering is required.
        /// </summary>
        public static event renderer Renderer;
        #endregion

        #region public Dictionary<string, List<Dictionary<string, object>>> AllData
        /// <summary>
        /// plotter data, indexed by chart title. for each chart, we have
        /// a list of dictionaries, one entry per row, with key being the name of the field.
        /// </summary>
        public Dictionary<string, List<Dictionary<string, object>>> AllData
        {
            get;
            private set;
        } = new Dictionary<string, List<Dictionary<string, object>>>();
        #endregion

        //----- initialization & cleanup
        #region public Plotter()
        /// <summary>
        /// Create and initialize logger object.
        /// </summary>
        public Plotter() { }
        #endregion
        #region public void Clear()
        /// <summary>
        /// Clear all current data.
        /// </summary>
        public void Clear()
        {
            AllLabels.Clear();
            AllData.Clear();
        }
        #endregion

        //----- logging values
        #region public void SelectChart(string chartTitle, string xLabel)
        /// <summary>
        /// Select current chart.
        /// </summary>
        /// <param name="chartTitle">title of chart</param>
        /// <param name="xLabel">label on x-axis</param>
        public void SelectChart(string chartTitle, string xLabel)
        {
            if (!AllLabels.ContainsKey(chartTitle))
                AllLabels[chartTitle] = new List<string>();

            if (!AllData.ContainsKey(chartTitle))
                AllData[chartTitle] = new List<Dictionary<string, object>>();

            CurrentPlot = chartTitle;
            CurrentData = AllData[chartTitle];
            CurrentLabels = AllLabels[chartTitle];

            if (CurrentLabels.Count == 0)
                CurrentLabels.Add(xLabel);

            if (CurrentLabels.First() != xLabel)
                throw new Exception("Logger: x-label mismatch");
        }
        #endregion
        #region public void SetX(object xValue)
        /// <summary>
        /// Set value along x-asis.
        /// </summary>
        /// <param name="xValue">x-axis value</param>
        public void SetX(object xValue)
        {
            if (CurrentPlot == null)
                SelectChart("Untitled Plot", "x");

            // create row for xValue (multiple rows w/ identical xValues are possible)
            CurrentData.Add(new Dictionary<string, object>());

            // save xValue
            CurrentData.Last()[CurrentLabels.First()] = xValue;
        }
        #endregion
        #region public void Plot(string yLabel, object yValue)
        /// <summary>
        /// Log new value to current plot, at current x-axis value.
        /// </summary>
        /// <param name="yLabel">y-axis label</param>
        /// <param name="yValue">y-axis value</param>
        public void Plot(string yLabel, object yValue)
        {
            if (!CurrentLabels.Contains(yLabel))
                CurrentLabels.Add(yLabel);

            CurrentData.Last()[yLabel] = yValue;
        }
        #endregion

        //----- output
        #region public int SaveAsCsv(string filePath, string plotTitle = null)
        /// <summary>
        /// Save log as CSV file.
        /// </summary>
        /// <param name="filePath">path to destination file</param>
        /// <param name="plotTitle">plot to save, null for current plot</param>
        /// <param name="toString">function to convert object to string, default null</param>
        public int SaveAsCsv(string filePath, string plotTitle = null, Func<object, string> toString = null)
        {
            if (plotTitle == null)
                plotTitle = CurrentPlot;

            if (toString == null)
                toString = (o) => o.ToString();

            using (StreamWriter file = new StreamWriter(filePath))
            {
                Output.WriteLine("{0}: saving {1} data points to {2}", plotTitle, AllData[plotTitle].Count, filePath);

                //--- header row
                var currentData = AllData[plotTitle];
                var currentLabels = AllLabels[plotTitle];

                file.WriteLine(string.Join(",", currentLabels.Select(l => AddQuotesAsRequired(l))));

                //--- data rows
                foreach (var row in currentData)
                {
                    var items = currentLabels
                        .Select(l => row.ContainsKey(l)
                            ? AddQuotesAsRequired(toString(row[l]))
                            : "");

                    file.WriteLine(string.Join(",", items));
                }

                // empty row seperates tables
                file.WriteLine("");
            }

            return AllData[plotTitle].Count;
        }
        #endregion
        #region public void OpenWith(string pathToTemplateFile)
        /// <summary>
        /// Open plot with Excel or R, depending on the template file.
        /// The template file can be an absolute path, a relative path, 
        /// or a simple file name. Relative pathes as well as simple
        /// file names will both start from the Templates folder inside
        /// TuringTrader's home location.
        /// </summary>
        /// <param name="pathToTemplateFile">path to template file</param>
        public void OpenWith(string pathToTemplateFile)
        {
            if (AllData.Keys.Count == 0)
                throw new Exception("Plotter: no data to plot");

            string fullPath = Path.IsPathRooted(pathToTemplateFile)
                ? pathToTemplateFile
                : Path.Combine(GlobalSettings.TemplatePath, pathToTemplateFile);

            string extension = Path.GetExtension(fullPath).ToLower();
            if (extension.Equals(""))
            {
                extension = GlobalSettings.DefaultTemplateExtension;
                fullPath += extension;
            }

            if (!File.Exists(fullPath))
                throw new Exception(string.Format("Plotter: template {0} not found", fullPath));

            if (Renderer != null)
                Renderer(this, fullPath);
        }
        #endregion
    }
}

//==============================================================================
// end of file