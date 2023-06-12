//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        ReportTemplate
// Description: Base class for C# report templates.
// History:     2019v28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Base class for C# plotter templates.
    /// </summary>
    public abstract class _ReportTemplate
    {
        #region chart configuration
        /// <summary>
        /// Configure report type. If this property is set to false,
        /// every plotter chart will result in one sheet (table, chart,
        /// or scatter). With this property set to true, additional sheets
        /// with metrics, annual bars, and monte-carlo simulations will
        /// be rendered.
        /// </summary>
        protected virtual bool CFG_IS_REPORT => false;
        /// <summary>
        /// Configure vertical axis. If this property is set to true,
        /// the charts scale the vertical axis logarithmically.
        /// </summary>
        protected virtual bool CFG_IS_LOG => false;
        /// <summary>
        /// Configure chart legend. If this property is set to false,
        /// the chart will not have a legend.
        /// </summary>
        protected virtual bool CFG_HAS_LEGEND => true;
        /// <summary>
        /// Configure tracking chart. If this property is set to true,
        /// the chart will calculate all tracking graphs against the
        /// last series as the benchmark. Otherwise, it will chart
        /// the first series against all others as benchmarks.
        /// </summary>
        protected virtual bool CFG_TRACKING_TO_LAST => true;
        /// <summary>
        /// Configure calculation of beta. If this property is set to false,
        /// beta is calculated vs the benchmark. If this property is set to true,
        /// beta is calculated vs the S&P 500.
        /// </summary>
        protected virtual bool CFG_BETA_TO_SPX => false;
        /// <summary>
        /// Configure logo. If this property is null, charts will be rendered
        /// without logo. If this property is set, the logo will be loaded from
        /// the template directory and placed in the lower right corner of
        /// the chart.
        /// </summary>
        protected virtual string CFG_LOGO => null;
        /// <summary>
        /// Configure colors.
        /// </summary>
        protected virtual List<OxyColor> CFG_COLORS => _cfgColors;
        private readonly List<OxyColor> _cfgColors = new List<OxyColor>
        {
            OxyColor.FromRgb(68, 114, 196),  // blue
            OxyColor.FromRgb(237, 125, 49),  // orange
            OxyColor.FromRgb(165, 165, 165), // grey
            OxyColor.FromRgb(255, 192, 0),   // yellow
            OxyColor.FromRgb(91, 155, 213),  // light blue
            OxyColor.FromRgb(112, 173, 71),  // green
            OxyColor.FromRgb(38, 68, 120),   // dark blue
            OxyColor.FromRgb(158, 72, 14),   // brown
            OxyColor.FromRgb(99, 99, 99),    // dark grey
            OxyColor.FromRgb(153, 115, 0),   // swampy green
            OxyColor.FromRgb(37, 94, 145),   // blue grey
            OxyColor.FromRgb(67, 104, 43),   // algae green
            OxyColor.FromRgb(105, 142, 208), // light blue
            OxyColor.FromRgb(241, 151, 90),  // orange tan
            OxyColor.FromRgb(183, 183, 183), // light grey
            OxyColor.FromRgb(255, 205, 51),  // light yellow
            OxyColor.FromRgb(124, 175, 221), // light blue
            OxyColor.FromRgb(140, 193, 104), // light green
            OxyColor.FromRgb(51, 90, 161),   // blue
            OxyColor.FromRgb(210, 96, 18),   // brown
            //OxyColor.FromRgb(132, 132, 132), // grey
        };
        protected virtual OxyColor CFG_COLOR0_FILL => CFG_COLORS[0];
        /// <summary>
        /// Configre graph format. Return true for area chart, and false for
        /// line charts. The default implementation shows the first colum/
        /// NAV as an area, and the following columns/ benchmarks as lines.
        /// </summary>
        protected virtual bool CFG_IS_AREA(string label) => label == _firstYLabel && _numYLabels <= 2;
        protected virtual int CFG_SCATTER_SIZE => 2;
        #endregion

        #region internal data
        private const string METRIC_LABEL = "Metric";
        private const string UNI_LABEL = "";
        private const double DISTR_CUTOFF = 0.495;
        private const int NUM_MONTE_CARLO_SIMS = 1000;
        private const string SPXTR = "$SPXTR";
        private const string RF_YIELD = "FRED:DTB3"; // 3-Month Treasury Bill: Secondary Market Rate
        #endregion
        #region internal helper functions
        protected string _firstChartName => PlotData.First().Key;
        protected List<Dictionary<string, object>> _firstChart => PlotData.First().Value;

        protected string _xLabel => _firstChart.First().First().Key;
        protected Type xType => _firstChart.First().First().Value.GetType();

        protected List<string> _yLabels => _firstChart.First().Keys.Skip(1).ToList();
        protected int _numYLabels => _yLabels.Count();
        protected string _firstYLabel => _yLabels.First();
        protected string _benchYLabel => _yLabels.Last();

        protected DateTime _startDate => (DateTime)_firstChart.First()[_xLabel];
        protected DateTime _endDate => (DateTime)_firstChart.Last()[_xLabel];
        protected double _numYears => (_endDate - _startDate).TotalDays / 365.25;

        protected Dictionary<DateTime, double> _getSeries(string yLabel)
        {
            var xLabel = _xLabel;
            var series = new Dictionary<DateTime, double>();
            foreach (var row in _firstChart)
            {
                if (row.ContainsKey(yLabel))
                    series[(DateTime)row[xLabel]] = (double)row[yLabel];
            }
            return series;
        }

        protected double _startValue(string label) => _getSeries(label).First().Value;
        protected double _endValue(string label) => _getSeries(label).Last().Value;

        protected double _cagr(string label) => Math.Pow(_endValue(label) / _startValue(label), 1.0 / _numYears) - 1.0;
        protected double _mdd(string label)
        {
            double max = -1e99;
            double mdd = 0.0;

            foreach (var row in _firstChart)
            {
                double val = (double)row[label];

                max = Math.Max(max, val);
                double dd = (max - val) / max;

                mdd = Math.Max(mdd, dd);
            }

            return mdd;
        }

        protected Dictionary<DateTime, double> _monthlyReturns(string label)
        {
            var monthlyReturns = new Dictionary<DateTime, double>();
            DateTime prevTime = _startDate;
            double? prevValue = null;
            double? prevMonthEndValue = null;

            foreach (var row in _firstChart)
            {
                DateTime curTime = (DateTime)row.First().Value;
                double curValue = (double)row[label];

#if false
                // code retired 05/31/2022
                // BUGBUG: this code calculates monthly returns from first-to-first
                if (curTime.Month != prevTime.Month)
                {
                    DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);

                    if (prevValue != null)
                        monthlyReturns[ts] = Math.Log(curValue / (double)prevValue);

                    prevTime = curTime;
                    prevValue = curValue;
                }
#else
                // new code 05/31/2022
                // this code calculates monthly returns from last-to-last
                if (curTime.Month != prevTime.Month)
                {
                    if (prevMonthEndValue != null)
                    {
                        DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);
                        monthlyReturns[ts] = Math.Log((double)prevValue / (double)prevMonthEndValue);
                    }

                    prevMonthEndValue = prevValue;
                }
                prevTime = curTime;
                prevValue = curValue;
#endif
            }

            return monthlyReturns;
        }

        protected Dictionary<DateTime, double> _monthlySpxReturns()
        {
            DataSource spx = DataSource.New(SPXTR);
            var data = spx.LoadData(_startDate, _endDate);

            var monthlyReturns = new Dictionary<DateTime, double>();
            DateTime prevTime = _startDate;
            double? prevValue = null;
            double? prevMonthEndValue = null;

            foreach (var bar in data)
            {
                DateTime curTime = (DateTime)bar.Time;
                double curValue = (double)bar.Close;

#if false
                if (curTime.Month != prevTime.Month)
                {
                    // code retired 05/31/2022
                    // BUGBUG: this code calculates monthly returns from first-to-first
                    DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);

                    if (prevValue != null)
                        monthlyReturns[ts] = Math.Log(curValue / (double)prevValue);

                    prevTime = curTime;
                    prevValue = curValue;
            }
#else
                // new code 05/31/2022
                // calculate monthly returns from last-to-last
                if (curTime.Month != prevTime.Month)
                {
                    if (prevMonthEndValue != null)
                    {
                        DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);
                        monthlyReturns[ts] = Math.Log((double)prevValue / (double)prevMonthEndValue);
                    }

                    prevMonthEndValue = prevValue;
                }
                prevTime = curTime;
                prevValue = curValue;
