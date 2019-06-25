//==============================================================================
// Project:     TuringTrader, report templates
// Name:        SimpleReport
// Description: C# report template for SimpleReport
// History:     2019vi22, FUB, created
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
    /// C# report template for SimpleReport.
    /// </summary>
    public class SimpleReport : ReportTemplate
    {
        #region private PlotModel RenderNavAndDrawdown(string selectedChart)
        /// <summary>
        /// Specialized chart rendering NAV and drawdown logarithmically
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        private PlotModel RenderNavAndDrawdown(string selectedChart)
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
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LogarithmicAxis();
            yAxis.Position = AxisPosition.Right;
            yAxis.StartPosition = 0.25;
            yAxis.EndPosition = 1.0;
            yAxis.Key = "y";

            var ddAxis = new LinearAxis();
            ddAxis.Position = AxisPosition.Right;
            ddAxis.StartPosition = 0.0;
            ddAxis.EndPosition = 0.25;
            ddAxis.Key = "dd";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            plotModel.Axes.Add(ddAxis);

            Dictionary<string, object> normalizeValues = chartData
                .First();

            Dictionary<string, double> maxValues = new Dictionary<string, double>();

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
                    double yValue = (double)col.Value / (double)normalizeValues[yLabel];

                    maxValues[yLabel] = maxValues.ContainsKey(yLabel)
                        ? Math.Max(maxValues[yLabel], yValue)
                        : yValue;

                    double dd = (yValue - maxValues[yLabel]) / maxValues[yLabel];

                    if (!allSeries.ContainsKey(yLabel))
                    {
                        var newSeries = yLabel == row.Skip(1).First().Key
                            ? new AreaSeries
                            {
                                Color = OxyColors.Blue,
                                ConstantY2 = 1.0,
                            }
                            : new LineSeries
                            {
                                Color = OxyColors.Red,
                            };
                        newSeries.Title = yLabel;
                        newSeries.IsVisible = true;
                        newSeries.XAxisKey = "x";
                        newSeries.YAxisKey = "y";
                        allSeries[yLabel] = newSeries;

                        var ddSeries = yLabel == row.Skip(1).First().Key
                            ? new AreaSeries
                            {
                                Color = OxyColors.Blue,
                            }
                            : new LineSeries
                            {
                                Color = OxyColors.Red,
                            };
                        ddSeries.Title = "DD(" + yLabel + ")";
                        ddSeries.IsVisible = true;
                        ddSeries.XAxisKey = "x";
                        ddSeries.YAxisKey = "dd";
                        allSeries["dd" + yLabel] = ddSeries;
                    }

                    allSeries[yLabel].Points.Add(new DataPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        (double)yValue));

                    allSeries["dd" + yLabel].Points.Add(new DataPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        dd));
                }
            }

            //===== add series to plot model
            foreach (var series in allSeries)
                plotModel.Series.Add(series.Value);

            return plotModel;
        }
        #endregion
        #region private PlotModel RenderSimple(string selectedChart)
        /// <summary>
        /// Render simple x/y chart.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>plot model</returns>
        private PlotModel RenderSimple(string selectedChart)
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
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Right;
            yAxis.StartPosition = 0.25;
            yAxis.EndPosition = 1.0;
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
        #region private List<Dictionary<string, object>> RenderMetrics()
        private static string METRICS = "Strategy Metrics";
        /// <summary>
        /// Specialized table rendering strategy and benchmark metrics.
        /// </summary>
        /// <returns>table model</returns>
        private List<Dictionary<string, object>> RenderMetrics()
        {
            var retvalue = new List<Dictionary<string, object>>();
            var chartData = PlotData.First().Value;
            var nav = chartData
                .First() // first row
                .Skip(1) // second column
                .First()
                .Key;
            var benchmark = chartData
                .First() // first row
                .Skip(2) // third column
                .First()
                .Key;

            // FIXME: somehow we need to escape the carrot, as XAML treats it
            //        as a special character
            // https://stackoverflow.com/questions/6720285/how-do-i-escape-a-slash-character-in-a-wpf-binding-path-or-how-to-work-around
            string benchmark2 = benchmark.Replace("^", string.Empty);

            //===== start and end date
            DateTime startDate;
            double years;
            {
                startDate = chartData
                        .Min(row => (DateTime)row.First().Value);
                DateTime endDate = chartData
                        .Max(row => (DateTime)row.First().Value);
                years = (endDate - startDate).TotalDays / 365.25;

                retvalue.Add(new Dictionary<string, object> {
                { "Metric", "Simulation Start [date]" },
                { "Value", startDate } });

                retvalue.Add(new Dictionary<string, object> {
                { "Metric", "Simulation End [date]" },
                { "Value", endDate } });

                retvalue.Add(new Dictionary<string, object> {
                { "Metric", "Simulation Period [years]" },
                { "Value", years } });
            }

            //===== CAGR
            {
                double nav1 = (double)chartData.First()[nav];
                double nav2 = (double)chartData.Last()[nav];
                double navCagr = 100.0 * (Math.Pow(nav2 / nav1, 1.0 / years) - 1.0);

                double bench1 = (double)chartData.First()[benchmark];
                double bench2 = (double)chartData.Last()[benchmark];
                double benchCagr = 100.0 * (Math.Pow(bench2 / bench1, 1.0 / years) - 1.0);

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Compound Annual Growth Rate [%]" },
                    { nav, navCagr },
                    { benchmark2, benchCagr } });
            }

            //===== MDD
            {
                double navMax = 0.0;
                double benchMax = 0.0;
                double navDd = 0.0;
                double benchDd = 0.0;

                foreach (var row in chartData)
                {
                    navMax = Math.Max(navMax, (double)row[nav]);
                    benchMax = Math.Max(benchMax, (double)row[benchmark]);

                    navDd = Math.Max(navDd, (navMax - (double)row[nav]) / navMax);
                    benchDd = Math.Max(benchDd, (benchMax - (double)row[benchmark]) / benchMax);
                }

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Maximum Drawdown [%]" },
                    { nav, -100.0 * navDd },
                    { benchmark2, -100.0 * benchDd } });
            }

            //===== Maximum Flat Days
            {
                double navMaxValue = 0.0;
                DateTime navMaxTime = startDate;
                double navMaxFlat = 0.0;
                double benchMaxValue = 0.0;
                DateTime benchMaxTime = startDate;
                double benchMaxFlat = 0.0;

                foreach (var row in chartData)
                {
                    double navVal = (double)row[nav];
                    double benchVal = (double)row[benchmark];
                    DateTime timestamp = (DateTime)row.First().Value;

                    if (navVal > navMaxValue)
                    {
                        navMaxValue = navVal;
                        navMaxTime = timestamp;
                    }
                    else
                    {
                        navMaxFlat = Math.Max(navMaxFlat, (timestamp - navMaxTime).TotalDays);
                    }

                    if (benchVal > benchMaxValue)
                    {
                        benchMaxValue = benchVal;
                        benchMaxTime = timestamp;
                    }
                    else
                    {
                        benchMaxFlat = Math.Max(benchMaxFlat, (timestamp - benchMaxTime).TotalDays);
                    }
                }

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Maximum Flat Period [days]" },
                    { nav, navMaxFlat },
                    { benchmark2, benchMaxFlat } });
            }

            //===== Sharpe Ratio
            {
                const double riskFree = 0.02 / 12.0; // 2% per year

                List<double> navReturns = new List<double>();
                List<double> benchReturns = new List<double>();

                DateTime prevTimestamp = startDate;
                double? navPrev = null;
                double? benchPrev = null;

                foreach (var row in chartData)
                {
                    double navVal = (double)row[nav];
                    double benchVal = (double)row[benchmark];
                    DateTime timestamp = (DateTime)row.First().Value;

                    if (timestamp.Month != prevTimestamp.Month)
                    {
                        if (navPrev != null)
                        {
                            navReturns.Add(navVal / (double)navPrev - 1.0 - riskFree);
                            benchReturns.Add(benchVal / (double)benchPrev - 1.0 - riskFree);
                        }

                        prevTimestamp = timestamp;
                        navPrev = navVal;
                        benchPrev = benchVal;
                    }
                }

                double navAvgRet = navReturns.Average();
                double navVarRet = navReturns.Average(r => Math.Pow(r - navAvgRet, 2.0));
                double navSharpe = Math.Sqrt(12.0) * navAvgRet / Math.Sqrt(navVarRet);

                double benchAvgRet = benchReturns.Average();
                double benchVarRet = navReturns.Average(r => Math.Pow(r - benchAvgRet, 2.0));
                double benchSharpe = Math.Sqrt(12.0) * benchAvgRet / Math.Sqrt(benchVarRet);

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Sharpe Ratio" },
                    { nav, navSharpe },
                    { benchmark2, benchSharpe } });
            }

            //===== Beta
            {
                foreach (var row in chartData)
                {

                }
            }

            return retvalue;
        }
        #endregion
        #region private bool IsTable(string selectedChart)
        /// <summary>
        /// Determine if we should render as table.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>true for table</returns>
        private bool IsTable(string selectedChart)
        {
            return true;
        }
        #endregion

        #region public override IEnumerable<string> AvailableCharts
        /// <summary>
        /// Enumerate available charts.
        /// </summary>
        public override IEnumerable<string> AvailableCharts
        {
            get
            {
                yield return PlotData.Keys.First();

                yield return METRICS;

                foreach (string chart in PlotData.Keys.Skip(1))
                    yield return chart;

                yield break;
            }
        }
        #endregion
        #region public override object GetModel(string selectedChart)
        /// <summary>
        /// Get table or plot model for selected chart.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>model</returns>
        public override object GetModel(string selectedChart)
        {
            // 1st chart is always NAV and drawdown
            if (selectedChart == PlotData.Keys.First())
                return RenderNavAndDrawdown(selectedChart);

            // 2nd chart is always metrics
            if (selectedChart == METRICS)
                return RenderMetrics();

            // all other are either tables or tables
            if (IsTable(selectedChart))
                return PlotData[selectedChart];
            else
                return RenderSimple(selectedChart);
        }
        #endregion
    }
}

//==============================================================================
// end of file