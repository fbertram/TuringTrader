//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_FAA
// Description: Flexible Asset Allocation (FAA) strategy, as published in 
//              Wouter J. Keller, and Hugo S. van Putten's paper 
//              'Generalized Momentum and Flexible Asset Allocation (FAA)'
//              https://ssrn.com/abstract=2193735
// History:     2020iv24, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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

// see also:
// https://alphaarchitect.com/2014/09/18/flexible-asset-allocation-dethroning-moving-average-rules/
// https://systematicinvestor.github.io/strategy/Strategy-FAA
// https://indexswingtrader.blogspot.com/2013/11/flexibile-asset-allocation-with-crash.html

//#define DATE_RANGES_FROM_PAPER
// DATE_RANGES_FROM_PAPER: if defined, use fixed date ranges from paper
// otherwise, use global date ranges

#if DATE_RANGES_FROM_PAPER
//#define DATA_RANG_IS
// DATA_RANG_IS: if defined, use in-sample range (

//#define DATE_RANGE_OS
// DATE_RANGE_OS: if defined, use out-of-sample range

#define DATE_RANGE_FULL
// DATE_RANGE_FULL: if defined, use full range
#endif

//#define MUTUAL_FUNDS
// MUTUAL_FUNDS: if defined, use mutual funds, otherwise ETFs

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{

    public abstract class Keller_FAA_Core : SubclassableAlgorithm
    {
        public override string Name => string.Format(
            "Keller's FAA Strategy {0}/{1}; {2}m/{3}m/{4}m; {5}/{6}/{7}%",
            N, U.Count,
            LOOKBACK_R / 21, LOOKBACK_V / 21, LOOKBACK_C / 21,
            WR, WV, WC);

        #region inputs
        /// <summary>
        /// universe of index funds
        /// </summary>
        protected abstract List<string> U { get; }
        /// <summary>
        /// safe instrument. used to replace instruments
        /// with absolute momentum less than RMIN
        /// </summary>
        protected abstract string U_SAFE { get; }
        /// <summary>
        /// number of assets to select from universe U
        /// </summary>
        public abstract int N { get; set; }
        /// <summary>
        /// minimum absolute momentum
        /// </summary>
        public virtual int RMIN => 0;
        /// <summary>
        /// blend between equal-weight and weights
        /// inversely proportional to rank
        /// </summary>
        public virtual int A => 0;
        /// <summary>
        /// weight for returns. normalized to 1.0
        /// </summary>
        public virtual int WR => 100;
        /// <summary>
        /// weight for volatility
        /// </summary>
        public virtual int WV { get; set; } = 0;
        /// <summary>
        /// weight for correlation
        /// </summary>
        public virtual int WC { get; set; } = 0;
        /// <summary>
        /// lookback period for relative returns
        /// </summary>
        public virtual int LOOKBACK_R { get; set; } = 84;
        /// <summary>
        /// lookback period for absolute returns
        /// </summary>
        public virtual int LOOKBACK_A => LOOKBACK_R;
        /// <summary>
        /// lookback period for volatility
        /// </summary>
        public virtual int LOOKBACK_V { get; set; } = 84;
        /// <summary>
        /// lookback period for correlation
        /// </summary>
        public virtual int LOOKBACK_C { get; set; } = 84;
        #endregion
        #region internal data
        //private readonly string BENCHMARK = Assets.PORTF_60_40;
        private readonly string BENCHMARK = "algo:Keller_FAA_EW";
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        protected struct indicatorValues
        {
            public double r;
            public double a;
            public double v;
            public double c;
        }
        #endregion
        #region ctor
        public Keller_FAA_Core()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region LossFunction
        protected virtual Dictionary<Instrument, double> LossFunction(Dictionary<Instrument, indicatorValues> indicators)
        {
            // rank by decreasing momentum
            var rankR = indicators
                .OrderByDescending(kv => kv.Value.r)
                .Select((kv, i) => new { instr = kv.Key, rank = i + 1})
                .ToDictionary(
                    x => x.instr,
                    x => x.rank);

            // rank by increasing volatility
            var rankV = indicators
                .OrderBy(kv => kv.Value.v)
                .Select((kv, i) => new { instr = kv.Key, rank = i + 1})
                .ToDictionary(
                    x => x.instr,
                    x => x.rank);

            // rank by increasing correlation
            var rankC = indicators
                .OrderBy(kv => kv.Value.c)
                .Select((kv, i) => new { instr = kv.Key, rank = i + 1})
                .ToDictionary(
                    x => x.instr,
                    x => x.rank);

            // loss function L(i), formula (2)
            var L = indicators.Keys
                .ToDictionary(
                    i => i,
                    i => WR / 100.0 * rankR[i]
                        + WV / 100.0 * rankV[i]
                        + WC / 100.0 * rankC[i]);

            return L;
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

#if DATE_RANGES_FROM_PAPER
    #if DATA_RANG_IS
            StartTime = DateTime.Parse("01/03/2005", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("12/11/2012, 4pm", CultureInfo.InvariantCulture);
    #endif
    #if DATE_RANGE_OS
            StartTime = DateTime.Parse("01/02/1998", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("12/31/2004, 4pm", CultureInfo.InvariantCulture);
    #endif
    #if DATE_RANGE_FULL
            StartTime = DateTime.Parse("01/02/1998", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("12/14/2012, 4pm", CultureInfo.InvariantCulture);
    #endif
            WarmupStartTime = StartTime - TimeSpan.FromDays(126);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            var universe = AddDataSources(U);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                // skip if there are any instruments missing from our universe
                if (!HasInstruments(universe) || !HasInstrument(benchmark))
                    continue;

                // calculate indicators
                var indicators = universe
                    .Select(ds => ds.Instrument)
                    .ToDictionary(
                        i => i,
                        i => new indicatorValues
                        {
                            r = i.Close[0] / i.Close[LOOKBACK_R] - 1.0,
                            a = i.Close[0] / i.Close[LOOKBACK_A] - 1.0,
                            v = i.Close.Volatility(LOOKBACK_C)[0],
                            c = universe
                                .Select(ds => ds.Instrument)
                                .Where(i2 => i != i2)
                                // FIXME: how exactly are we calculating correlation here?
                                .Average(i2 => i.Close.Correlation(i2.Close, LOOKBACK_C)[0])
                                //.Average(i2 => i.Close.Return().Correlation(i2.Close.Return(), LOOKBACK_C)[0])
                                //.Average(i2 => i.Close.LogReturn().Correlation(i2.Close.LogReturn(), LOOKBACK_C)[0])
                        });

                // trigger monthly rebalancing
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // calculate loss function for universe
                    var L = LossFunction(indicators);

                    // Keller is adamant to filter for absolute momentum
                    // after ranking for the loss function
                    var top = L.Keys
                        .OrderBy(i => L[i])
                        .Take(N)
                        .Where(i => indicators[i].a > RMIN / 100.0)
                        .ToList();

                    // initialize all weights to zero
                    var weights = indicators.Keys
                        .ToDictionary(
                            i => i,
                            i => 0.0);

                    // assign weights for top instruments
                    for (int n = 0; n < top.Count; n++)
                    {
                        // N = 3, a = 0.5: weights = 2.5/6, 2.0/6, 1.5/6
                        //                           0.417  0.333  0.250
                        weights[top[n]] +=
                            (1.0 - A / 100.0) / N
                            + A / 100.0 * (N - n) / ((N + 1.0) * N / 2.0);
                    }

                    // assign any leftover weight to safe instrument
                    weights[FindInstrument(U_SAFE)] += 1.0 - weights.Sum(kv => kv.Value);
                    //weights[FindInstrument(U_SAFE)] += (N - top.Count) / (double)N;

                    // submit orders
                    foreach (var i in weights.Keys)
                    {
                        var shares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(shares - i.Position);
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));

#if true
                    // additional plotter output
                    _plotter.SelectChart("Factor R", "Date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in indicators.Keys)
                        _plotter.Plot(i.Symbol, indicators[i].r);

                    _plotter.SelectChart("Factor V", "Date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in indicators.Keys)
                        _plotter.Plot(i.Symbol, indicators[i].v);

                    _plotter.SelectChart("Factor C", "Date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in indicators.Keys)
                        _plotter.Plot(i.Symbol, indicators[i].c);
#endif

                    if (IsSubclassed) AddSubclassedBar();
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    #region U7 Universe
    static class Keller_FAA_U7
    {
#if MUTUAL_FUNDS
        public static List<string> Universe => new List<string>
        {
            // global stocks
            "yahoo:VTSMX",
            "yahoo:FDIVX",
            "yahoo:VEIEX",
            // US bonds
            SafeInstrument,
            "yahoo:VBMFX",
            // commodities
            "yahoo:QRAAX",
            // REITs
            "yahoo:VGSIX"
        };
        public static string SafeInstrument => "yahoo:VFISX";
#else
        public static List<string> Universe => new List<string>
        {
            // global stocks
            "splice:VTI,yahoo:VTSMX",
            "splice:VEA,yahoo:FDIVX",
            "splice:VWO,yahoo:VEIEX",
            // US bonds
            SafeInstrument,
            "splice:BND,yahoo:VBMFX",
            // commodities
            "splice:GSG,yahoo:QRAAX",
            // REITs
            "splice:VNQ,yahoo:VGSIX"
        };
        public static string SafeInstrument => "splice:SHY,yahoo:VFISX";
#endif
    }
    #endregion

    #region Equal-Weighted Benchmark
    public class Keller_FAA_EW : LazyPortfolio
    {
        public override string Name => "Keller's FAA: EW Benchmark";

        private HashSet<Tuple<string, double>> _allocation;
        public Keller_FAA_EW()
        {
            _allocation = new HashSet<Tuple<string, double>>();

            foreach (var a in Keller_FAA_U7.Universe)
                _allocation.Add(Tuple.Create(a, 0.0));
        }
        public override HashSet<Tuple<string, double>> ALLOCATION => _allocation;
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion

    #region FAA Example 1: Relative momentum (factor R)
    public class Keller_FAA_Example1_R : Keller_FAA_Core
    {
        public override string Name => "Keller's FAA (Example 1: R Momentum)";
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
        public override int RMIN => -999; // defeat absolute momentum
    }
    #endregion
    #region FAA Example 2: Absolute momentum (factor A)
    public class Keller_FAA_Example2_RA : Keller_FAA_Core
    {
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
    }
    #endregion
    #region FAA Example 3: Generalized momentum with factors R, A and V
    public class Keller_FAA_Example3_RAV : Keller_FAA_Core
    {
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
        public override int LOOKBACK_V { get; set; } = 84;
        public override int WV { get; set; } = 50;
    }
    #endregion
    #region FAA Example 4: Correlations
    public class Keller_FAA_Example4_RAVC : Keller_FAA_Core
    {
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
        public override int LOOKBACK_V { get; set; } = 84;
        public override int LOOKBACK_C { get; set; } = 84;
        public override int WV { get; set; } = 50;
        public override int WC { get; set; } = 50;
    }
    #endregion
    #region FAA Example: Logarithmic loss function
    public class Keller_FAA_Example_LogLoss : Keller_FAA_Core
    {
        public override string Name => "Keller's FAA (Example of logarithmic loss function)";
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
        public override int LOOKBACK_V { get; set; } = 84;
        public override int LOOKBACK_C { get; set; } = 84;
        public override int WV { get; set; } = 50;
        public override int WC { get; set; } = 50;

        protected override Dictionary<Instrument, double> LossFunction(Dictionary<Instrument, indicatorValues> indicators)
        {
            var rmax = indicators.Max(kv => kv.Value.r);
            var vmin = indicators.Min(kv => kv.Value.v);
            var cmin = indicators.Min(kv => kv.Value.c);

            // loss function L(i), see equation (3)
            var L = indicators.Keys
                .ToDictionary(
                    i => i,
                    i => -1.0 * 
                        (WR / 100.0 * Math.Log(indicators[i].r / rmax)
                        + WV / 100.0 * Math.Log(vmin / indicators[i].v)
                        + WC / 100.0 * Math.Log((cmin + 1.0) / (indicators[i].c + 1.0)))
                );

            return L;
        }
    }
    #endregion

    #region FAA: Optimized for "best" parameters
    public class Keller_FAA_Optimized : Keller_FAA_Core
    {
        protected override List<string> U => Keller_FAA_U7.Universe;
        protected override string U_SAFE => Keller_FAA_U7.SafeInstrument;
        public override int N { get; set; } = 3;
        public override int LOOKBACK_R { get; set; } = 84;
        public override int LOOKBACK_V { get; set; } = 84;
        public override int LOOKBACK_C { get; set; } = 84;
        public override int WV { get; set; } = 80;
        public override int WC { get; set; } = 60;
    }
    #endregion
}

//==============================================================================
// end of file