#endif
            }

            return monthlyReturns;
        }

        protected (Dictionary<double, double>, Dictionary<double, double>) _tailCagr(string label, int LEFT_TAIL, int RIGHT_TAIL)
        {
            //===== create distribution
            var monthlyDistribution = _monthlyReturns(label)
                .Select(kv => kv.Value)
                .ToList();

            //===== create swarm of price pathes
            //var TRIALS = 25000;
            var TRIALS = 500 * 100 / LEFT_TAIL;
            var YEARS = 25;

            Random rnd = new Random(0);
            var pathes = new List<List<double>>();

            for (var trials = 0; trials < TRIALS; trials++)
            {
                var path = new List<double>();
                var nav = 1.0;
                path.Add(nav); // FIXME: do all pathes need to start at 1.0?

                for (var months = 0; months < 12 * YEARS; months++)
                {
                    var monthlyRet = Math.Exp(monthlyDistribution[rnd.Next(monthlyDistribution.Count)]);
                    nav *= monthlyRet;
                    path.Add(nav);
                }

                pathes.Add(path);
            }

            //===== create 5-th percentile envelopes
            var envelopeLeft = new Dictionary<double, double>();
            var envelopeRight = new Dictionary<double, double>();

            var LEFT_PICKS = (int)Math.Round(pathes.Count * LEFT_TAIL / 100.0);
            var RIGHT_PICKS = (int)Math.Round(pathes.Count * RIGHT_TAIL / 100.0);

            for (var months = 1; months < pathes.First().Count; months++)
            {
                var years = months / 12.0;

                var sortedNavs = pathes
                    .Select(path => path[months])
                    .OrderBy(nav => nav)
                    .ToList();

                var navLeft = sortedNavs
                    .Take(LEFT_PICKS)
                    .Last();
                var navRight = sortedNavs
                    .Skip(RIGHT_PICKS)
                    .First();

                var cagrLeft = Math.Pow(navLeft, 1.0 / years) - 1.0;
                var cagrRight = Math.Pow(navRight, 1.0 / years) - 1.0;

                envelopeLeft.Add(years, cagrLeft);
                envelopeRight.Add(years, cagrRight);
            }

            return (envelopeLeft, envelopeRight);
        }

        protected Dictionary<double, double> _tailDd(string label, int LEFT_TAIL)
        {
            //===== create distribution
            var monthlyDistribution = _monthlyReturns(label)
                .Select(kv => kv.Value)
                .ToList();

            //===== create swarm of price pathes
            // * each path starts with a drawdown
            // * once the drawdown is recovered, it is pegged to zero

            var TRIALS = 250 * 100 / LEFT_TAIL * 100 / LEFT_TAIL;
            var YEARS = 25;

            Random rnd = new Random(0);
            var pathes = new List<List<double>>();

            for (var trials = 0; trials < TRIALS; trials++)
            {
                var path = new List<double>();
                var nav = 1.0;
                path.Add(0.0);
                var isDrawdown = true;

                for (var months = 0; months < 12 * YEARS; months++)
                {
                    var monthlyRet = 1.0;
                    do
                    {
                        monthlyRet = Math.Exp(monthlyDistribution[rnd.Next(monthlyDistribution.Count)]);
                    } while (months == 0 && monthlyRet > 1.0);
                    nav *= monthlyRet;
                    if (nav >= 1.0) isDrawdown = false;

                    path.Add(isDrawdown ? nav - 1.0 : 0.0);
                }

                pathes.Add(path);
            }

            //===== pick the worst drawdowns
            var worstPathes = pathes
                //.OrderBy(path => path.Min()) // deepest drawdowns
                .OrderByDescending(path => path.Sum(dd => Math.Pow(dd, 2.0))) // Ulcer Index
                .Take((int)Math.Round(pathes.Count * LEFT_TAIL / 100.0))
                .ToList();

            //===== create envelope
            var envelope = Enumerable.Range(0, 12 * YEARS)
                .ToDictionary(
                    t => t / 12.0,
                    t => worstPathes
                        .Select(path => path[t])
                        .OrderBy(dd => dd)
                        .Take((int)Math.Round(worstPathes.Count * LEFT_TAIL / 100.0))
                        .Last());

            return envelope;
        }
        protected Dictionary<double, double> _leftTailReturns(string label, double LEFT_TAIL, out Dictionary<double, double> mdd)
        {
            //===== create distribution
            var monthlyDistribution = _monthlyReturns(label)
                .Select(kv => kv.Value)
                .ToList();

            //===== create swarm of pathes
            var TRIALS = 25000;
            var LEFT_TAIL_PICKS = (int)Math.Round(TRIALS * LEFT_TAIL);
            var YEARS = 25;

            Random rnd = new Random();
            var pathes = new List<List<double>>();
            var mdds = new List<List<double>>();

            for (var trials = 0; trials < TRIALS; trials++)
            {
                pathes.Add(new List<double> { 1.0 });
                mdds.Add(new List<double> { 0.0 });
                var peak = pathes.Last().Last();

                for (var years = 0; years < YEARS; years++)
                {
                    for (var months = 0; months < 12; months++)
                    {
                        var monthlyRet = monthlyDistribution[rnd.Next(monthlyDistribution.Count)];
                        pathes.Last().Add(pathes.Last().Last() * Math.Exp(monthlyRet));

                        peak = Math.Max(peak, pathes.Last().Last());
                        mdds.Last().Add(Math.Min(mdds.Last().Last(), pathes.Last().Last() / peak - 1.0));
                    }
                }
            }

            //===== create 5-th percentile envelopes
            var envelopeNav = new Dictionary<double, double>();
            var envelopeMdd = new Dictionary<double, double>();

            envelopeNav = Enumerable.Range(0, pathes.First().Count())
                .ToDictionary(
                    t => t / 12.0, // in years
                    t =>
                    {
                        var nav = pathes
                            .Select(path => path[t])
                            .OrderBy(ret => ret)
                            .Take(LEFT_TAIL_PICKS)
                            .Last();
                        return Math.Pow(nav, 12.0 / t) - 1.0;
                    });

            envelopeMdd = Enumerable.Range(0, mdds.First().Count())
                .ToDictionary(
                    t => t / 12.0,
                    t => mdds
                        .Select(dd => dd[t])
                        .OrderBy(dd => dd) // dd is negative
                        .Take(LEFT_TAIL_PICKS)
                        .Last());

            mdd = envelopeMdd;
            return envelopeNav;
        }
        protected Dictionary<double, double> _leftTailReturns(string label, double LEFT_TAIL)
        {
            var mdd = new Dictionary<double, double>();
            return _leftTailReturns(label, LEFT_TAIL, out mdd);
        }
        protected Dictionary<double, double> _leftTailReturns(string label, double LEFT_TAIL, out double mdd)
        {
            var mdds = new Dictionary<double, double>();
            var cagrs = _leftTailReturns(label, LEFT_TAIL, out mdds);
            mdd = mdds.Max(mdd => -100.0 * mdd.Value);
            return cagrs;
        }

        protected double _avgMonthlyReturn(string label) => _monthlyReturns(label).Values.Average(r => r);
        protected double _stdMonthlyReturn(string label)
        {
            var avg = _avgMonthlyReturn(label);
            var stdev = Math.Sqrt(_monthlyReturns(label).Values.Average(r => Math.Pow(r - avg, 2.0)));
            return stdev;
        }
        protected double _sharpeRatio(string label)
        {
            var exc = _excMonthlyReturns(label);
            var avg = exc.Values.Average(r => r);
            var var = exc.Values.Average(r => Math.Pow(r - avg, 2.0));

            return Math.Sqrt(12.0) * avg / Math.Sqrt(var);
        }

        // FIXME: somehow we need to escape the carrot, as XAML treats it
        //        as a special character
        // https://stackoverflow.com/questions/6720285/how-do-i-escape-a-slash-character-in-a-wpf-binding-path-or-how-to-work-around
        private string _xamlLabel(string raw) { return raw.Replace("^", string.Empty); }

        private Dictionary<DateTime, double> _rfMonthlyReturnsCache = null;
        protected private Dictionary<DateTime, double> _rfMonthlyReturns
        {
            get
            {
                if (_rfMonthlyReturnsCache == null)
                {
                    var dsRiskFree = DataSource.New(RF_YIELD);
                    var data = dsRiskFree.LoadData(_startDate, _endDate);

                    _rfMonthlyReturnsCache = new Dictionary<DateTime, double>();
                    DateTime prevTime = _startDate;

                    foreach (var bar in data)
                    {
                        DateTime curTime = bar.Time;
                        double curValue = bar.Close;

                        if (curTime.Month != prevTime.Month)
                        {
                            DateTime ts = new DateTime(curTime.Year, curTime.Month, 1) - TimeSpan.FromDays(1);
                            _rfMonthlyReturnsCache[ts] = curValue / 100.0 / 12.0; // monthly yield

                            prevTime = curTime;
                        }
                    }
                }
                return _rfMonthlyReturnsCache;
            }
        }

        protected double _maxFlatDays(string label)
        {
            double peakValue = -1e99;
            DateTime peakTime = _startDate;
            double maxFlat = 0.0;

            foreach (var row in _firstChart)
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

        protected Dictionary<DateTime, double> _excMonthlyReturns(string label)
        {
            Dictionary<DateTime, double> rfMonthlyReturns = _rfMonthlyReturns;
            Dictionary<DateTime, double> monthlyReturns = _monthlyReturns(label)
                .Where(r => rfMonthlyReturns.ContainsKey(r.Key))
                .ToDictionary(r => r.Key, r => r.Value);

            return monthlyReturns
                .ToDictionary(r => r.Key, r => Math.Log(Math.Exp(r.Value) - _rfMonthlyReturns[r.Key]));
        }

        protected double _beta(string seriesLabel, string benchLabel)
        {
            var series = _monthlyReturns(seriesLabel);
            var seriesAvg = series.Values.Average(v => v);
            var bench = _monthlyReturns(benchLabel);
            var benchAvg = bench.Values.Average(v => v);
            var benchVar = bench.Values.Sum(v => Math.Pow(v - benchAvg, 2.0)) / (bench.Count - 1.0);
            var dates = series.Keys.ToList();

            double covar = dates
                .Sum(d => (series[d] - seriesAvg) * (bench[d] - benchAvg))
                / (dates.Count - 1.0);

            return covar / benchVar;
        }

        protected double _betaToSpx(string seriesLabel)
        {
            var bench = _monthlySpxReturns();
            var series = _monthlyReturns(seriesLabel)
                .Where(r => bench.ContainsKey(r.Key))
                .ToDictionary(r => r.Key, r => r.Value);
            var seriesAvg = series.Values.Average(v => v);
            var benchAvg = bench.Values.Average(v => v);
            var benchVar = bench.Values.Sum(v => Math.Pow(v - benchAvg, 2.0)) / (bench.Count - 1.0);
            var dates = series.Keys.ToList();

            double covar = dates
                .Sum(d => (series[d] - seriesAvg) * (bench[d] - benchAvg))
                / (dates.Count - 1.0);

            return covar / benchVar;
        }

        protected double _ulcerIndex(string label)
        {
            double peak = 0.0;
            double sumDd2 = 0.0;
            int N = 0;

            foreach (var row in _firstChart)
            {
                N++;
                peak = Math.Max(peak, (double)row[label]);
                sumDd2 += Math.Pow(1.0 * (peak - (double)row[label]) / peak, 2.0);
            }

            return Math.Sqrt(sumDd2 / N);
        }

        protected double _ulcerPerformanceIndex(string label)
        {
            double perf = _cagr(label);
            double ulcer = _ulcerIndex(label);
            double martinRatio = perf / ulcer;
            return martinRatio;
        }

        protected List<double> _returnDistribution(string label)
        {
            List<double> returns = new List<double>();
            double? prevValue = null;

            foreach (var row in _firstChart)
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

        protected double _probabilityOfReturn(List<double> distr, double val)
        {
            var less = distr
                .Where(v => v <= val)
                .ToList();
            var more = distr
                .Where(v => v >= val)
                .ToList();

            if (less.Count == 0)
                return 0.0;
            if (more.Count == 0)
                return 1.0;

            var pLess = (double)less.Count() / distr.Count;
            var vLess = less.Max();
            var pMore = 1.0 - (double)more.Count() / distr.Count;
            var vMore = more.Min();

            double p = pLess;

            if (val > vLess)
                p += (val - vLess) / (vMore - vLess) * (pMore - pLess);

            return p;
        }
        protected PlotModel _addLogo(PlotModel plotModel)
        {
            if (CFG_LOGO == null)
                return plotModel;

            string logoPath = Path.Combine(GlobalSettings.TemplatePath, CFG_LOGO);
            OxyImage logoImage = new OxyImage(File.OpenRead(logoPath));

            // Centered in plot area, filling width
            var logoAnnot = new OxyPlot.Annotations.ImageAnnotation
            {
#if false
                // centered, full width
                ImageSource = logoImage,
                Opacity = 0.2,
                Interpolate = false,
                X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Width = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
#endif
#if true
                // bottom right, 120 pixel wide
                ImageSource = logoImage,
                Opacity = 0.5,
                X = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
                Y = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
                Width = new PlotLength(120, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
#endif
            };

            plotModel.Annotations.Add(logoAnnot);

            return plotModel;
        }
        #endregion

        #region protected bool IsNumeric(object obj)
        protected bool IsNumeric(object obj)
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

            // when we get here, we did not find any non-numeric fields
            // however, if we don't have data, we call it 'table' nontheless
            return chartData.Count == 0 ? true : false;
        }
        #endregion
        #region protected bool IsScatter(string selectedChart)
        /// <summary>
        /// Determine if we should render as scatter plot.
        /// </summary>
        /// <param name="selectedChart">selected chart</param>
        /// <returns>true for scatter</returns>
        protected bool IsScatter(string selectedChart)
        {
            var chartData = PlotData[selectedChart];
            var pointsPerSeries = new Dictionary<string, int>();

            object prevX = null;
            foreach (var row in chartData)
            {
                //--- criterion #1: if the x-value goes backwards, it's a scatter
                object curX = row.First().Value;
                prevX = prevX ?? curX;

                // note how we cast everything to double here,
                // unless it is DateTime:
                if (curX.GetType() == typeof(DateTime))
                {
                    if ((DateTime)curX < (DateTime)prevX)
                        return true;
                }
                else
                {
                    if ((double)curX < (double)prevX)
                        return true;
                }

                prevX = curX;

                //--- criterion #2: if there is only one datapoint per series, it's a scatter
                foreach (var col in row.Keys.Skip(1))
                {
                    if (!pointsPerSeries.ContainsKey(col))
                        pointsPerSeries[col] = 1;
                    else
                        pointsPerSeries[col]++;
                }
            }

            return pointsPerSeries.Max(kv => kv.Value) <= 1 ? true : false;
        }
        #endregion

        #region protected object RenderTable(string selectedChart)
        /// <summary>
        /// render chart as table
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>table model</returns>
        protected object RenderTable(string selectedChart)
        {
            return PlotData[selectedChart];
        }

        #endregion
        #region protected PlotModel RenderSimple(string selectedChart)
        /// <summary>
        /// Render simple x/y line chart.
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
            if (chartData.First().Count < 25)
                plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = CFG_IS_LOG ? (OxyPlot.Axes.Axis)new LogarithmicAxis() : new LinearAxis();
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

                    // new 2020v01
                    if (!IsNumeric(col.Value))
                        continue;

                    if (col.Value.GetType() == typeof(double)
                    && (double.IsInfinity((double)col.Value) || double.IsNaN((double)col.Value)))
                        continue;

                    string yLabel = col.Key;
                    double yValue = col.Value.GetType() == typeof(double)
                        ? (double)col.Value
                        : (double)(int)col.Value;

                    if (!allSeries.ContainsKey(yLabel))
                    {
                        var newSeries = new LineSeries();
                        newSeries.Title = CFG_HAS_LEGEND ? yLabel : "";
                        newSeries.IsVisible = true;
                        newSeries.XAxisKey = "x";
                        newSeries.YAxisKey = "y";
                        newSeries.Color = CFG_COLORS[allSeries.Count % CFG_COLORS.Count()];
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
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
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
                        newSeries.MarkerSize = CFG_SCATTER_SIZE;
                        newSeries.MarkerStroke = CFG_COLORS[allSeries.Count % CFG_COLORS.Count()];
                        newSeries.MarkerFill = newSeries.MarkerStroke;
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

        #region protected PlotModel RenderNavAndDrawdown()
        /// <summary>
        /// Specialized chart rendering NAV and drawdown logarithmically
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        protected PlotModel RenderNavAndDrawdown()
        {
            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = PlotData.Keys.First();
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
            plotModel.Axes.Clear();

            Axis xAxis = new DateTimeAxis();
            xAxis.Title = "Date";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            if ((_endDate - _startDate).TotalDays < 400)
            {
                // force monthly grid
                DateTime date = _startDate.Date;
                DateTime prevDate = date;
                List<double> extraGridLines = new List<double>();
                do
                {
                    date += TimeSpan.FromDays(1);

                    if (date.Month != prevDate.Month)
                    {
                        extraGridLines.Add(DateTimeAxis.ToDouble(date));
                    }

                    prevDate = date;
                } while (date < _endDate.Date);

                xAxis.ExtraGridlines = extraGridLines.ToArray();
                xAxis.ExtraGridlineThickness = 1;
                xAxis.ExtraGridlineStyle = LineStyle.Dot;
                xAxis.ExtraGridlineColor = OxyColor.FromAColor(255, OxyColors.LightGray);
                //xAxis.StringFormat = "yyyy/MM/dd";
            }

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
            for (int i = 0; i < _numYLabels; i++)
            {
                string yLabel = _yLabels[i];
                var series = _getSeries(yLabel);
                var color = CFG_COLORS[i % CFG_COLORS.Count()];

                var eqSeries = CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
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

                var ddSeries = CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "dd",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
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
                double y0 = _startValue(yLabel);

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
        #region protected List<Dictionary<string, object>> RenderMetrics()
        /// <summary>
        /// Specialized table rendering strategy and benchmark metrics.
        /// </summary>
        /// <returns>table model</returns>
        protected List<Dictionary<string, object>> RenderMetrics()
        {
            // TODO: this code needs some cleanup!
            // we want to make sure that meaningful metrics 
            // are generated for an arbitrary number of series

            var retvalue = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = null;

            row = new Dictionary<String, object>();
            row[METRIC_LABEL] = "Simulation Start";
            row[UNI_LABEL] = string.Format("{0:MM/dd/yyyy}", _startDate);
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:C2}", 1000.0);
            //row[label] = string.Format("{0:C2}", START_VALUE(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Simulation End";
            row[UNI_LABEL] = string.Format("{0:MM/dd/yyyy}", _endDate);
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:C2}", 1000.0 * _endValue(label) / _startValue(label));
            //row[label] = string.Format("{0:C2}", END_VALUE(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Simulation Period";
            row[UNI_LABEL] = string.Format("{0:F1} years", _numYears);
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Compound Annual Growth Rate";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:P2}", _cagr(label));
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
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:P2}",
                    Math.Sqrt(12.0) * _stdMonthlyReturn(label)); // likely incorrect as we are using log-returns
                                                                 //(Math.Exp(Math.Sqrt(12.0) * _stdMonthlyReturn(label)) - 1.0)); // is this better? code cuplicated in RenderComps!
            retvalue.Add(row);


            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Maximum Drawdown (Daily)";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:P2}", _mdd(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Maximum Flat Days";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:F2} days", _maxFlatDays(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Sharpe Ratio (Rf=T-Bill, Monthly, Annualized)";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:F2}", _sharpeRatio(label));
            retvalue.Add(row);

            if (CFG_BETA_TO_SPX)
            {
                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = "Beta (To S&P 500, Monthly)";
                foreach (var label in _yLabels)
                    row[label] = string.Format("{0:F2}", _betaToSpx(label));
                retvalue.Add(row);
            }
            else
            {
                if (_numYLabels >= 2)
                {
                    row = new Dictionary<string, object>();
                    row[METRIC_LABEL] = "Beta (To Benchmark, Monthly)";
                    foreach (var label in _yLabels)
                        row[_xamlLabel(label)] = label == _benchYLabel
                            ? "- benchmark -"
                            : string.Format("{0:F2}", _beta(label, _benchYLabel));
                    retvalue.Add(row);
                }
            }

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Ulcer Index";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:P2}", _ulcerIndex(label));
            retvalue.Add(row);

            row = new Dictionary<string, object>();
            row[METRIC_LABEL] = "Ulcer Performance Index (Martin Ratio)";
            foreach (var label in _yLabels)
                row[_xamlLabel(label)] = string.Format("{0:F2}", _ulcerPerformanceIndex(label));
            retvalue.Add(row);

            // Information Ratio
            // Sortino Ratio
            // Calmar Ratio
            // Fouse Ratio
            // Sterling Ratio

            return retvalue;
        }
        #endregion
        #region protected PlotModel RenderAnnualColumns()
        /// <summary>
        /// Specialized chart rendering annual columns for NAV and benchmark P&L
        /// </summary>
        /// <param name="selectedChart">chart to render</param>
        /// <returns>OxyPlot model</returns>
        protected PlotModel RenderAnnualColumns()
        {
            //===== create annual bars
            Dictionary<int, Dictionary<string, double>> yearlyBars = new Dictionary<int, Dictionary<string, double>>();
            foreach (var label in _yLabels)
            {
                var series = _getSeries(label);
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
            plotModel.Title = Plotter.SheetNames.ANNUAL_BARS;
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
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
            for (int i = 0; i < _numYLabels; i++)
            {
                string yLabel = _yLabels[i];
                var color = CFG_COLORS[i];

                // TODO: use CFG_IS_AREA here? 
                // Note that logic for NUM_Y_LABELS is inverted!
                var colSeries = (yLabel == _firstYLabel || _numYLabels > 2)
                    ? new BarSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "y", // we transpose!
                        YAxisKey = "x",
                        StrokeColor = color,
                        FillColor = i == 0 ? CFG_COLOR0_FILL : color,
                    }
                    : new BarSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "y", // we transpose!
                        YAxisKey = "x",
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

                    colSeries.Items.Add(new BarItem(pnl));
                }
            }

            return plotModel;
        }
        #endregion
        #region protected PlotModel RenderReturnDistribution()
        protected PlotModel RenderReturnDistribution()
        {
            //===== create distributions
            Dictionary<string, List<double>> distributions = new Dictionary<string, List<double>>();
            foreach (var label in _yLabels)
                distributions[label] = _returnDistribution(label);

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = Plotter.SheetNames.RETURN_DISTRIBUTION;
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
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
            for (int s = 0; s < _numYLabels; s++)
            {
                string yLabel = distributions.Keys.Skip(s).First();
                OxyColor color = CFG_COLORS[s];

                var distrSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y",
                    Color = color,
                    Fill = s == 0 ? CFG_COLOR0_FILL : color,
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
        #region protected PlotModel RenderMonteCarloV1()
        protected PlotModel RenderMonteCarloLegacyV1()
        {
            //===== create distributions
            Dictionary<string, List<double>> distributions = new Dictionary<string, List<double>>();
            foreach (var label in _yLabels)
                distributions[label] = _returnDistribution(label);

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
                    double cagr = Math.Pow(equityCurve.Last(), 1.0 / _numYears) - 1.0;
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
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
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
            for (int s = 0; s < _numYLabels; s++)
            {
                string yLabel = distributions.Keys.Skip(s).First();
                OxyColor color = CFG_COLORS[s];

                //--- CAGR
                var cagrSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    Color = color,
                    Fill = s == 0 ? CFG_COLOR0_FILL : color,
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
                var mddSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y2",
                    Color = color,
                    Fill = s == 0 ? CFG_COLOR0_FILL : color,
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

                var cagrValue = _cagr(yLabel);
                var cagrProb = _probabilityOfReturn(distrCagr, cagrValue);
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

                var mddValue = _mdd(yLabel);
                var mddProb = _probabilityOfReturn(distrMdd, mddValue);
                mddMarker.Points.Add(new ScatterPoint(100.0 * mddProb, -100.0 * mddValue));
            }

            return plotModel;
        }
        #endregion
        #region protected PlotModel RenderMonteCarlo()
        protected PlotModel RenderMonteCarlo()
        {
            var LEFT_TAIL = 5;
            var RIGHT_TAIL = 95;

            //===== get CAGRs
            var cagrsLeft = new Dictionary<string, Dictionary<double, double>>();
            var cagrsRight = new Dictionary<string, Dictionary<double, double>>();
            var maxCagrToChart = 0.0;

            for (int s = 0; s < _numYLabels; s++)
            {
                string yLabel = _yLabels[s];
                (Dictionary<double, double> cagrLeft, Dictionary<double, double> cagrRight)
                    = _tailCagr(yLabel, LEFT_TAIL, RIGHT_TAIL);

                cagrsLeft[yLabel] = cagrLeft;
                cagrsRight[yLabel] = cagrRight;

                try
                {
                    var yearsToBreakEven = cagrLeft
                        .OrderBy(kv => kv.Key)
                        .Where(kv => kv.Value > 0.0)
                        .First().Key;

                    var maxCagrAtBreakeven = cagrRight[yearsToBreakEven];
                    maxCagrToChart = Math.Max(maxCagrToChart, maxCagrAtBreakeven);
                }
                catch (Exception)
                {
                    // simply ignore
                    // we get here, when there is no break-even point
                }
            }

            //===== get DDs
            var dds = new Dictionary<string, Dictionary<double, double>>();

            for (int s = 0; s < _numYLabels; s++)
            {
                string yLabel = _yLabels[s];
                dds[yLabel] = _tailDd(yLabel, LEFT_TAIL);
            }

            //===== plot results
            PlotModel plotModel = new PlotModel();
            plotModel.Title = String.Format("Monte Carlo Analysis of Expected Returns and Drawdowns", LEFT_TAIL, RIGHT_TAIL);
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.RightBottom });
            plotModel.Axes.Clear();

            Axis xAxis = new LinearAxis();
            //Axis xAxis = new LogarithmicAxis();
            xAxis.Title = "Investment Period [years]";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";
            plotModel.Axes.Add(xAxis);

            var yAxis1 = new LinearAxis();
            yAxis1.Title = string.Format("CAGR at {0}-th/ {1}-th Percentile [%]", LEFT_TAIL, RIGHT_TAIL);
            yAxis1.Position = AxisPosition.Right;
            yAxis1.StartPosition = 0.51;
            yAxis1.EndPosition = 1.0;
            yAxis1.Key = "y1";
            plotModel.Axes.Add(yAxis1);

            var yAxis2 = new LinearAxis();
            yAxis2.Title = string.Format("Worst Drawdowns at {0}-th Percentile [%]", LEFT_TAIL, RIGHT_TAIL);
            yAxis2.Position = AxisPosition.Right;
            yAxis2.StartPosition = 0.0;
            yAxis2.EndPosition = 0.49;
            yAxis2.Key = "y2";
            plotModel.Axes.Add(yAxis2);

            //===== add series
            for (int s = 0; s < _numYLabels; s++)
            {
                string yLabel = _yLabels[s];

                var cagrLeft = cagrsLeft[yLabel];
                var cagrRight = cagrsRight[yLabel];
                var dd = dds[yLabel];

                OxyColor color = CFG_COLORS[s];

                //--- CAGR
                var cagrHighSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    Color = color,
                    Fill = s == 0 ? CFG_COLOR0_FILL : color,
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

                var cagrLowSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    //Color = color,
                    //Fill = color,
                    Color = OxyColors.White,
                    Fill = OxyColors.White,
                    ConstantY2 = 0.0,
                }
                : new LineSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y1",
                    Color = color,
                };

                plotModel.Series.Add(cagrHighSeries);
                plotModel.Series.Add(cagrLowSeries); // must come *after* cagrHighSeries

                foreach (var years in cagrLeft.Keys)
                {
                    if (CFG_IS_AREA(yLabel))
                    {
                        cagrLowSeries.Points.Add(new DataPoint(years, 100.0 * Math.Max(0.0, cagrLeft[years])));
                        cagrHighSeries.Points.Add(new DataPoint(years, 100.0 * Math.Min(maxCagrToChart, cagrRight[years])));
                    }
                    else
                    {
                        if (cagrLeft[years] >= 0.0)
                            cagrLowSeries.Points.Add(new DataPoint(years, 100.0 * cagrLeft[years]));

                        if (cagrRight[years] <= maxCagrToChart)
                            cagrHighSeries.Points.Add(new DataPoint(years, 100.0 * cagrRight[years]));
                    }
                }

                //--- drawdown
                var ddSeries = CFG_IS_AREA(yLabel)
                ? new AreaSeries
                {
                    //Title = yLabel,
                    IsVisible = true,
                    XAxisKey = "x",
                    YAxisKey = "y2",
                    Color = color,
                    Fill = s == 0 ? CFG_COLOR0_FILL : color,
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

                plotModel.Series.Add(ddSeries);

                foreach (var years in dd.Keys)
                {
                    ddSeries.Points.Add(new DataPoint(years, 100.0 * dd[years]));
                }
            }

            return plotModel;
        }
        #endregion
        #region protected PlotModel RenderExposure()
        protected PlotModel RenderExposure(string selectedChart)
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
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
            plotModel.Axes.Clear();

            Axis xAxis = xValue.GetType() == typeof(DateTime)
                ? new DateTimeAxis()
                : new LinearAxis();
            xAxis.Title = xLabel;
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Title = "Exposure [%]";
            yAxis.Position = AxisPosition.Right;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            //===== create series
            Dictionary<string, AreaSeries> allSeries = new Dictionary<string, AreaSeries>();

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
                        var color = CFG_COLORS[allSeries.Count % CFG_COLORS.Count()];
                        var newSeries = new AreaSeries
                        {
                            Title = yLabel,
                            IsVisible = true,
                            XAxisKey = "x",
                            YAxisKey = "y",
                            Color = color,
                            Fill = color,
                            //ConstantY2 = 0.0,
                        };
                        allSeries[yLabel] = newSeries;
                    }

                    double yStacked = 0.0;
                    foreach (var col2 in row)
                    {
                        if (col2.Key == xLabel)
                            continue;

                        if (col2.Key == col.Key)
                            break;

                        yStacked += (double)col2.Value;
                    }

                    allSeries[col.Key].Points.Add(new DataPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        100.0 * (yStacked + (double)yValue)));

                    allSeries[col.Key].Points2.Add(new DataPoint(
                        xValue.GetType() == typeof(DateTime) ? DateTimeAxis.ToDouble(xValue) : (double)xValue,
                        100.0 * (yStacked /*+ (double)yValue*/)));
                }
            }

            //===== add series to plot model
            foreach (var series in allSeries)
                plotModel.Series.Add(series.Value);

            return plotModel;
        }
        #endregion
        #region protected List<Dictionary<string, object>> RenderDash()
        protected List<Dictionary<string, object>> RenderDash()
        {
            var retvalue = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = null;

            DateTime dateLast = _endDate;
            double navLast = _endValue(_firstYLabel);

            //--- 12-months
            {
                DateTime datePast = dateLast - TimeSpan.FromDays(365.25);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePast - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                double navPast = (double)rowPast[_firstYLabel];

                row = new Dictionary<String, object>();
                row[METRIC_LABEL] = "12-Months Return";
                row["Value"] = string.Format("{0:+0.0;-0.0;0.0}%", 100.0 * (navLast / navPast - 1.0));
                retvalue.Add(row);
            }

            //--- 3-months
            {
                DateTime datePast = dateLast - TimeSpan.FromDays(3 * 30.5);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePast - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                double navPast = (double)rowPast[_firstYLabel];

                row = new Dictionary<String, object>();
                row[METRIC_LABEL] = "3-Months Return";
                row["Value"] = string.Format("{0:+0.0;-0.0;0.0}%", 100.0 * (navLast / navPast - 1.0));
                retvalue.Add(row);
            }

            //--- 1 months
            {
                DateTime datePast = dateLast - TimeSpan.FromDays(30.5);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePast - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                double navPast = (double)rowPast[_firstYLabel];

                row = new Dictionary<String, object>();
                row[METRIC_LABEL] = "1-Month Return";
                row["Value"] = string.Format("{0:+0.0;-0.0;0.0}%", 100.0 * (navLast / navPast - 1.0));
                retvalue.Add(row);
            }

            //--- 1 week
            {
                DateTime datePast = dateLast - TimeSpan.FromDays(7);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePast - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                double navPast = (double)rowPast[_firstYLabel];

                row = new Dictionary<String, object>();
                row[METRIC_LABEL] = "1-Week Return";
                row["Value"] = string.Format("{0:+0.0;-0.0;0.0}%", 100.0 * (navLast / navPast - 1.0));
                retvalue.Add(row);
            }

            return retvalue;

        }
        #endregion
        #region protected List<Dictionary<string, object>> RenderComps()
        protected List<Dictionary<string, object>> RenderComps()
        {
            var retvalue = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = null;

            DateTime dateLast = _endDate;
            double navLast = _endValue(_firstYLabel);

            //--- cagr over various periods
            var periods = new List<Tuple<string, double>>
            {
                Tuple.Create("cagr-1w", 1 / 52.0),
                Tuple.Create("cagr-1m", 1 / 12.0),
                Tuple.Create("cagr-3m", 1 / 4.0),
                Tuple.Create("cagr-1y", 1.0),
                Tuple.Create("cagr-2y", 2.0),
                Tuple.Create("cagr-5y", 5.0),
                Tuple.Create("cagr-10y", 10.0),
                Tuple.Create("cagr-max", 100.0),
            };

            foreach (var p in periods)
            {
                DateTime datePastTarget = dateLast - TimeSpan.FromDays(p.Item2 * 365.25);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePastTarget - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                DateTime datePast = (DateTime)rowPast[_xLabel];
                double years = (dateLast - datePast).TotalDays / 365.25;
                double navPast = (double)rowPast[_firstYLabel];
                double cagr = 100.0 * (Math.Pow(navLast / navPast, 1.0 / years) - 1.0);

                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = p.Item1;
                row["Value"] = cagr;
                retvalue.Add(row);
            }
            //--- year-to-date
            {
                DateTime datePastTarget = dateLast - TimeSpan.FromDays(dateLast.DayOfYear);
                Dictionary<string, object> rowPast = _firstChart
                    .OrderBy(r => Math.Abs((datePastTarget - (DateTime)r[_xLabel]).TotalSeconds))
                    .First();
                DateTime datePast = (DateTime)rowPast[_xLabel];
                double years = (dateLast - datePast).TotalDays / 365.25;
                double navPast = (double)rowPast[_firstYLabel];
                double change = 100.0 * (navLast / navPast - 1.0);
                double cagr = 100.0 * (Math.Pow(navLast / navPast, 1.0 / years) - 1.0);

                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = "cagr-ytd";
                row["Value"] = cagr;
                retvalue.Add(row);

                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = "chg-ytd";
                row["Value"] = change;
                retvalue.Add(row);
            }

            //--- other metrics
            var metrics = new List<Tuple<string, Func<object>>>
            {
                //Tuple.Create<string, Func<object>>("stdev", () => 100.0 * (Math.Exp(Math.Sqrt(12.0) * _stdMonthlyReturn(_firstYLabel)) - 1.0)),
                Tuple.Create<string, Func<object>>("stdev", () => 100.0 * Math.Sqrt(12.0) * _stdMonthlyReturn(_firstYLabel)),
                Tuple.Create<string, Func<object>>("mdd", () => 100.0 * _mdd(_firstYLabel)),
                Tuple.Create<string, Func<object>>("ulcer", () => 100.0 * _ulcerIndex(_firstYLabel)),
                Tuple.Create<string, Func<object>>("sharpe", () => _sharpeRatio(_firstYLabel)),
                Tuple.Create<string, Func<object>>("martin", () => _ulcerPerformanceIndex(_firstYLabel)),
                Tuple.Create<string, Func<object>>("nav-end", ()  => 1000.0 * _endValue(_firstYLabel) / _startValue(_firstYLabel)),
                Tuple.Create<string, Func<object>>("cagr-5th", () => "[" + _leftTailReturns(_firstYLabel, 0.05)
                    .Where(kv => kv.Key > 0.0 && kv.Key < 25.1
                        && Math.Abs(kv.Key - Math.Round(kv.Key)) < 1.0 / 24.0)
                    .Select(kv => kv.Value)
                    .Aggregate("", (agg, item) => agg
                        + (agg.Length > 0 ? "," : "")
                        + string.Format("{0:F2}", 100.0 * item)) + "]"),
                Tuple.Create<string, Func<object>>("mdd-5th", () =>
                {
                    var mdd = 0.0;
                    var dummy = _leftTailReturns(_firstYLabel, 0.05, out mdd);
                    return mdd;
                }),
                Tuple.Create<string, Func<object>>("date-end", () => string.Format("{0:MM/dd/yyyy}", _endDate)),
                Tuple.Create<string, Func<object>>("updated", () => DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm'Z'")),
            };

            foreach (var m in metrics)
            {
                row = new Dictionary<string, object>();
                row[METRIC_LABEL] = m.Item1;
                try
                {
                    row["Value"] = m.Item2();
                    retvalue.Add(row);
                }
                catch (Exception)
                {
                    // ignore for now
                    Output.WriteLine("Error: metric {0} failed", m.Item1);
                }
            }

            return retvalue;
        }
        #endregion
        #region protected PlotModel RenderRollingReturns()
        protected PlotModel RenderRollingReturns()
        {
            const double ROLLING_YEARS = 1.0;
            const double ROLLING_DAYS = ROLLING_YEARS * 365.25;

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = string.Format("{0}-Year Rolling Returns & Tracking to Benchmark", ROLLING_YEARS);
            plotModel.Legends.Add(new Legend() { LegendPosition = LegendPosition.TopLeft });
            plotModel.Axes.Clear();

            Axis xAxis = new DateTimeAxis();
            xAxis.Title = "Date";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var retAxis = new LinearAxis();
            retAxis.Title = "Annualized Rolling Return [%]";
            retAxis.Position = AxisPosition.Right;
            retAxis.StartPosition = 0.5;
            retAxis.EndPosition = 1.0;
            retAxis.Key = "ret";

            var trkAxis = new LinearAxis();
            trkAxis.Title = "Tracking to Benchmark [%]";
            trkAxis.Position = AxisPosition.Right;
            trkAxis.StartPosition = 0.0;
            trkAxis.EndPosition = 0.5;
            trkAxis.Key = "trk";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(retAxis);
            plotModel.Axes.Add(trkAxis);

            #region plotRollingReturn
            void plotRollingReturn(int i)
            {
                string yLabel = _yLabels[i];
                var series = _getSeries(yLabel);
                var color = CFG_COLORS[i % CFG_COLORS.Count()];

                var retSeries = CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "ret",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
                        ConstantY2 = 1.0,
                    }
                    : new LineSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "ret",
                        Color = color,
                    };

                plotModel.Series.Add(retSeries);

                var navCurrentFiltered = (double?)null;
                var navPastFiltered = (double?)null;
                foreach (var point in series)
                {
                    var dateCurrent = point.Key;
                    var datePastRaw = dateCurrent - TimeSpan.FromDays(ROLLING_DAYS);
                    var datePast = series.Keys
                        .OrderBy(d => Math.Abs((d - datePastRaw).TotalDays))
                        .First();

                    if (Math.Abs((datePastRaw - datePast).TotalDays) > 5)
                        continue;

                    var navCurrent = point.Value;
                    var navPast = series[datePast];
                    const double ALPHA = 2.0 / (40.0 + 1.0);
                    navCurrentFiltered = navCurrentFiltered == null
                        ? navCurrent
                        : ALPHA * (navCurrent - navCurrentFiltered) + navCurrentFiltered;
                    navPastFiltered = navPastFiltered == null
                        ? navPast
                        : ALPHA * (navPast - navPastFiltered) + navPastFiltered;
                    var retCurrent = 100.0 * (Math.Pow((double)(navCurrentFiltered / navPastFiltered), 1.0 / ROLLING_YEARS) - 1.0);

                    retSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(dateCurrent),
                        retCurrent));
                }
            }
            #endregion
            #region plotTracking
            void plotTracking(int i)
            {
                if (CFG_TRACKING_TO_LAST)
                {
                    // until 2023vi12 - benchmark all against last
                    if (i >= _numYLabels - 1)
                        return;

                    var benchSeries = _getSeries(_benchYLabel);

                    string yLabel = _yLabels[i];
                    var series = _getSeries(yLabel);
                    var color = CFG_COLORS[i % CFG_COLORS.Count()];

                    var trkSeries = /*CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
                        ConstantY2 = 1.0,
                    }
                    :*/ new LineSeries
                        {
                            //Title = yLabel + " vs " + _benchYLabel,
                            IsVisible = true,
                            XAxisKey = "x",
                            YAxisKey = "trk",
                            Color = color,
                        };

                    plotModel.Series.Add(trkSeries);

                    var navFiltered = (double?)null;
                    var benchFiltered = (double?)null;
                    var scale = (double?)null;
                    foreach (var point in series)
                    {
                        var dateCurrent = point.Key;
                        var navCurrent = point.Value;
                        var benchCurrent = benchSeries[dateCurrent];

                        const double ALPHA = 2.0 / (40.0 + 1.0);
                        navFiltered = navFiltered == null
                            ? navCurrent
                            : ALPHA * (navCurrent - navFiltered) + navFiltered;
                        benchFiltered = benchFiltered == null
                            ? benchCurrent
                            : ALPHA * (benchCurrent - benchFiltered) + benchFiltered;
                        scale = scale ?? benchCurrent / navCurrent;

                        var tracking = 100.0 * ((double)(scale * navFiltered / benchFiltered) - 1.0);

                        trkSeries.Points.Add(new DataPoint(
                            DateTimeAxis.ToDouble(dateCurrent),
                            tracking));
                    }
                }
                else
                {
                    // since 2023vi12 - benchmark first against all others
                    if (i == 0)
                        return;

                    var series = _getSeries(_firstYLabel);

                    string yLabel = _yLabels[i];
                    var benchSeries = _getSeries(yLabel);

                    var color = CFG_COLORS[i % CFG_COLORS.Count()];

                    var trkSeries = /*CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
                        ConstantY2 = 1.0,
                    }
                    :*/ new LineSeries
                        {
                            //Title = yLabel + " vs " + _benchYLabel,
                            IsVisible = true,
                            XAxisKey = "x",
                            YAxisKey = "trk",
                            Color = color,
                        };

                    plotModel.Series.Add(trkSeries);

                    var navFiltered = (double?)null;
                    var benchFiltered = (double?)null;
                    var scale = (double?)null;
                    foreach (var point in series)
                    {
                        var dateCurrent = point.Key;
                        var navCurrent = point.Value;
                        var benchCurrent = benchSeries[dateCurrent];

                        const double ALPHA = 2.0 / (40.0 + 1.0);
                        navFiltered = navFiltered == null
                            ? navCurrent
                            : ALPHA * (navCurrent - navFiltered) + navFiltered;
                        benchFiltered = benchFiltered == null
                            ? benchCurrent
                            : ALPHA * (benchCurrent - benchFiltered) + benchFiltered;
                        scale = scale ?? benchCurrent / navCurrent;

                        var tracking = 100.0 * ((double)(scale * navFiltered / benchFiltered) - 1.0);

                        trkSeries.Points.Add(new DataPoint(
                            DateTimeAxis.ToDouble(dateCurrent),
                            tracking));
                    }
                }
            }
            #endregion

            //===== create series
            for (int i = 0; i < _numYLabels; i++)
            {
                plotRollingReturn(i);
                plotTracking(i);
            }

            return plotModel;
        }
