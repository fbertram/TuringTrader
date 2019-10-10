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
        private const string EQUITY_CURVE = "Equity Curve with Drawdown";
        private const string METRICS = "Performance Metrics";
        private const string ANNUAL_BARS = "Annual Performance";
        private const string RETURN_DISTRIBUTION = "Cumulative Distribution of Returns";
        private const string MONTE_CARLO = "Monte-Carlo Analysis";
        private string METRIC_LABEL = "Metric";
        private string UNI_LABEL = "";
        private const double DISTR_CUTOFF = 0.495;
        private const int NUM_MONTE_CARLO_SIMS = 1000;
        #endregion
        #region internal helpers
        private string FIRST_CHART_NAME => PlotData.First().Key;
        private List<Dictionary<string, object>> FIRST_CHART => PlotData.First().Value;

        private string X_LABEL => FIRST_CHART.First().First().Key; 
        private Type X_TYPE => FIRST_CHART.First().First().Value.GetType();

        private List<string> ALL_Y_LABELS => FIRST_CHART.First().Keys.Skip(1).ToList(); 
        private int NUM_Y_LABELS => ALL_Y_LABELS.Count();
        private string FIRST_Y_LABEL => ALL_Y_LABELS.First();
        private string BENCH_Y_LABEL => ALL_Y_LABELS.Last();

        private DateTime START_DATE => (DateTime)FIRST_CHART.First()[X_LABEL];
        private DateTime END_DATE => (DateTime)FIRST_CHART.Last()[X_LABEL];
        private double YEARS => (END_DATE - START_DATE).TotalDays / 365.25;

        private Dictionary<DateTime, double> GET_SERIES(string yLabel)
        {
            var xLabel = X_LABEL;
            var series = new Dictionary<DateTime, double>();
            foreach (var row in FIRST_CHART)
            {
                if (row.ContainsKey(yLabel))
                    series[(DateTime)row[xLabel]] = (double)row[yLabel];
            }
            return series;
        }

        private double START_VALUE(string label) => GET_SERIES(label).First().Value;
        private double END_VALUE(string label) => GET_SERIES(label).Last().Value;

        private double CAGR(string label) => Math.Pow(END_VALUE(label) / START_VALUE(label), 1.0 / YEARS) - 1.0;
        private double MDD(string label)
        {
            double max = -1e99;
            double mdd = 0.0;

            foreach (var row in FIRST_CHART)
            {
                double val = (double)row[label];

                max = Math.Max(max, val);
                double dd = (max - val) / max;

                mdd = Math.Max(mdd, dd);
            }

            return mdd;
        }

        private Dictionary<DateTime, double> MONTHLY_RETURNS(string label)
        {
            var monthlyReturns = new Dictionary<DateTime, double>();
            DateTime prevTime = START_DATE;
            double? prevValue = null;

            foreach (var row in FIRST_CHART)
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

        double AVG_MONTHLY_RETURN(string label) => MONTHLY_RETURNS(label).Values.Average(r => r);
        double STD_MONTHLY_RETURN(string label)
        {
            double avg = AVG_MONTHLY_RETURN(label);
            return Math.Sqrt(MONTHLY_RETURNS(label).Values.Average(r => Math.Pow(r - avg, 2.0)));
        }
        double SHARPE_RATIO(string label)
        {
            var exc = EXC_MONTHLY_RETURNS(label);
            var avg = exc.Values.Average(r => r);
            var var = exc.Values.Average(r => Math.Pow(r - avg, 2.0));

            return Math.Sqrt(12.0) * avg / Math.Sqrt(var);
        }

        // FIXME: somehow we need to escape the carrot, as XAML treats it
        //        as a special character
        // https://stackoverflow.com/questions/6720285/how-do-i-escape-a-slash-character-in-a-wpf-binding-path-or-how-to-work-around
        private string XAML_LABEL(string raw) { return raw.Replace("^", string.Empty); }

        private Dictionary<DateTime, double> _rfMonthlyYield = null;
        private Dictionary<DateTime, double> RF_MONTHLY_RETURNS
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

        private double MAX_FLAT_DAYS(string label)
        {
            double peakValue = -1e99;
            DateTime peakTime = START_DATE;
            double maxFlat = 0.0;

            foreach (var row in FIRST_CHART)
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

        private Dictionary<DateTime, double> EXC_MONTHLY_RETURNS(string label)
        {
            Dictionary<DateTime, double> monthlyReturns = MONTHLY_RETURNS(label);

            return monthlyReturns
                .ToDictionary(r => r.Key, r => Math.Log(Math.Exp(r.Value) - RF_MONTHLY_RETURNS[r.Key]));
        }

        private double BETA(string seriesLabel, string benchLabel)
        {
            var series = MONTHLY_RETURNS(seriesLabel);
            var seriesAvg = series.Values.Average(v => v);
            var bench = MONTHLY_RETURNS(benchLabel);
            var benchAvg = bench.Values.Average(v => v);
            var benchVar = bench.Values.Average(v => Math.Pow(v - benchAvg, 2.0));
            var dates = series.Keys.ToList();

            double covar = dates
                .Sum(d => (series[d] - seriesAvg) * (bench[d] - benchAvg))
                / (dates.Count - 1.0);

            return covar / benchVar;
        }

        private double ULCER_INDEX(string label)
        {
            double peak = 0.0;
            double sumDd2 = 0.0;
            int N = 0;

            foreach (var row in FIRST_CHART)
            {
                N++;
                peak = Math.Max(peak, (double)row[label]);
                sumDd2 += Math.Pow(1.0 * (peak - (double)row[label]) / peak, 2.0);
            }

            return Math.Sqrt(sumDd2 / N);
        }

        private double ULCER_PERFORMANCE_INDEX(string label)
        {
            double perf = Math.Exp(12.0 * EXC_MONTHLY_RETURNS(label).Values.Average(r => r)) - 1.0;
            double ulcer = ULCER_INDEX(label);
            return perf / ulcer;
        }

        private List<double> DISTR_DAILY_RETURNS(string label)
        {
            List<double> returns = new List<double>();
            double? prevValue = null;

            foreach (var row in FIRST_CHART)
            {
                double curValue = (double)row[label];

                if (prevValue != null)
                    returns.Add(Math.Log(curValue / (double)prevValue));

                prevValue = curValue;
            }

            return returns
                .OrderBy(v => v)
                .ToList();
        }

        private double DISTR_RET_PROBABILITY(List<double> distr, double val)
        {
            var less = distr
                .Where(v => v <= val)
                .ToList();
            var more = distr
                .Where(v => v >= val)
                .ToList();

            var pLess = (double)less.Count() / distr.Count;
            var vLess = less.Max();
            var pMore = 1.0 - (double)more.Count() / distr.Count;
            var vMore = more.Min();

            double p = pLess;

            if (val > vLess)
                p += (val - vLess) / (vMore - vLess) * (pMore - pLess);

            return p;
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
            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = PlotData.Keys.First();
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = new DateTimeAxis();
            xAxis.Title = "Date";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LogarithmicAxis();
            yAxis.Title = "Relative Equity";
            yAxis.Position = AxisPosition.Right;
            yAxis.StartPosition = 0.35;
            yAxis.EndPosition = 1.0;
            yAxis.Key = "y";

            var ddAxis = new LinearAxis();
            ddAxis.Title = "Drawdown [%]";
            ddAxis.Position = AxisPosition.Right;
            ddAxis.StartPosition = 0.0;
            ddAxis.EndPosition = 0.30;
            ddAxis.Key = "dd";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            plotModel.Axes.Add(ddAxis);

            //===== create series
            for (int i = 0; i < NUM_Y_LABELS; i++)
            {
                string yLabel = ALL_Y_LABELS[i];
                var series = GET_SERIES(yLabel);
                var color = SeriesColors[i];

                var eqSeries = (yLabel == FIRST_Y_LABEL && NUM_Y_LABELS <= 2)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = color,
                        ConstantY2 = 1.0,
                    }
                    : new LineSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                    };

                plotModel.Series.Add(eqSeries);

                var ddSeries = (yLabel == FIRST_Y_LABEL && NUM_Y_LABELS <= 2)
                    ? new AreaSeries
                    {
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "dd",
                        Color = color,
                        Fill = color,
                    }
                    : new LineSeries
                    {
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "dd",
                        Color = color,
                    };

                plotModel.Series.Add(ddSeries);

                double max = 0.0;
                double y0 = START_VALUE(yLabel);

                foreach (var point in series)
                {
                    var x = point.Key;
                    var y = point.Value;
                    max = Math.Max(max, y);
                    double dd = (y - max) / max;

                    eqSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(x),
                        (double)y / y0));

                    ddSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(x),
                        100.0 * dd));
                }
            }

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
            // TODO: this code needs some cleanup!
            // we want to make sure that meaningful metrics 
            // are generated for an arbitrary number of series

            var retvalue = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = null;

            row = new Dictionary<String, object>();
            row[METRIC_LABEL] = "Simulation Start";
            row[UNI_LABEL] = string.Format("{0:MM/dd/yyyy}", START_DATE);
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:C2}", START_VALUE(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Simulation End";
            row[UNI_LABEL] = string.Format("{0:MM/dd/yyyy}", END_DATE);
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:C2}", END_VALUE(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Simulation Period";
            row[UNI_LABEL] = string.Format("{0:F1} years", YEARS);
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Compound Annual Growth Rate";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:P2}", CAGR(label));
            retvalue.Add(row);

