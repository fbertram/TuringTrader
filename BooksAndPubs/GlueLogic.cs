//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        GlueLogic
// Description: some glue to help re-using algorithms for other applications
// History:     2019x02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2098, Bertram Solutions LLC
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
using System.Text;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
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

        public static readonly string STOCK_MARKET = "SPY";
        public static readonly string BALANCED_PORTFOLIO = "@60_40";

#if USE_NORGATE_UNIVERSE
        public static Universe LARGE_CAP_UNIVERSE = Universe.New("$SPX");
#else
        #region test universe
        private class TestUniverse : Universe
        {
            private static List<string> NDX = new List<string>()
        {
            // Trade all US stocks, but filter out 
            // * ETFs; 
            // * stocks < $10; and 
            // * average daily volume< 500,000 over last 50 days.

            // here, we use Nasdaq 100, as of 03/21/2019
            "AAL",
            "AAPL",
            "ADBE",
            "ADI",
            "ADP",
            "ADSK",
            "ALGN",
            "ALXN",
            "AMAT",
            "AMD",
            "AMGN",
            "AMZN",
            "ASML",
            "ATVI",
            "AVGO",
            "BIDU",
            "BIIB",
            "BKNG",
            "BMRN",
            "CDNS",
            //"CELG",
            "CERN",
            "CHKP",
            "CHTR",
            "CMCSA",
            "COST",
            "CSCO",
            "CSX",
            "CTAS",
            //"CTRP", // delisted
            "CTSH",
            "CTXS",
            "DLTR",
            "EA",
            "EBAY",
            "EXPE",
            "FAST",
            "FB",
            "FISV",
            "GILD",
            "GOOG",
            "GOOGL",
            "HAS",
            "HSIC",
            "IDXX",
            "ILMN",
            "INCY",
            "INTC",
            "INTU",
            "ISRG",
            "JBHT",
            "JD",
            "KHC",
            "KLAC",
            "LBTYA",
            "LBTYK",
            "LRCX",
            "LULU",
            "MAR",
            "MCHP",
            "MDLZ",
            "MELI",
            "MNST",
            "MSFT",
            "MU",
            "MXIM",
            "MYL",
            "NFLX",
            "NTAP",
            "NTES",
            "NVDA",
            "NXPI",
            "ORLY",
            "PAYX",
            "PCAR",
            "PEP",
            "PYPL",
            "QCOM",
            "REGN",
            "ROST",
            "SBUX",
            "SIRI",
            "SNPS",
            "SWKS",
            //"SYMC", // delisted
            //"TFCF", // delisted
            //"TFCFA", // delisted
            "TMUS",
            "TSLA",
            "TTWO",
            "TXN",
            "UAL",
            "ULTA",
            "VRSK",
            "VRSN",
            "VRTX",
            "WBA",
            "WDAY",
            "WDC",
            "WLTW",
            "WYNN",
            "XEL",
            "XLNX",
        };
            private static List<string> OEX = new List<string>()
        {
            // Trade all US stocks, but filter out 
            // * ETFs; 
            // * stocks < $10; and 
            // * average daily volume< 500,000 over last 50 days.

            // here, we use S&P 100, as of 03/20/2019
            "AAPL",
            "ABBV",
            "ABT",
            "ACN",
            "ADBE",
            "AGN",
            "AIG",
            "ALL",
            "AMGN",
            "AMZN",
            "AXP",
            "BA",
            "BAC",
            "BIIB",
            "BK",
            "BKNG",
            "BLK",
            "BMY",
            "BRK.B",
            "C",
            "CAT",
            //"CELG",
            "CHTR",
            "CL",
            "CMCSA",
            "COF",
            "COP",
            "COST",
            "CSCO",
            "CVS",
            "CVX",
            "DHR",
            "DIS",
            "DUK",
            //"DWDP",
            "EMR",
            "EXC",
            "F",
            "FB",
            "FDX",
            "GD",
            "GE",
            "GILD",
            "GM",
            "GOOG",
            "GOOGL",
            "GS",
            "HAL",
            "HD",
            "HON",
            "IBM",
            "INTC",
            "JNJ",
            "JPM",
            "KHC",
            "KMI",
            "KO",
            "LLY",
            "LMT",
            "LOW",
            "MA",
            "MCD",
            "MDLZ",
            "MDT",
            "MET",
            "MMM",
            "MO",
            "MRK",
            "MS",
            "MSFT",
            "NEE",
            "NFLX",
            "NKE",
            "NVDA",
            "ORCL",
            "OXY",
            "PEP",
            "PFE",
            "PG",
            "PM",
            "PYPL",
            "QCOM",
            "RTN",
            "SBUX",
            "SLB",
            "SO",
            "SPG",
            "T",
            "TGT",
            "TXN",
            "UNH",
            "UNP",
            "UPS",
            "USB",
            "UTX",
            "V",
            "VZ",
            "WBA",
            "WFC",
            "WMT",
            "XOM",
        };
            public override IEnumerable<string> Constituents => NDX.Concat(OEX).ToList();
            public override bool IsConstituent(string nickname, DateTime timestamp)
            {
                return true;
            }
        }
        #endregion
        public static Universe LARGE_CAP_UNIVERSE = new TestUniverse();