#if false
        protected PlotModel RenderRollingReturns()
        {
            const double ROLLING_YEARS = 1.0;
            const double ROLLING_DAYS = ROLLING_YEARS * 365.25;

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = string.Format("{0}-Year Rolling Returns & Drawdowns", ROLLING_YEARS);
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = new DateTimeAxis();
            xAxis.Title = "Date";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Title = "Return [%]";
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
            for (int i = 0; i < _numYLabels; i++)
            {
                string yLabel = _yLabels[i];
                var series = _getSeries(yLabel);
                var color = CFG_COLORS[i % CFG_COLORS.Count()];

                var eqSeries = CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
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

                var ddSeries = CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "dd",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
                    }
                    : new LineSeries
                    {
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "dd",
                        Color = color,
                    };

                plotModel.Series.Add(ddSeries);

                var navCurrentFiltered = (double?)null;
                var navPastFiltered = (double?)null;
                foreach (var point in series)
                {
                    var dateCurrent = point.Key;
                    var datePastRaw = dateCurrent - TimeSpan.FromDays(ROLLING_DAYS);
                    var datePast = series.Keys
                        .OrderBy(d => Math.Abs((d - datePastRaw).TotalDays))
                        .First();

                    if (Math.Abs((datePastRaw - datePast).TotalDays) > 5)
                        continue;

                    var navCurrent = point.Value;
                    var navPast = series[datePast];
                    const double ALPHA = 2.0 / (40.0 + 1.0);
                    navCurrentFiltered = navCurrentFiltered == null
                        ? navCurrent
                        : ALPHA * (navCurrent - navCurrentFiltered) + navCurrentFiltered;
                    navPastFiltered = navPastFiltered == null
                        ? navPast
                        : ALPHA * (navPast - navPastFiltered) + navPastFiltered;
                    var retCurrent = 100.0 * (Math.Pow((double)(navCurrentFiltered / navPastFiltered), 1.0 / ROLLING_YEARS) - 1.0);

                    eqSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(dateCurrent),
                        retCurrent));

                    var navPeak = series
                        .Where(kv => kv.Key >= datePast && kv.Key <= dateCurrent)
                        .Max(kv => kv.Value);
                    var dd = 100.0 * ((double)navCurrentFiltered / navPeak - 1.0);

                    ddSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(dateCurrent),
                        dd));
                }
            }

            return plotModel;
        }
