//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        ReportTemplate
// Description: Base class for C# report templates.
// History:     2019v28, FUB, created
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
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Base class for C# plotter templates.
    /// </summary>
    public abstract class ReportTemplate
    {
        #region protected object RenderTable(string selectedChart)
        protected object RenderTable(string selectedChart)
        {
            return PlotData[selectedChart];
        }

        #endregion
        #region protected PlotModel RenderSimple(string selectedChart)
        /// <summary>
        /// Render x/y line chart.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>plot model</returns>
        protected PlotModel RenderSimple(string selectedChart)
        {
            //===== get plot data
            var chartData = PlotData[selectedChart];

            string xLabel = chartData
                .First()      // first row is as good as any
                .First().Key; // first column is x-axis

            object xValue = chartData
                .First()        // first row is as good as any
                .First().Value; // first column is x-axis

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = selectedChart;
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            Dictionary<string, LineSeries> allSeries = new Dictionary<string, LineSeries>();

            foreach (var row in chartData)
            {
                xValue = row[xLabel];

                foreach (var col in row)
                {
                    if (col.Key == xLabel)
                        continue;

                    if (col.Value.GetType() != typeof(double)
                    || double.IsInfinity((double)col.Value) || double.IsNaN((double)col.Value))
                        continue;

                    string yLabel = col.Key;
                    double yValue = (double)col.Value;

                    if (!allSeries.ContainsKey(yLabel))
                    {
                        var newSeries = new LineSeries();
                        newSeries.Title = yLabel;
                        newSeries.IsVisible = true;
                        newSeries.XAxisKey = "x";
                        newSeries.YAxisKey = "y";
                        allSeries[yLabel] = newSeries;
                    }

                    allSeries[yLabel].Points.Add(new DataPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        (double)yValue));
                }
            }

            //===== add series to plot model
            foreach (var series in allSeries)
                plotModel.Series.Add(series.Value);

            return plotModel;
        }
        #endregion
        #region protected PlotModel RenderScatter(string selectedChart)
        /// <summary>
        /// Render x/y scatter chart.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>plot model</returns>
        protected PlotModel RenderScatter(string selectedChart)
        {
            //===== get plot data
            var chartData = PlotData[selectedChart];

            string xLabel = chartData
                .First()      // first row is as good as any
                .First().Key; // first column is x-axis

            object xValue = chartData
                .First()        // first row is as good as any
                .First().Value; // first column is x-axis

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = selectedChart;
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            Dictionary<string, ScatterSeries> allSeries = new Dictionary<string, ScatterSeries>();

            foreach (var row in chartData)
            {
                xValue = row[xLabel];

                foreach (var col in row)
                {
                    if (col.Key == xLabel)
                        continue;

                    if (col.Value.GetType() != typeof(double)
                    || double.IsInfinity((double)col.Value) || double.IsNaN((double)col.Value))
                        continue;

                    string yLabel = col.Key;
                    double yValue = (double)col.Value;

                    if (!allSeries.ContainsKey(yLabel))
                    {
                        var newSeries = new ScatterSeries();
                        newSeries.Title = yLabel;
                        newSeries.IsVisible = true;
                        newSeries.XAxisKey = "x";
                        newSeries.YAxisKey = "y";
                        newSeries.MarkerType = MarkerType.Circle;
                        newSeries.MarkerSize = 2;
                        allSeries[yLabel] = newSeries;
                    }

                    allSeries[yLabel].Points.Add(new ScatterPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        (double)yValue));
                }
            }

            //===== add series to plot model
            foreach (var series in allSeries)
                plotModel.Series.Add(series.Value);

            return plotModel;
        }
        #endregion
        #region protected bool IsTable(string selectedChart)
        /// <summary>
        /// Determine if we should render as table.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>true for table</returns>
        protected bool IsTable(string selectedChart)
        {
            //===== get plot data
            var chartData = PlotData[selectedChart];

            bool isNumeric(object obj)
            {
                // see https://stackoverflow.com/questions/1749966/c-sharp-how-to-determine-whether-a-type-is-a-number

                HashSet<Type> numericTypes = new HashSet<Type>
                {
                    typeof(int),  typeof(double),  typeof(decimal),
                    typeof(long), typeof(short),   typeof(sbyte),
                    typeof(byte), typeof(ulong),   typeof(ushort),
                    typeof(uint), typeof(float),
                    typeof(DateTime)
                };

                Type type = obj.GetType();
                return numericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
            }

            foreach (var row in chartData)
            {
                foreach (var val in row.Values)
                {
                    if (!isNumeric(val))
                        return true;
                }
            }

            return false;
        }
        #endregion

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
        #region public abstract object RenderChart(string selectedChart)
        /// <summary>
        /// Abstract method to render chart to model.
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        public abstract object GetModel(string selectedChart);
        #endregion
    }
}

//==============================================================================
// end of file