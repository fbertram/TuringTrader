//==============================================================================
// Project:     TuringTrader, report templates
// Name:        SimpleReport
// Description: C# report template for SimpleReport
// History:     2019vi22, FUB, created
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
        #region internal data
        private static string EQUITY_CURVE = "Equity Curve";
        private static string METRICS = "Performance Metrics";
        private static string ANNUAL_BARS = "Annual Performance";
        #endregion

        #region private PlotModel RenderNavAndDrawdown()
        /// <summary>
        /// Specialized chart rendering NAV and drawdown logarithmically
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        private PlotModel RenderNavAndDrawdown()
        {
            //===== get plot data
            var chartData = PlotData.First().Value;

            string xLabel = chartData
                .First()      // first row is as good as any
                .First().Key; // first column is x-axis

            object xValue = chartData
                .First()        // first row is as good as any
                .First().Value; // first column is x-axis

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = PlotData.Keys.First();
            plotModel.LegendPosition = LegendPosition.LeftTop;
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
            OxyColor navColor = _seriesColors[0]; //OxyColor.FromRgb(0x44, 0x72, 0xc4); // OxyColors.Blue
            OxyColor benchColor = _seriesColors[1]; //OxyColor.FromRgb(0xeb, 0x7f, 0x34); // OxyColors.Orange
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
                                Color = navColor,
                                Fill = navColor,
                                ConstantY2 = 1.0,
                            }
                            : new LineSeries
                            {
                                Color = benchColor,
                            };
                        newSeries.Title = yLabel;
                        newSeries.IsVisible = true;
                        newSeries.XAxisKey = "x";
                        newSeries.YAxisKey = "y";
                        allSeries[yLabel] = newSeries;

                        var ddSeries = yLabel == row.Skip(1).First().Key
                            ? new AreaSeries
                            {
                                Color = navColor,
                                Fill = navColor,
                            }
                            : new LineSeries
                            {
                                Color = benchColor,
                            };
                        // ddSeries.Title = "DD(" + yLabel + ")";
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
        #region private List<Dictionary<string, object>> RenderMetrics()
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
                    { "Metric", "Simulation Start" },
                    { "Value", string.Format("{0:MM/dd/yyyy}", startDate) },
                    { nav, string.Format("{0:C2}", (double)chartData.First()[nav]) },
                    { benchmark2, string.Format("{0:C2}", (double)chartData.First()[benchmark]) } });

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Simulation End" },
                    { "Value", string.Format("{0:MM/dd/yyyy}", endDate) },
                    { nav, string.Format("{0:C2}", (double)chartData.Last()[nav]) },
                    { benchmark2, string.Format("{0:C2}", (double)chartData.Last()[benchmark]) } });

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Simulation Period" },
                    { "Value", string.Format("{0:F1} years", years) } });
            }

            //===== CAGR
            {
                double nav1 = (double)chartData.First()[nav];
                double nav2 = (double)chartData.Last()[nav];
                double navCagr = Math.Pow(nav2 / nav1, 1.0 / years) - 1.0;

                double bench1 = (double)chartData.First()[benchmark];
                double bench2 = (double)chartData.Last()[benchmark];
                double benchCagr = Math.Pow(bench2 / bench1, 1.0 / years) - 1.0;

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Compound Annual Growth Rate" },
                    { nav, string.Format("{0:P2}", navCagr) },
                    { benchmark2, string.Format("{0:P2}", benchCagr) } });
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
                    { "Metric", "Maximum Drawdown" },
                    { nav, string.Format("{0:P2}", navDd) },
                    { benchmark2, string.Format("{0:P2}", benchDd) } });
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
                    { "Metric", "Maximum Flat Period" },
                    { nav, string.Format("{0} days", navMaxFlat) },
                    { benchmark2, string.Format("{0} days", benchMaxFlat) } });
            }

            //===== Sharpe Ratio
            List<double> navReturns = new List<double>();
            List<double> benchReturns = new List<double>();
            double navAvgRet;
            double benchAvgRet;
            {
                const bool calcMonthly = true;

                DateTime prevTimestamp = startDate;
                double? navPrev = null;
                double? benchPrev = null;

                // create list of returns
                foreach (var row in chartData)
                {
                    DateTime timestamp = (DateTime)row.First().Value;
                    double navVal = (double)row[nav];
                    double benchVal = (double)row[benchmark];

                    if (!calcMonthly || calcMonthly && timestamp.Month != prevTimestamp.Month)
                    {
                        if (navPrev != null && benchPrev != null)
                        {
#if true
                            // use log-returns. note that we don't use the risk free rate here.
                            navReturns.Add(Math.Log10(navVal / (double)navPrev));
                            benchReturns.Add(Math.Log10(benchVal / (double)benchPrev));
#else
                            const double riskFree = 0.0;
                            navReturns.Add(navVal / (double)navPrev - 1.0 - riskFree);
                            benchReturns.Add(benchVal / (double)benchPrev - 1.0 - riskFree);
#endif
                        }

                        prevTimestamp = timestamp;
                        navPrev = navVal;
                        benchPrev = benchVal;
                    }
                }

                navAvgRet = navReturns.Average();
                double navStdRet = Math.Sqrt(navReturns.Average(r => Math.Pow(r - navAvgRet, 2.0)));
                double navSharpe = Math.Sqrt(calcMonthly ? 12.0 : 252.0) * navAvgRet / navStdRet;

                benchAvgRet = benchReturns.Average();
                double benchStdRet = Math.Sqrt(benchReturns.Average(r => Math.Pow(r - benchAvgRet, 2.0)));
                double benchSharpe = Math.Sqrt(calcMonthly ? 12.0 : 252.0) * benchAvgRet / benchStdRet;

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Sharpe Ratio" },
                    { nav, string.Format("{0:F2}", navSharpe) },
                    { benchmark2, string.Format("{0:F2}", benchSharpe) } });
            }

            //===== Beta
            {
                int numBars = navReturns.Count();

                double covar = Enumerable.Range(0, numBars)
                    .Sum(i => (navReturns[i] - navAvgRet) * (benchReturns[i] - benchAvgRet))
                    / (numBars - 1.0);

                double benchVar = benchReturns.Average(r => Math.Pow(r - benchAvgRet, 2.0));

                double beta = covar / benchVar;

                retvalue.Add(new Dictionary<string, object> {
                    { "Metric", "Beta" },
                    { nav, string.Format("{0:F2}", beta) },
                    { benchmark2, "n/a" } });
            }

            return retvalue;
        }
        #endregion
        #region private PlotModel RenderAnnualBars()
        /// <summary>
        /// Specialized chart rendering NAV and drawdown logarithmically
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        private PlotModel RenderAnnualBars()
        {
            //===== get plot data
            var chartData = PlotData.First().Value;
            var nav = chartData
                .First() // first row
                .Skip(1) // second column
                .First()
                .Key;
            var bench = chartData
                .First() // first row
                .Skip(2) // third column
                .First()
                .Key;

            //===== create annual bars
            Dictionary<int, Tuple<double, double>> annualBars = new Dictionary<int, Tuple<double, double>>();

            DateTime timePrev = default(DateTime);
            double navPrev = 0.0;
            double benchPrev = 0.0;
            DateTime timeLast = (DateTime)chartData.Last().First().Value;

            foreach (var row in chartData)
            {
                DateTime timeNow = (DateTime)row.First().Value;
                double navNow = (double)row[nav];
                double benchNow = (double)row[bench];

                if (timePrev == default(DateTime))
                {
                    timePrev = timeNow;
                    navPrev = navNow;
                    benchPrev = benchNow;
                }

                if (timeNow.Date.Year != timePrev.Date.Year
                || timeNow == timeLast)
                {
                    int year = timePrev.Date.Year;
                    double navGain = 100.0 * (navNow / navPrev - 1.0);
                    double benchGain = 100.0 * (benchNow / benchPrev - 1.0);

                    annualBars.Add(year, new Tuple<double, double>(navGain, benchGain));

                    timePrev = timeNow;
                    navPrev = navNow;
                    benchPrev = benchNow;
                }
            }

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = ANNUAL_BARS;
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            var xAxis = new CategoryAxis();
            xAxis.Title = "Year";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            OxyColor navColor = _seriesColors[0];
            OxyColor benchColor = _seriesColors[1];

            var navSeries = new ColumnSeries();
            navSeries.Title = nav;
            navSeries.IsVisible = true;
            navSeries.XAxisKey = "x";
            navSeries.YAxisKey = "y";
            navSeries.FillColor = navColor;

            var benchSeries = new ColumnSeries();
            benchSeries.Title = bench;
            benchSeries.IsVisible = true;
            benchSeries.XAxisKey = "x";
            benchSeries.YAxisKey = "y";
            benchSeries.FillColor = benchColor;

            foreach (var row in annualBars)
            {
                int year = row.Key;
                double navGain = row.Value.Item1;
                double benchGain = row.Value.Item2;

                xAxis.Labels.Add(year.ToString());

                navSeries.Items.Add(new ColumnItem(navGain));
                benchSeries.Items.Add(new ColumnItem(benchGain));
            }

            //===== add series to plot model
            plotModel.Series.Add(navSeries);
            plotModel.Series.Add(benchSeries);

            return plotModel;
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
                yield return EQUITY_CURVE;
                yield return METRICS;
                yield return ANNUAL_BARS;

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
            if (selectedChart == EQUITY_CURVE)
                return RenderNavAndDrawdown();

            // 2nd chart is always metrics
            if (selectedChart == METRICS)
                return RenderMetrics();

            // 3rd chart is always annual bars
            if (selectedChart == ANNUAL_BARS)
                return RenderAnnualBars();

            // all other are either tables or tables
            if (IsTable(selectedChart))
                return RenderTable(selectedChart);
            else
                return RenderSimple(selectedChart);
        }
        #endregion
    }
}

//==============================================================================
// end of file