#endif
        #endregion
        #region protected PlotModel RenderTrackingToBenchmark()
#if false
        protected PlotModel RenderTrackingToBenchmark()
        {
            if (_numYLabels < 2)
                return null;

            //===== initialize plot model
            PlotModel plotModel = new PlotModel();
            plotModel.Title = "Tracking to Benchmark";
            plotModel.LegendPosition = LegendPosition.LeftTop;
            plotModel.Axes.Clear();

            Axis xAxis = new DateTimeAxis();
            xAxis.Title = "Date";
            xAxis.Position = AxisPosition.Bottom;
            xAxis.Key = "x";

            var yAxis = new LinearAxis();
            yAxis.Title = "Excess Return [%]";
            yAxis.Position = AxisPosition.Right;
            yAxis.StartPosition = 0.0;
            yAxis.EndPosition = 1.0;
            yAxis.Key = "y";

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            var benchSeries = _getSeries(_benchYLabel);

            //===== create series
            for (int i = 0; i < _numYLabels - 1; i++)
            {
                string yLabel = _yLabels[i];
                var series = _getSeries(yLabel);
                var color = CFG_COLORS[i % CFG_COLORS.Count()];

                var eqSeries = /*CFG_IS_AREA(yLabel)
                    ? new AreaSeries
                    {
                        Title = yLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                        Fill = i == 0 ? CFG_COLOR0_FILL : color,
                        ConstantY2 = 1.0,
                    }
                    :*/ new LineSeries
                    {
                        Title = yLabel + " vs " + _benchYLabel,
                        IsVisible = true,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        Color = color,
                    };

                plotModel.Series.Add(eqSeries);

                var navFiltered = (double?)null;
                var benchFiltered = (double?)null;
                var scale = (double?)null;
                foreach (var point in series)
                {
                    var dateCurrent = point.Key;
                    var navCurrent = point.Value;
                    var benchCurrent = benchSeries[dateCurrent];

                    const double ALPHA = 2.0 / (40.0 + 1.0);
                    navFiltered = navFiltered == null
                        ? navCurrent
                        : ALPHA * (navCurrent - navFiltered) + navFiltered;
                    benchFiltered = benchFiltered == null
                        ? benchCurrent
                        : ALPHA * (benchCurrent - benchFiltered) + benchFiltered;
                    scale = scale ?? benchCurrent / navCurrent;

                    var tracking = 100.0 * ((double)(scale * navFiltered / benchFiltered) - 1.0);

                    eqSeries.Points.Add(new DataPoint(
                        DateTimeAxis.ToDouble(dateCurrent),
                        tracking));
                }
            }

            return plotModel;
        }