#if false
            // testing only, should be same as CAGR
            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Average Return (Monthly, Annualized)";
            foreach (var label in _getLabels())
                row[XAML_LABEL(label)] = string.Format("{0:P2}", Math.Sqrt(12.0) * _getStdMonthlyReturns(label));
            retvalue.Add(row);
#endif

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Stdev of Returns (Monthly, Annualized)";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:P2}",
                    Math.Exp(Math.Sqrt(12.0 * MONTHLY_RETURNS(label).Values.Average(r => r * r))) - 1.0);
            retvalue.Add(row);


            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Maximum Drawdown";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:P2}", MDD(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Maximum Flat Days";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0} days", MAX_FLAT_DAYS(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Sharpe Ratio (Monthly, Annualized)";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:F2}", SHARPE_RATIO(label));
            retvalue.Add(row);

            if (NUM_Y_LABELS >= 2)
            {
                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = "Beta (To Benchmark, Monthly)";
                foreach (var label in ALL_Y_LABELS)
                    row[XAML_LABEL(label)] = label == BENCH_Y_LABEL
                        ? "- benchmark -"
                        : string.Format("{0:F2}", BETA(label, BENCH_Y_LABEL));
                retvalue.Add(row);

            }

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Ulcer Index";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:P2}", ULCER_INDEX(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Ulcer Performance Index (Martin Ratio)";
            foreach (var label in ALL_Y_LABELS)
                row[XAML_LABEL(label)] = string.Format("{0:F2}", ULCER_PERFORMANCE_INDEX(label));
            retvalue.Add(row);

            // Information Ratio
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
            //===== create annual bars
            Dictionary<int, Dictionary<string, double>> yearlyBars = new Dictionary<int, Dictionary<string, double>>();
            foreach (var label in ALL_Y_LABELS)
            {
                var series = GET_SERIES(label);
                DateTime lastTime = series.Last().Key;

                int? currentYear = null;
                double prevYearClose = 0.0;
                double prevDayClose = 0.0;
                foreach (var point in series)
                {
                    if (currentYear == null)
                    {
                        currentYear = point.Key.Year;
                        prevDayClose = point.Value;
                        prevYearClose = point.Value;
                    }

                    if (currentYear < point.Key.Date.Year
                    || point.Key == lastTime)
                    {
                        double refValue = point.Key == lastTime ? point.Value : prevDayClose;
                        double pnl = 100.0 * (refValue / (double)prevYearClose - 1.0);

                        if (!yearlyBars.ContainsKey((int)currentYear))
                            yearlyBars[(int)currentYear] = new Dictionary<string, double>();

                        yearlyBars[(int)currentYear][label] = pnl;
                        prevYearClose = prevDayClose;
                        currentYear = point.Key.Year;
                    }

                    prevDayClose = point.Value;
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
            yAxis.Title = "Annual P&L [%]";
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";
            yAxis.ExtraGridlines = new Double[] { 0 };

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            foreach (var y in yearlyBars.Keys)
                xAxis.Labels.Add(y.ToString());

            //===== create series
            for (int i = 0; i < NUM_Y_LABELS; i++)
            {
                string yLabel = ALL_Y_LABELS[i];
                var color = SeriesColors[i];

                var colSeries = (yLabel == FIRST_Y_LABEL || NUM_Y_LABELS > 2)
                    ? new ColumnSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        FillColor = color,
                    }
                    : new ColumnSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        StrokeColor = color,
                        StrokeThickness = 1,
                        FillColor = OxyColors.White,
                    };

                plotModel.Series.Add(colSeries);

                foreach (var y in yearlyBars.Keys)
                {
                    var pnl = yearlyBars[y].ContainsKey(yLabel)
                        ? yearlyBars[y][yLabel]
                        : 0.0;

                    colSeries.Items.Add(new ColumnItem(pnl));
                }
            }

            return plotModel;
        }
        #endregion
        #region private PlotModel RenderReturnDistribution()
        private PlotModel RenderReturnDistribution()
        {
            //===== create distributions
            Dictionary<string, List<double>> distributions = new Dictionary<string, List<double>>();
            foreach (var label in ALL_Y_LABELS)
                distributions[label] = DISTR_DAILY_RETURNS(label);

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = RETURN_DISTRIBUTION;
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = new LinearAxis();
            xAxis.Title = "Probability [%]";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Title = "Daily Log-Return [%]";
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            for (int s = 0; s < NUM_Y_LABELS; s++)
            {
                string yLabel = distributions.Keys.Skip(s).First();
                OxyColor color = SeriesColors[s];

                var distrSeries = (yLabel == FIRST_Y_LABEL && NUM_Y_LABELS <= 2)
                ? new AreaSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y",
                    Color = color,
                    Fill = color,
                    ConstantY2 = 0.0,
                }
                : new LineSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y",
                    Color = color,
                };

                plotModel.Series.Add(distrSeries);

                List<double> distribution = distributions[yLabel];

                for (int i = 0; i < distribution.Count; i++)
                {
                    double p = (double)i / distribution.Count;

                    if (Math.Abs(p - 0.5) < DISTR_CUTOFF)
                        distrSeries.Points.Add(new DataPoint(
                            100.0 * p, 
                            100.0 * distribution[i]));
                }
            }

            return plotModel;
        }
        #endregion
        #region private PlotModel RenderMonteCarlo()
        private PlotModel RenderMonteCarlo()
        {
            //===== create distributions
            Dictionary<string, List<double>> distributions = new Dictionary<string, List<double>>();
            foreach (var label in ALL_Y_LABELS)
                distributions[label] = DISTR_DAILY_RETURNS(label);

            //===== create Monte-Carlo simulations
            Random rnd = new Random();
            Dictionary<string, List<double>> allCagr = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> allMdd = new Dictionary<string, List<double>>();
            foreach (var s in distributions.Keys)
            {
                List<double> distribution = distributions[s];
                List<double> simsCagr = new List<double>();
                List<double> simsMdd = new List<double>();

                for (int n = 0; n < NUM_MONTE_CARLO_SIMS; n++)
                {
                    //--- create new equity curve
                    List<double> equityCurve = new List<double>();
                    equityCurve.Add(1.0);

                    for (int t = 0; t < distribution.Count; t++)
                    {
                        int idx = rnd.Next(distribution.Count);
                        double logReturn = distribution[idx];

                        equityCurve.Add(equityCurve.Last() * Math.Exp(logReturn));
                    }

                    //--- calculate CAGR
                    double cagr = Math.Pow(equityCurve.Last(), 1.0 / YEARS) - 1.0;
                    simsCagr.Add(cagr);

                    //--- calculate MDD
                    double peak = 0.0;
                    double mdd = 0.0;

                    foreach (var nav in equityCurve)
                    {
                        peak = Math.Max(peak, nav);
                        mdd = Math.Max(mdd, (peak - nav) / peak);
                    }

                    simsMdd.Add(mdd);
                }

                //--- create distributions
                allCagr[s] = simsCagr
                    .OrderBy(x => x)
                    .ToList();

                allMdd[s] = simsMdd
                    .OrderBy(x => x)
                    .ToList();
            }

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = "Monte-Carlo Analysis of Returns and Drawdowns";
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = new LinearAxis();
            xAxis.Title = "Probability [%]";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis1 = new LinearAxis();
            yAxis1.Title = "CAGR [%]";
            yAxis1.Position = AxisPosition.Right;
            yAxis1.StartPosition = 0.52;
            yAxis1.EndPosition = 1.0;
            yAxis1.Key = "y1";

            var yAxis2 = new LinearAxis();
            yAxis2.Title = "MDD [%]";
            yAxis2.Position = AxisPosition.Right;
            yAxis2.StartPosition = 0.0;
            yAxis2.EndPosition = 0.48;
            yAxis2.Key = "y2";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis1);
            plotModel.Axes.Add(yAxis2);

            //===== add series
            for (int s = 0; s < NUM_Y_LABELS; s++)
            {
                string yLabel = distributions.Keys.Skip(s).First();
                OxyColor color = SeriesColors[s];

                //--- CAGR
                var cagrSeries = (yLabel == FIRST_Y_LABEL && NUM_Y_LABELS <= 2)
                ? new AreaSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    Color = color,
                    Fill = color,
                    ConstantY2 = 0.0,
                }
                : new LineSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    Color = color,
                };

                plotModel.Series.Add(cagrSeries);

                List<double> distrCagr = allCagr[yLabel];
                for (int i = 0; i < distrCagr.Count; i++)
                {
                    double p = (double)i / distrCagr.Count;

                    if (Math.Abs(p - 0.5) < DISTR_CUTOFF)
                        cagrSeries.Points.Add(new DataPoint(
                            100.0 * p,
                            100.0 * distrCagr[i]));
                }

                //--- MDD
                var mddSeries = (yLabel == FIRST_Y_LABEL && NUM_Y_LABELS <= 2)
                ? new AreaSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y2",
                    Color = color,
                    Fill = color,
                    ConstantY2 = 0.0,
                }
                : new LineSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y2",
                    Color = color,
                };

                plotModel.Series.Add(mddSeries);

                List<double> distrMdd = allMdd[yLabel];
                for (int i = 0; i < distrMdd.Count; i++)
                {
                    double p = (double)i / distrMdd.Count;

                    if (Math.Abs(p - 0.5) < DISTR_CUTOFF)
                        mddSeries.Points.Add(new DataPoint(
                            100.0 * p,
                            -100.0 * distrMdd[i]));
                }

                //--- CAGR marker
                var cagrMarker = new ScatterSeries
                {
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 5,
                    MarkerStroke = color,
                    MarkerFill = OxyColors.White,
                };

                plotModel.Series.Add(cagrMarker);

                var cagrValue = CAGR(yLabel);
                var cagrProb = DISTR_RET_PROBABILITY(distrCagr, cagrValue);
                cagrMarker.Points.Add(new ScatterPoint(100.0 * cagrProb, 100.0 * cagrValue));

                //--- mdd marker
                var mddMarker = new ScatterSeries
                {
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y2",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 5,
                    MarkerStroke = color,
                    MarkerFill = OxyColors.White,
                };

                plotModel.Series.Add(mddMarker);

                var mddValue = MDD(yLabel);
                var mddProb = DISTR_RET_PROBABILITY(distrMdd, mddValue);
                mddMarker.Points.Add(new ScatterPoint(100.0 * mddProb, -100.0 * mddValue));
            }

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
                yield return RETURN_DISTRIBUTION;
                yield return MONTE_CARLO;

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
            switch(selectedChart)
            {
                case EQUITY_CURVE:
                    return RenderNavAndDrawdown();

                case METRICS:
                    return RenderMetrics();

                case ANNUAL_BARS:
                    return RenderAnnualColumns();

                case RETURN_DISTRIBUTION:
                    return RenderReturnDistribution();

                case MONTE_CARLO:
                    return RenderMonteCarlo();

                default:
                    if (IsTable(selectedChart))
                        return RenderTable(selectedChart);
                    else if (IsScatter(selectedChart))
                        return RenderScatter(selectedChart);
                    else
                        return RenderSimple(selectedChart);
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file