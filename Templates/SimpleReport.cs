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
        private static string EQUITY_CURVE = "Equity Curve with Drawdown";
        private static string METRICS = "Performance Metrics";
        private static string ANNUAL_BARS = "Annual Performance";
        private static string MONTE_CARLO = "Monte Carlo Analysis";
        #endregion
        #region internal helpers
        private List<Dictionary<string, object>> NAV_BENCH_DATA { get { return PlotData.First().Value; } }
        private string METRIC_LABEL = "Metric";
        private string UNI_LABEL = "Value"; 
        private string NAV_LABEL   { get { return NAV_BENCH_DATA.First().Skip(1).First().Key; } }
        private string BENCH_LABEL { get { return NAV_BENCH_DATA.First().Skip(2).First().Key; } }

        // FIXME: somehow we need to escape the carrot, as XAML treats it
        //        as a special character
        // https://stackoverflow.com/questions/6720285/how-do-i-escape-a-slash-character-in-a-wpf-binding-path-or-how-to-work-around
        private string BENCH_LABEL_XAML { get { return BENCH_LABEL.Replace("^", string.Empty); } }

        private DateTime START_DATE { get { return (DateTime)NAV_BENCH_DATA.First().First().Value; } }
        private DateTime END_DATE { get { return (DateTime)NAV_BENCH_DATA.Last().First().Value; } }
        private double YEARS { get { return (END_DATE - START_DATE).TotalDays / 365.25; } }

        private double NAV_START   { get { return (double)NAV_BENCH_DATA.First()[NAV_LABEL];   } }
        private double NAV_END     { get { return (double)NAV_BENCH_DATA.Last()[NAV_LABEL];    } }
        private double BENCH_START { get { return (double)NAV_BENCH_DATA.First()[BENCH_LABEL]; } }
        private double BENCH_END   { get { return (double)NAV_BENCH_DATA.Last()[BENCH_LABEL];  } }
        private double NAV_CAGR    { get { return Math.Pow(NAV_END / NAV_START, 1.0 / YEARS) - 1.0; } }
        private double BENCH_CAGR  { get { return Math.Pow(BENCH_END / BENCH_START, 1.0 / YEARS) - 1.0; } }

        private Dictionary<DateTime, double> _getMonthlyReturns(string label)
        {
            var monthlyReturns = new Dictionary<DateTime, double>();
            DateTime prevTime = START_DATE;
            double? prevValue = null;

            foreach (var row in NAV_BENCH_DATA)
            {
                DateTime curTime = (DateTime)row.First().Value;
                double curValue = (double)row[label];

                if (curTime.Month != prevTime.Month)
                {
                    DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);

                    if (prevValue != null)
                        monthlyReturns[ts] = Math.Log(curValue / (double)prevValue);

                    prevTime = curTime;
                    prevValue = curValue;
                }
            }

            return monthlyReturns;
        }
        private Dictionary<DateTime, double> _navMonthlyRet = null;
        private Dictionary<DateTime, double> NAV_MONTHLY_RET { get { if (_navMonthlyRet == null) _navMonthlyRet = _getMonthlyReturns(NAV_LABEL); return _navMonthlyRet; } }

        private Dictionary<DateTime, double> _benchMonthlyRet = null;
        private Dictionary<DateTime, double> BENCH_MONTHLY_RET { get { if (_benchMonthlyRet == null) _benchMonthlyRet = _getMonthlyReturns(BENCH_LABEL); return _benchMonthlyRet; } }

        private Dictionary<DateTime, double> _rfMonthlyYield = null;
        private Dictionary<DateTime, double> RF_MONTHLY_YIELD
        {
            get
            {
                if (_rfMonthlyYield == null)
                {
                    var dsRiskFree = DataSource.New("FRED:DTB3"); // 3-Month Treasury Bill: Secondary Market Rate
                    dsRiskFree.LoadData(START_DATE, END_DATE);

                    _rfMonthlyYield = new Dictionary<DateTime, double>();
                    DateTime prevTime = START_DATE;

                    foreach (var bar in dsRiskFree.Data)
                    {
                        DateTime curTime = bar.Time;
                        double curValue = bar.Close;

                        if (curTime.Month != prevTime.Month)
                        {
                            DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);
                            _rfMonthlyYield[ts] = curValue / 100.0 / 12.0; // monthly yield

                            prevTime = curTime;
                        }
                    }
                }
                return _rfMonthlyYield;
            }
        }

        private double NAV_AVG_MONTHLY_RET   { get { return NAV_MONTHLY_RET.Average(r => r.Value); } }
        private double BENCH_AVG_MONTHLY_RET { get { return BENCH_MONTHLY_RET.Average(r => r.Value); } }

        private double NAV_STDEV_MONTHLY_RET   { get { double avg = NAV_AVG_MONTHLY_RET;  return Math.Sqrt(NAV_MONTHLY_RET.Average(r => Math.Pow(r.Value - avg, 2.0))); } }
        private double BENCH_STDEV_MONTHLY_RET { get { double avg = BENCH_AVG_MONTHLY_RET; return Math.Sqrt(BENCH_MONTHLY_RET.Average(r => Math.Pow(r.Value - avg, 2.0))); } }

        private double _getMdd(string label)
        {
            double max = -1e99;
            double mdd = 0.0;

            foreach (var row in NAV_BENCH_DATA)
            {
                double val = (double)row[label];

                max = Math.Max(max, val);
                double dd = (max - val) / max;

                mdd = Math.Max(mdd, dd);
            }

            return mdd;
        }
        private double NAV_MDD { get { return _getMdd(NAV_LABEL); } }
        private double BENCH_MDD { get { return _getMdd(BENCH_LABEL); } }

        private double _getMaxFlat(string label)
        {
            double peakValue = -1e99;
            DateTime peakTime = START_DATE;
            double maxFlat = 0.0;

            foreach (var row in NAV_BENCH_DATA)
            {
                DateTime time = (DateTime)row.First().Value;
                double value = (double)row[label];

                if (value > peakValue)
                {
                    double flatDays = (time - peakTime).TotalDays;
                    maxFlat = Math.Max(maxFlat, flatDays);

                    peakValue = value;
                    peakTime = time;
                }
            }

            return maxFlat;
        }
        private double NAV_MAX_FLAT { get { return _getMaxFlat(NAV_LABEL); } }
        private double BENCH_MAX_FLAT { get { return _getMaxFlat(BENCH_LABEL); } }

        private Dictionary<DateTime, double> _getExcReturns(string label)
        {
            Dictionary<DateTime, double> monthlyReturns = label == NAV_LABEL ? NAV_MONTHLY_RET : BENCH_MONTHLY_RET;

            return monthlyReturns
                .ToDictionary(r => r.Key, r => Math.Log(Math.Exp(r.Value) - RF_MONTHLY_YIELD[r.Key]));
        }

        private Dictionary<DateTime, double> _navMonthlyExcess = null;
        private Dictionary<DateTime, double> NAV_MONTHLY_EXCESS   { get { if (_navMonthlyExcess == null) _navMonthlyExcess = _getExcReturns(NAV_LABEL); return _navMonthlyExcess; } }
        private Dictionary<DateTime, double> _benchMonthlyExcess = null;
        private Dictionary<DateTime, double> BENCH_MONTHLY_EXCESS { get { if (_benchMonthlyExcess == null) _benchMonthlyExcess = _getExcReturns(BENCH_LABEL); return _benchMonthlyExcess; } }

        private double NAV_AVG_MONTHLY_EXCESS   { get { return NAV_MONTHLY_EXCESS.Average(r => r.Value); } }
        private double BENCH_AVG_MONTHLY_EXCESS { get { return BENCH_MONTHLY_EXCESS.Average(r => r.Value); } }

        private double NAV_STDEV_MONTHLY_EXCESS   { get { double avg = NAV_AVG_MONTHLY_EXCESS; return Math.Sqrt(NAV_MONTHLY_EXCESS.Average(r => Math.Pow(r.Value - avg, 2.0))); } }
        private double BENCH_STDEV_MONTHLY_EXCESS { get { double avg = BENCH_AVG_MONTHLY_EXCESS; return Math.Sqrt(BENCH_MONTHLY_EXCESS.Average(r => Math.Pow(r.Value - avg, 2.0))); } }

        private double NAV_SHARPE { get { return Math.Sqrt(12.0) * NAV_AVG_MONTHLY_EXCESS / NAV_STDEV_MONTHLY_EXCESS; } }
        private double BENCH_SHARPE { get { return Math.Sqrt(12.0) * BENCH_AVG_MONTHLY_EXCESS / BENCH_STDEV_MONTHLY_EXCESS; } }

        private double BETA
        {
            get
            {
                var dates = NAV_MONTHLY_RET.Keys.ToList();

                double covar = dates
                    .Sum(d => (NAV_MONTHLY_RET[d] - NAV_AVG_MONTHLY_RET) * (BENCH_MONTHLY_RET[d] - BENCH_AVG_MONTHLY_RET))
                    / (dates.Count - 1.0);

                //double benchVar = benchReturns.Values.Average(r => Math.Pow(r - benchAvgRet, 2.0));

                //double beta = covar / benchVar;

                return covar / Math.Pow(BENCH_STDEV_MONTHLY_RET, 2.0);
            }
        }

        private double _getUlcerIndex(string label)
        {
            double peak = 0.0;
            double sumDd2 = 0.0;
            int N = 0;

            foreach (var row in NAV_BENCH_DATA)
            {
                N++;
                peak = Math.Max(peak, (double)row[label]);
                sumDd2 += Math.Pow(1.0 * (peak - (double)row[label]) / peak, 2.0);
            }

            return Math.Sqrt(sumDd2 / N);
        }
        private double NAV_ULCER_INDEX   { get { return _getUlcerIndex(NAV_LABEL); } }
        private double BENCH_ULCER_INDEX { get { return _getUlcerIndex(BENCH_LABEL); } }

        private double NAV_UPI   { get { return (Math.Exp(12.0 * NAV_AVG_MONTHLY_EXCESS) - 1.0) / NAV_ULCER_INDEX; } }
        private double BENCH_UPI { get { return (Math.Exp(12.0 * BENCH_AVG_MONTHLY_EXCESS) - 1.0) / BENCH_ULCER_INDEX; } }

        private Dictionary<DateTime, double> _activeReturn = null;
        private Dictionary<DateTime, double> ACTIVE_RETURN
        {
            get
            {
                if (_activeReturn == null)
                {
                    _activeReturn = new Dictionary<DateTime, double>();
                    var dates = NAV_MONTHLY_RET.Keys;

                    foreach (var date in dates)
                        _activeReturn[date] = NAV_MONTHLY_RET[date] - BENCH_MONTHLY_RET[date];
                }
                return _activeReturn;
            }
        }

        // FIXME: results seem incorrect
        private double NAV_INFORMATION_RATIO
        {
            get
            {
                double expectedActiveReturn = ACTIVE_RETURN
                    .Average(r => r.Value);
                double trackingError = Math.Sqrt(ACTIVE_RETURN
                    .Average(r => Math.Pow(r.Value - expectedActiveReturn, 2.0)));
                return Math.Sqrt(12.0) * expectedActiveReturn / trackingError;
            }
        }
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

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Simulation Start" },
                { UNI_LABEL, string.Format("{0:MM/dd/yyyy}", START_DATE) },
                { NAV_LABEL, string.Format("{0:C2}", NAV_START) },
                { BENCH_LABEL_XAML, string.Format("{0:C2}", BENCH_START) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Simulation End" },
                { UNI_LABEL, string.Format("{0:MM/dd/yyyy}", END_DATE) },
                { NAV_LABEL, string.Format("{0:C2}", NAV_END) },
                { BENCH_LABEL_XAML, string.Format("{0:C2}", BENCH_END) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Simulation Period" },
                { "Value", string.Format("{0:F1} years", YEARS) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Compound Annual Growth Rate" },
                { NAV_LABEL, string.Format("{0:P2}", NAV_CAGR) },
                { BENCH_LABEL, string.Format("{0:P2}", BENCH_CAGR) } });

#if false
            // testing only, should be same as CAGR
            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Average Return (Annualized)" },
                { NAV_LABEL, string.Format("{0:P2}", Math.Exp(12.0 * NAV_AVG_MONTHLY_RET) - 1.0) },
                { BENCH_LABEL_XAML, string.Format("{0:P2}", Math.Exp(12.0 * BENCH_AVG_MONTHLY_RET) - 1.0) } });