#endif
        #endregion

        #region public void SaveAsPng(string chartToSave, string pngFilePath)
        /// <summary>
        /// save chart as PNG
        /// </summary>
        /// <param name="chartToSave">chart to save</param>
        /// <param name="pngFilePath">path to PNG</param>
        public void SaveAsPng(string chartToSave, string pngFilePath, int width = 1280, int height = 720)
        {
            PlotModel model = (PlotModel)GetModel(chartToSave);

#if false
            using (var stream = File.Create(pngFilePath))
            {
                var exporter = new OxyPlot.Wpf.SvgExporter();
                exporter.Export(model, stream);
            }
#else
            OxyPlot.Wpf.PngExporter.Export(model,
                pngFilePath,
                width, height);
            //OxyColors.White);
            //OxyColor.FromArgb(0, 0, 0, 0)); // transparent
#endif
        }
        #endregion
        #region public void SaveAsCsv(string chartToSave, string csvFilePath)
        /// <summary>
        /// save table as CSV
        /// </summary>
        /// <param name="chartToSave">chart to save</param>
        /// <param name="csvFilePath">path to CSV</param>
        public void SaveAsCsv(string chartToSave, string csvFilePath)
        {
            using (StreamWriter sw = new StreamWriter(csvFilePath))
            {

                List<Dictionary<string, object>> tableModel = (List<Dictionary<string, object>>)GetModel(chartToSave);

                List<string> columns = tableModel
                    .SelectMany(row => row.Keys)
                    .Distinct()
                    .ToList();

                foreach (var col in columns)
                {
                    sw.Write("{0},", col);
                }
                sw.WriteLine("");

                foreach (var row in tableModel)
                {
                    foreach (var col in columns)
                    {
                        if (row.ContainsKey(col))
                        {
                            sw.Write("\"{0}\",", row[col].ToString());
                        }
                        else
                        {
                            sw.Write(",");
                        }
                    }

                    sw.WriteLine("");
                }
            }
        }
        #endregion
        #region public void SaveAsJson(string chartToSave, string csvFilePath)
        /// <summary>
        /// save table as Json
        /// </summary>
        /// <param name="chartToSave">chart to save</param>
        /// <param name="csvFilePath">path to Json</param>
        public void SaveAsJson(string chartToSave, string csvFilePath)
        {
            using (StreamWriter sw = new StreamWriter(csvFilePath))
            {

                List<Dictionary<string, object>> tableModel = (List<Dictionary<string, object>>)GetModel(chartToSave);

                List<string> columns = tableModel
                    .SelectMany(row => row.Keys)
                    .Distinct()
                    .ToList();

                sw.WriteLine("[");

                //----- header row
                sw.Write("[");
                for (var col = 0; col < columns.Count; col++)
                {
                    sw.Write("\"{0}\"{1}",
                        columns[col],
                        col < columns.Count - 1
                            ? ","
                            : "");
                }
                sw.WriteLine("],");

                //----- data rows
                for (var row = 0; row < tableModel.Count; row++)
                {
                    sw.Write("[");
                    for (var col = 0; col < columns.Count; col++)
                    {
                        sw.Write("\"{0}\"{1}",
                            tableModel[row].ContainsKey(columns[col])
                                ? tableModel[row][columns[col]]
                                : "",
                            col < columns.Count - 1
                                ? ","
                                : "");
                    }

                    sw.WriteLine("]{0}",
                        row < tableModel.Count - 1
                            ? ","
                            : "");
                }

                sw.WriteLine("]");
            }
        }
        #endregion
        #region public void SaveAsHtml(string chartToSave, string csvFilePath)
        /// <summary>
        /// save table as Html
        /// </summary>
        /// <param name="chartToSave">chart to save</param>
        /// <param name="csvFilePath">path to Html</param>
        public void SaveAsHtml(string chartToSave, string csvFilePath)
        {
            using (StreamWriter sw = new StreamWriter(csvFilePath))
            {

                List<Dictionary<string, object>> tableModel = (List<Dictionary<string, object>>)GetModel(chartToSave);

                List<string> columns = tableModel
                    .SelectMany(row => row.Keys)
                    .Distinct()
                    .ToList();

                sw.WriteLine("<html lang=\"en-us\"><head><link rel=\"stylesheet\" href=\"styles.css\"/><title>{0}</title></head><body><table>", chartToSave);

                //----- header row
                sw.Write("<thead><tr>");
                for (var col = 0; col < columns.Count; col++)
                {
                    sw.Write("<th>{0}</th>", columns[col]);
                }
                sw.WriteLine("</tr></thead>");

                //----- data rows
                sw.Write("<tbody>");
                for (var row = 0; row < tableModel.Count; row++)
                {
                    sw.Write("<tr>");
                    for (var col = 0; col < columns.Count; col++)
                    {
                        sw.Write("<td>{0}</td>",
                            tableModel[row].ContainsKey(columns[col])
                                ? tableModel[row][columns[col]]
                                : "");
                    }
                    sw.WriteLine("</tr>");
                }
                sw.WriteLine("</tbody>");

                //----- close
                sw.WriteLine("</table></body></html>");
            }
        }
        #endregion

        #region public void SaveAs(string chartToSave, string filePathWithoutExtension)
        public void SaveAs(string chartToSave, string filePathWithoutExtension)
        {
            // FIXME: we are rendering the model twice:
            //        once here, and once in SaveAsPng or SaveAsCsv

            object model = GetModel(chartToSave);

            if (model.GetType() == typeof(PlotModel))
            {

                string pngFilePath = Path.ChangeExtension(filePathWithoutExtension, ".png");
                SaveAsPng(chartToSave, pngFilePath);
            }
            else
            {
                string csvFilePath = Path.ChangeExtension(filePathWithoutExtension, ".csv");
                SaveAsCsv(chartToSave, csvFilePath);

#if true
                string jsonFilePath = Path.ChangeExtension(filePathWithoutExtension, ".json");
                SaveAsJson(chartToSave, jsonFilePath);
#endif
#if false
                string htmlFilePath = Path.ChangeExtension(filePathWithoutExtension, ".html");
                SaveAsHtml(chartToSave, htmlFilePath);
#endif
            }
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
        #region public string PlotTitle
        private string _plotTitle = null;
        public string PlotTitle
        {
            set
            {
                _plotTitle = value;
            }
            get
            {
                return _plotTitle ?? PlotData.Keys.First();
            }
        }
        #endregion
        #region public virtual IEnumerable<string> AvailableCharts
        /// <summary>
        /// Enumerate available charts.
        /// </summary>
        public virtual IEnumerable<string> AvailableCharts
        {
            get
            {
                if (CFG_IS_REPORT)
                {
                    yield return Plotter.SheetNames.EQUITY_CURVE;
                    yield return Plotter.SheetNames.METRICS;
                    yield return Plotter.SheetNames.ANNUAL_BARS;
                    yield return Plotter.SheetNames.RETURN_DISTRIBUTION;
                    yield return Plotter.SheetNames.MONTE_CARLO;
                    yield return Plotter.SheetNames.MONTE_CARLO_V2;
                    yield return Plotter.SheetNames.ROLLING_RETUNRS;
                    //yield return Plotter.SheetNames.TRACKING_TO_BENCH;

                    foreach (string chart in PlotData.Keys.Skip(1))
                        yield return chart;

                    yield break;
                }
                else
                {
                    foreach (string chart in PlotData.Keys)
                        yield return chart;
                }
            }
        }
        #endregion
        #region public virtual object GetModel(string selectedChart)
        /// <summary>
        /// Get table or plot model for selected chart.
        /// </summary>
        /// <param name="selectedChart"></param>
        /// <returns>model</returns>
        public virtual object GetModel(string selectedChart)
        {
            object retvalue = null;
            switch (selectedChart)
            {
                case Plotter.SheetNames.EQUITY_CURVE:
                    retvalue = RenderNavAndDrawdown();
                    break;
                case Plotter.SheetNames.METRICS:
                    retvalue = RenderMetrics();
                    break;
                case Plotter.SheetNames.ANNUAL_BARS:
                    retvalue = RenderAnnualColumns();
                    break;
                case Plotter.SheetNames.RETURN_DISTRIBUTION:
                    retvalue = RenderReturnDistribution();
                    break;
                case Plotter.SheetNames.MONTE_CARLO:
                    retvalue = RenderMonteCarloLegacyV1();
                    break;
                case Plotter.SheetNames.MONTE_CARLO_V2:
                    retvalue = RenderMonteCarlo();
                    break;
                case Plotter.SheetNames.ROLLING_RETUNRS:
                    retvalue = RenderRollingReturns();
                    break;
                //case Plotter.SheetNames.TRACKING_TO_BENCH:
                //    retvalue = RenderTrackingToBenchmark();
                //    break;
                case Plotter.SheetNames.EXPOSURE_VS_TIME:
                    retvalue = RenderExposure(selectedChart);
                    break;
                case Plotter.SheetNames.DASH:
                    retvalue = RenderDash();
                    break;
                case Plotter.SheetNames.COMPS:
                    retvalue = RenderComps();
                    break;
                default:
                    if (IsTable(selectedChart))
                        retvalue = RenderTable(selectedChart);
                    else if (IsScatter(selectedChart))
                        retvalue = RenderScatter(selectedChart);
                    else
                        retvalue = RenderSimple(selectedChart);
                    break;
            };

            if (retvalue.GetType() == typeof(PlotModel))
            {
                retvalue = _addLogo((PlotModel)retvalue);

#if true
                // NOTE: we need to make sure the background is white
                // without doing so, the background is black
                // when exported as bitmap
                ((PlotModel)retvalue).Background = OxyColors.White;
#endif
            }

            return retvalue;
        }
        #endregion
    }

    public abstract class ReportTemplate : _ReportTemplate
    { }
}

//==============================================================================
// end of file