//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        GlueLogic
// Description: some glue to help re-using algorithms for other applications
// History:     2019x02, FUB, created
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

// USE_NORGATE_UNIVERSE
// defined: use survivorship-free universe through Norgate Data
// undefined: use fixed test univese with hefty survivorship bias
//#define USE_NORGATE_UNIVERSE

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Algorithms.Glue
{
    #region AlgorithmPlusGlue
    /// <summary>
    /// Class adding some convenient features to 
    /// the Algorithm base, most notably
    /// a Plotter and an AllocationTracker.
    /// </summary>
    public abstract class AlgorithmPlusGlue : Algorithm
    {
        protected readonly Plotter _plotter;
        public readonly AllocationTracker Alloc;

        public AlgorithmPlusGlue()
        {
            _plotter = new Plotter(this);
            Alloc = new AllocationTracker(this);
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
    }
    #endregion

    #region global constants
    /// <summary>
    /// constants used across all algorithms
    /// </summary>
    public static class Globals
    {
        public static readonly DateTime WARMUP_START_TIME = DateTime.Parse("01/01/2006", CultureInfo.InvariantCulture);
        public static readonly DateTime START_TIME = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
        //public static DateTime END_TIME = DateTime.Now.Date + TimeSpan.FromHours(16);
        public static readonly DateTime END_TIME = DateTime.Now.Date - TimeSpan.FromDays(5);

        public static readonly double INITIAL_CAPITAL = 1e6;
        public static readonly double COMMISSION = 0.015;
    }
    #endregion
    #region commonly used universes
    /// <summary>
    /// universes used across all algorithms
    /// </summary>
    public static class Universes
    {
#if USE_NORGATE_UNIVERSE
        public static readonly Universe STOCKS_US_LG_CAP = Universe.New("$SPX");
        public static readonly Universe STOCKS_US_TOP_100 = Universe.New("$OEX");
        public static readonly Universe STOCKS_US_ALL = Universe.New("$RUA");
        public static readonly Universe STOCKS_US_LG_MID_SMALL_CAP = Universe.New("$SP1500");
#else
        #region test universe
        private class TestUniverse : Universe
        {
            // this is a small test universe so that we can demo the
            // algorithm code. for proper functionality, a Norgate
            // subscription is required
            private static List<string> SP500_Top25 = new List<string>()
            {
                // see here: https://www.slickcharts.com/sp500

                // top 25 companies, representing ~40% of 
                // the S&P 500's total market capitalization
                "MSFT",  // Microsoft Corporation
                "AAPL",  // Apple Inc.
                "AMZN",  // Amazon.com Inc.
                "FB",    // Facebook Inc. Class A
                "JNJ",   // Johnson & Johnson
                "GOOG",  // Alphabet Inc. Class C
                "GOOGL", // Alphabet Inc. Class A
                "BRK.B", // Berkshire Hathaway Inc. Class B
                "PG",    // Procter & Gamble Company
                "JPM",   // JPMorgan Chase & Co.
                "V",     // Visa Inc. Class A
                "INTC",  // Intel Corporation
                "UNH",   // UnitedHealth Group Incorporated
                "VZ",    // Verizon Communications Inc.
                "MA",    // Mastercard Incorporated Class A
                "T",     // AT&T Inc.
                "HD",    // Home Depot Inc.
                "MRK",   // Merck & Co. Inc.
                "PFE",   // Pfizer Inc.
                "PEP",   // PepsiCo Inc.
                "BAC",   // Bank of America Corp
                "DIS",   // Walt Disney Company
                "KO",    // Coca-Cola Company
                "WMT",   // Walmart Inc.
                "CSCO",  // Cisco Systems Inc.
            };
            public override IEnumerable<string> Constituents => SP500_Top25;
            public override bool IsConstituent(string nickname, DateTime timestamp)
            {
                return true;
            }
        }
        #endregion
        public static readonly Universe STOCKS_US_LG_CAP = new TestUniverse();
        public static readonly Universe STOCKS_US_ALL = new TestUniverse();
#endif

    }
    #endregion
    #region commonly used assets and proxies
    /// <summary>
    /// assets used across all algorithms
    /// </summary>
    public static class Assets
    {
        //----- stocks
        public static readonly string STOCKS_US_LG_CAP = "splice:SPY,VFIAX,VFINX";

        public static readonly string STOCKS_US_SECT_MATERIALS = "splice:XLB,VMIAX"; // Materials
        public static readonly string STOCKS_US_SECT_COMMUNICATION = "splice:XLC,VTCAX"; // Communication Services
        public static readonly string STOCKS_US_SECT_ENERGY = "splice:XLE,VENAX"; // Energy
        public static readonly string STOCKS_US_SECT_FINANCIAL = "splice:XLF,VFAIX"; // Financial
        public static readonly string STOCKS_US_SECT_INDUSTRIAL = "splice:XLI,VINAX"; // Industrial
        public static readonly string STOCKS_US_SECT_TECHNOLOGY = "splice:XLK,VITAX"; // Technology
        public static readonly string STOCKS_US_SECT_STAPLES = "splice:XLP,VCSAX"; // Consumer Staples
        public static readonly string STOCKS_US_SECT_REAL_ESTATE = "splice:XLRE,VGSLX"; // Real Estate
        public static readonly string STOCKS_US_SECT_UTILITIES = "splice:XLU,VUIAX"; // Utilities
        public static readonly string STOCKS_US_SECT_HEALTH_CARE = "splice:XLV,VHCIX"; // Health Care
        public static readonly string STOCKS_US_SECT_DISCRETIONARY = "splice:XLY,VCDAX"; // Consumer Discretionary

        public static readonly string STOCKS_WXUS_LG_MID_CAP = "splice:ACWX,SCINX";

        //----- bonds
        public static readonly string BONDS_US_TOTAL = "splice:AGG,FUSGX";
        public static readonly string BONDS_US_TREAS_3M = "splice:BIL,PRTBX";
        public static readonly string BONDS_US_TREAS_3Y = "SHY"; // 1-3yr US Treasuries
        public static readonly string BONDS_US_TREAS_7Y = "IEI"; // 3-7yr US Treasuries
        public static readonly string BONDS_US_TREAS_10Y = "IEF"; //7-10yr 
        public static readonly string BONDS_US_TREAS_30Y = "TLT";  // long-term (20+yr) government bonds
        public static readonly string BONDS_US_CORP_10Y = "IGIB"; // intermediate-term corporate bonds
        public static readonly string BONDS_US_CORP_JUNK = "HYG";

        public static readonly string BONDS_WRLD_TREAS = "BWX";

        //----- real estate
        public static readonly string REIT_US = "VNQ"; // Vanguard Real Estate Index ETF
        public static readonly string MREIT_US = "REM"; // iShares Mortgage Real Estate ETF

        //----- commodities
        public static readonly string GOLD = "GLD"; // gold
        public static readonly string COMMODITIES = "DBC"; // commodities

        //----- portfolios
        public static readonly string PORTF_0 = "algorithm:ZeroReturn";
        public static readonly string PORTF_60_40 = "@60_40";

        //----- leveraged
        public static readonly string STOCKS_US_LG_CAP_3X = "SPXL";
        public static readonly string BONDS_US_TREAS_30Y_3X = "TMF";
    }
    #endregion
    #region allocation tracker
    /// <summary>
    /// helper class to track strategy target allocation
    /// </summary>
    public class AllocationTracker
    {
        #region internal data
        private readonly _Allocation _alloc;
        #endregion
        #region internal helpers
        public interface IAllocation
        {
            double this[Instrument instr] { get; set; }
            IEnumerable<Instrument> Keys { get; }
            bool ContainsKey(Instrument instr);
            void Clear();
        }
        private class _Allocation : IAllocation
        {
            private readonly Dictionary<Instrument, double> _weights = new Dictionary<Instrument, double>();
            private readonly Algorithm _algo;

            public _Allocation(Algorithm algo) => _algo = algo;
            public double this[Instrument instr]
            {
                get => _weights[instr];
                set 
                {
                    _weights[instr] = value;
                    _lastUpdate = _algo.SimTime[0];
                }
            }
            public IEnumerable<Instrument> Keys => _weights.Keys;
            public void Clear()
            {
                _weights.Clear();
                _lastUpdate = _algo.SimTime[0];
            }
            public bool ContainsKey(Instrument instr) => _weights.ContainsKey(instr);
            public DateTime _lastUpdate { get; private set; }
        }
        #endregion

        public AllocationTracker(Algorithm algo) => _alloc = new _Allocation(algo);
        public DateTime LastUpdate => _alloc._lastUpdate;
        public IAllocation Allocation => _alloc;
    }
    #endregion
    #region plotter extension functions
    /// <summary>
    /// plotter extension functions
    /// </summary>
    public static class PlotterExtensions
    {
        /// <summary>
        /// Add strategy NAV and benchmark to plotter. It is recommended
        /// to call this method at least once per simulated trading day.
        /// Some templates, especially the SimpleReport template, require
        /// NAV and benchmark to be logged to the first plotter chart,
        /// so that various charts can be generated from that:
        /// equity curve, metrics, annual bars, return distribution,
        /// and monte-carlo analysis.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="algo"></param>
        /// <param name="benchmark"></param>
        public static void AddNavAndBenchmark(this Plotter plotter, Algorithm algo, Instrument benchmark)
        {
            plotter.SelectChart(algo.Name, "Date");
            plotter.SetX(algo.SimTime[0]);
            plotter.Plot(algo.Name, algo.NetAssetValue[0]);
            plotter.Plot(benchmark.Name, benchmark.Close[0]);
        }

        /// <summary>
        /// Add strategy target allocation to plotter. This method should
        /// be called exactly once after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="alloc"></param>
        public static void AddTargetAllocation(this Plotter plotter, AllocationTracker alloc)
        {
            plotter.SelectChart(string.Format("{0} as of {1:MM/dd/yyyy}", 
                Plotter.SheetNames.HOLDINGS, alloc.LastUpdate), "Name");

            foreach (Instrument i in alloc.Allocation.Keys.OrderByDescending(k => alloc.Allocation[k]))
            {
                plotter.SetX(i.Name);
                plotter.Plot("Symbol", i.Symbol);
                plotter.Plot("Allocation", string.Format("{0:P2}", alloc.Allocation[i]));
            }
        }

        /// <summary>
        /// Add new row with strategy target allocation to plotter. This
        /// method should be called once for every time the strategy submits 
        /// orders to adjust the target allocation.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="alloc"></param>
        public static void AddTargetAllocationRow(this Plotter plotter, AllocationTracker alloc)
        {
            // TODO: write historical allocations here
        }

        /// <summary>
        /// Add new row with strategy holdings to plotter. This method should
        /// be called at least once per simulated trading day.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>
        /// <param name="assets"></param>
        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, IEnumerable<Instrument> assets)
        {
            plotter.SelectChart(Plotter.SheetNames.EXPOSURE_VS_TIME, "date");
            plotter.SetX(sim.SimTime[0]);

            foreach (var i in assets)
            {
                var pcnt = i.Position * i.Close[0] / sim.NetAssetValue[0];
                plotter.Plot(i.Symbol, pcnt);
            }

            //plotter.Plot("Total", sim.Positions.Sum(p => Math.Abs(p.Value * p.Key.Close[0] / sim.NetAssetValue[0])));
        }

        /// <summary>
        /// Add new row with strategy holdings to plotter. This method should
        /// be called at least once per simulated trading day.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>
        /// <param name="asset"></param>
        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, Instrument asset)
        {
            var pcnt = asset.Position * asset.Close[0] / sim.NetAssetValue[0];

            plotter.SelectChart(Plotter.SheetNames.EXPOSURE_VS_TIME, "date");
            plotter.SetX(sim.SimTime[0]);
            plotter.Plot(asset.Symbol, pcnt);
        }

        /// <summary>
        /// Add order log to plotter. This method should be called exactly
        /// once after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>
        public static void AddOrderLog(this Plotter plotter, SimulatorCore sim)
        {
            plotter.SelectChart("Order Log", "date");
            foreach (LogEntry entry in sim.Log)
            {
                plotter.SetX(string.Format("{0:MM/dd/yyyy}", entry.BarOfExecution.Time));
                plotter.Plot("action", entry.Action);
                plotter.Plot("type", entry.InstrumentType);
                plotter.Plot("instr", entry.Symbol);
                plotter.Plot("qty", entry.OrderTicket.Quantity);
                plotter.Plot("fill", entry.FillPrice);
                plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                plotter.Plot("commission", -entry.Commission);
                plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
        }

        /// <summary>
        /// Add position log to plotter. This method should be called exactly
        /// once after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>

        public static void AddPositionLog(this Plotter plotter, SimulatorCore sim)
        {
            var tradeLog = LogAnalysis
                .GroupPositions(sim.Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            plotter.SelectChart("Position Log", "entry date");
            foreach (var trade in tradeLog)
            {
                plotter.SetX(string.Format("{0:MM/dd/yyyy}", trade.Entry.BarOfExecution.Time));
                plotter.Plot("exit date", string.Format("{0:MM/dd/yyyy}", trade.Exit.BarOfExecution.Time));
                plotter.Plot("days held", (trade.Exit.BarOfExecution.Time - trade.Entry.BarOfExecution.Time).TotalDays);
                plotter.Plot("Symbol", trade.Symbol);
                plotter.Plot("Quantity", trade.Quantity);
                plotter.Plot("% Profit", Math.Round((trade.Quantity > 0 ? 100.0 : -100.0) * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0), 2));
                plotter.Plot("Exit", trade.Exit.OrderTicket.Comment ?? "");
                //plotter.Plot("$ Profit", trade.Quantity * (trade.Exit.FillPrice - trade.Entry.FillPrice));
            }
        }

        /// <summary>
        /// Add average strategy holdings to plotter. As a prerequisite, this method 
        /// requires that strategy holdings have been logged. It should be called
        /// exactly once after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="algo"></param>

        public static void AddAverageHoldings(this Plotter plotter, Algorithm algo) { }

        /// <summary>
        /// Add PnL vs hold time to plotter. This method should be called once
        /// after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>

        public static void AddPnLHoldTime(this Plotter plotter, SimulatorCore sim)
        {
            var tradeLog = LogAnalysis
                .GroupPositions(sim.Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            plotter.SelectChart(Plotter.SheetNames.PNL_HOLD_TIME, "Days Held");
            foreach (var trade in tradeLog)
            {
                var pnl = (trade.Quantity > 0 ? 100.0 : -100.0) * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0);
                var label = pnl > 0.0 ? "Profit" : "Loss";
                plotter.SetX((trade.Exit.BarOfExecution.Time - trade.Entry.BarOfExecution.Time).TotalDays);
                plotter.Plot(label, pnl);
            }
        }

        /// <summary>
        /// Add MFE/ MAE analysis to plotter. This method should be called once
        /// after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="sim"></param>

        public static void AddMfeMae(this Plotter plotter, SimulatorCore sim)
        {
            var tradeLog = LogAnalysis
                .GroupPositions(sim.Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            plotter.SelectChart(Plotter.SheetNames.PNL_MFE_MAE, "Max Excursion");
            foreach (var trade in tradeLog)
            {
                var pnl = 100.0 * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0);
                var label = pnl > 0.0 ? "Profit" : "Loss";

                plotter.SetX(100.0 * (trade.HighestHigh / trade.Entry.FillPrice - 1.0));
                plotter.Plot(label, pnl);

                plotter.SetX(100.0 * (trade.LowestLow / trade.Entry.FillPrice - 1.0));
                plotter.Plot(label, pnl);
            }
        }

        /// <summary>
        /// Add optimizable algorithm parameters to plotter. This method should
        /// be called once after the simulation loop finished.
        /// </summary>
        /// <param name="plotter"></param>
        /// <param name="algo"></param>
        public static void AddParameters(this Plotter plotter, Algorithm algo)
        {
            plotter.SelectChart("Parameters", "Name");
            foreach (var param in algo.OptimizerParams.Keys)
            {
                plotter.SetX(algo.OptimizerParams[param].Name);
                plotter.Plot("Value", algo.OptimizerParams[param].Value);

            }
        }
    }
    #endregion
    #region simulator extension functions
    /// <summary>
    /// simulator extension functions
    /// </summary>
    public static class SimExtensions
    {

        public static double CalcFitness(this SimulatorCore sim)
        {
#if true
            double cagr = Math.Exp(252.0 / Math.Max(1, sim.TradingDays) 
                * Math.Log(sim.NetAssetValue[0] / Globals.INITIAL_CAPITAL)) - 1.0;
            double mdd = Math.Max(0.01, sim.NetAssetValueMaxDrawdown);
            return cagr / mdd;
#else
            // calculate Keller ratio
            double R = Math.Exp(
                252.0 / sim.TradingDays * Math.Log(sim.NetAssetValue[0] / Globals.INITIAL_FUNDS));
            double K50 = sim.NetAssetValueMaxDrawdown < 0.5 && R > 0.0
                ? R * (1.0 - sim.NetAssetValueMaxDrawdown / (1.0 - sim.NetAssetValueMaxDrawdown))
                : 0.0;
            double K25 = sim.NetAssetValueMaxDrawdown < 0.25 && R > 0.0
                ? R * (1.0 - 2.0 * sim.NetAssetValueMaxDrawdown / (1.0 - 2 * sim.NetAssetValueMaxDrawdown))
                : 0.0;

            return K25;
#endif
        }
    }
    #endregion
}

//==============================================================================
// end of file