#endif

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Standard Deviation of Returns (Annualized)" },
                { NAV_LABEL, string.Format("{0:P2}", Math.Exp(Math.Sqrt(12.0) * NAV_STDEV_MONTHLY_RET) - 1.0) },
                { BENCH_LABEL_XAML, string.Format("{0:P2}", Math.Exp(Math.Sqrt(12.0) * BENCH_STDEV_MONTHLY_RET) - 1.0)} });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Maximum Drawdown" },
                { NAV_LABEL, string.Format("{0:P2}", NAV_MDD) },
                { BENCH_LABEL_XAML, string.Format("{0:P2}", BENCH_MDD) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Maximum Flat Period" },
                { NAV_LABEL, string.Format("{0} days", NAV_MAX_FLAT) },
                { BENCH_LABEL_XAML, string.Format("{0} days", BENCH_MAX_FLAT) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Sharpe Ratio" },
                { NAV_LABEL, string.Format("{0:F2}", NAV_SHARPE) },
                { BENCH_LABEL_XAML, string.Format("{0:F2}", BENCH_SHARPE) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Beta" },
                { NAV_LABEL, string.Format("{0:F2}", BETA) },
                { BENCH_LABEL_XAML, "n/a" } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Ulcer Index" },
                { NAV_LABEL, string.Format("{0:P2}", NAV_ULCER_INDEX) },
                { BENCH_LABEL_XAML, string.Format("{0:P2}", BENCH_ULCER_INDEX) } });

            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Ulcer Performance Index (Martin Ratio)" },
                { NAV_LABEL, string.Format("{0:F2}", NAV_UPI) },
                { BENCH_LABEL_XAML, string.Format("{0:F2}", BENCH_UPI) } });

#if true
            retvalue.Add(new Dictionary<string, object> {
                { METRIC_LABEL, "Information Ratio" },
                { NAV_LABEL, string.Format("{0:F2}", NAV_INFORMATION_RATIO) },
                { BENCH_LABEL_XAML, "n/a" } });
#endif

            // Sortino Ratio
            // Calmar Ratio
            // Fouse Ratio
            // Sterling Ratio

            return retvalue;
        }
        #endregion
        #region private PlotModel RenderAnnualColumns()
        /// <summary>
        /// Specialized chart rendering annual columns for NAV and benchmark P&L
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        private PlotModel RenderAnnualColumns()
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
            yAxis.ExtraGridlines = new Double[] { 0 };

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
        #region private PlotModel RenderMonteCarlo()
        private PlotModel RenderMonteCarlo()
        {
            return null;
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
                return RenderAnnualColumns();

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