#endif
    }
    #endregion
    #region global assets
    public static class Assets
    {
        //----- stocks
        public static readonly string STOCKS_US_LG_CAP = "splice:SPY,yahoo:VFINX";
        public static readonly string STOCKS_XUS_LG_MID_CAP = "splice:ACWX,yahoo:SCINX";

        //----- bonds
        public static readonly string BONDS_US_TOTAL = "splice:AGG,yahoo:FUSGX";
        public static readonly string BONDS_US_TBILL = "splice:BIL,yahoo:PRTBX";

        //----- portfolios
        public static readonly string PORTF_60_40 = "yahoo:VBINX";
    }
    #endregion
    #region allocation tracker
    /// <summary>
    /// helper class to track strategy target allocation
    /// </summary>
    public class AllocationTracker
    {
        public DateTime LastUpdate { get; set; }
        public Dictionary<Instrument, double> Allocation  = new Dictionary<Instrument, double>();
    }
    #endregion
    #region plotter extension functions
    /// <summary>
    /// plotter extension functions
    /// </summary>
    public static class PlotterExtensions
    {
        public static void AddNavAndBenchmark(this Plotter plotter, SimulatorCore sim, Instrument benchmark)
        {
            plotter.SelectChart(sim.Name, "Date");
            plotter.SetX(sim.SimTime[0]);
            plotter.Plot(sim.Name, sim.NetAssetValue[0]);
            plotter.Plot(benchmark.Name, benchmark.Close[0]);
        }
        public static void AddTargetAllocation(this Plotter plotter, AllocationTracker alloc)
        {
            plotter.SelectChart(string.Format("Target Allocation as of {0:MM/dd/yyyy}", alloc.LastUpdate), "Name");

            foreach (Instrument i in alloc.Allocation.Keys.OrderByDescending(k => alloc.Allocation[k]))
            {
                plotter.SetX(i.Name);
                plotter.Plot("Symbol", i.Symbol);
                plotter.Plot("Allocation", string.Format("{0:P2}", alloc.Allocation[i]));
            }
        }

        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, IEnumerable<Instrument> assets)
        {
            plotter.SelectChart("Exposure vs Time", "date");
            plotter.SetX(sim.SimTime[0]);

            foreach (var i in assets)
            {
                var pcnt = i.Position * i.Close[0] / sim.NetAssetValue[0];
                plotter.Plot(i.Symbol, pcnt);
            }

            plotter.Plot("Total", sim.Positions.Sum(p => Math.Abs(p.Value * p.Key.Close[0] / sim.NetAssetValue[0])));
        }

        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, Instrument asset)
        {
            var pcnt = asset.Position * asset.Close[0] / sim.NetAssetValue[0];

            plotter.SelectChart("Exposure vs Time", "date");
            plotter.SetX(sim.SimTime[0]);
            plotter.Plot(asset.Symbol, pcnt);
        }

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

        public static void AddPnLHoldTime(this Plotter plotter, SimulatorCore sim)
        {
            var tradeLog = LogAnalysis
                .GroupPositions(sim.Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            plotter.SelectChart("P&L vs Hold Time", "Days Held");
            foreach (var trade in tradeLog)
            {
                var pnl = (trade.Quantity > 0 ? 100.0 : -100.0) * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0);
                var label = pnl > 0.0 ? "Profit" : "Loss";
                plotter.SetX((trade.Exit.BarOfExecution.Time - trade.Entry.BarOfExecution.Time).TotalDays);
                plotter.Plot(label, pnl);
            }
        }

        public static void AddMfeMae(this Plotter plotter, SimulatorCore sim)
        {
            var tradeLog = LogAnalysis
                .GroupPositions(sim.Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            plotter.SelectChart("P&L vs Maximum Excursion", "Max Excursion");
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