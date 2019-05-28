//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SimpleChart
// Description: C# report template for SimpleChart
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
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// C# report template for SimpleChart
    /// </summary>
    public class SimpleChart : ReportTemplate
    {
        /// <summary>
        /// Render plotter chart to OxyPlot model.
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        public override PlotModel RenderChart(string selectedChart)
        {
            //===== get plot data
            var chartData = PlotData[selectedChart];

            Dictionary<string, LineSeries> allSeries = new Dictionary<string, LineSeries>();

            string xLabel = chartData
                .First() // first row contains column headers
                .First().Key; // first column is x-axis

            //object xValue = null;
            double xValue = 0;
            //object xValue = chartData[1] // 
            //    .First().Key;

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();

            plotModel.Title = selectedChart;

            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;

            var yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Right;

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            foreach (var row in chartData)
            {
                //xValue = row[xLabel];
                xValue += 1.0;

                if (xValue.GetType() != typeof(double))
                    continue;

                foreach (var col in row)
                {
                    if (col.Key == xLabel)
                        continue;

                    string yLabel = col.Key;
                    object yValue = col.Value;

                    if (yValue.GetType() != typeof(double)
                    || double.IsInfinity((double)yValue) || double.IsNaN((double)yValue))
                        continue;

                    if (!allSeries.ContainsKey(yLabel))
                    {
                        var newSeries = new LineSeries();
                        newSeries.Title = yLabel;
                        newSeries.IsVisible = true;
                        allSeries[yLabel] = newSeries;
                    }

                    allSeries[yLabel].Points.Add(new DataPoint((double)xValue, (double)yValue));
                }
            }

            //===== add series to plot model
            foreach (var series in allSeries)
                plotModel.Series.Add(series.Value);

            return plotModel;
        }
    }
}

//==============================================================================
// end of file