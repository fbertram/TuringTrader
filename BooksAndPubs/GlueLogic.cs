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
    static class Globals
    {
        public static DateTime WARMUP_START_TIME = DateTime.Parse("01/01/2006", CultureInfo.InvariantCulture);
        public static DateTime START_TIME = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
        //public static DateTime END_TIME = DateTime.Now.Date + TimeSpan.FromHours(16);
        public static DateTime END_TIME = DateTime.Now.Date - TimeSpan.FromDays(5);

        public static double INITIAL_CAPITAL = 1e6;
        public static double COMMISSION = 0.015;

        public static string STOCK_MARKET = "SPY";
        public static string BALANCED_PORTFOLIO = "@60_40";
    }
    #endregion
    #region allocation tracker
    /// <summary>
    /// helper class to track strategy target allocation
    /// </summary>
    class AllocationTracker
    {
        public DateTime LastUpdate { get; set; }
        public Dictionary<Instrument, double> Allocation  = new Dictionary<Instrument, double>();
    }
    #endregion
    #region plotter extension functions
    /// <summary>
    /// plotter extension functions
    /// </summary>
    static class PlotterExtensions
    {
        public static void AddNavAndBenchmark(this Plotter plotter, SimulatorCore sim, Instrument benchmark)
        {
            plotter.SelectChart(sim.Name, "Date");
            plotter.SetX(sim.SimTime[0]);
            plotter.Plot(sim.Name, sim.NetAssetValue[0]);
            plotter.Plot(benchmark.Symbol, benchmark.Close[0]);
        }
        public static void AddTargetAllocation(this Plotter plotter, AllocationTracker alloc)
        {
            plotter.SelectChart(string.Format("Asset Allocation as of {0:MM/dd/yyyy}", alloc.LastUpdate), "Name");

            foreach (Instrument i in alloc.Allocation.Keys.OrderByDescending(k => alloc.Allocation[k]))
            {
                plotter.SetX(i.Name);
                plotter.Plot("Symbol", i.Symbol);
                plotter.Plot("Allocation", string.Format("{0:P2}", alloc.Allocation[i]));
            }
        }

        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, IEnumerable<Instrument> assets)
        {
            foreach (var i in assets)
            {
                var pcnt = i.Position * i.Close[0] / sim.NetAssetValue[0];

                plotter.SelectChart("Holdings over Time", "date");
                plotter.SetX(sim.SimTime[0]);
                plotter.Plot(i.Symbol, pcnt);
            }
        }

        public static void AddStrategyHoldings(this Plotter plotter, SimulatorCore sim, Instrument asset)
        {
            var pcnt = asset.Position * asset.Close[0] / sim.NetAssetValue[0];

            plotter.SelectChart("Holdings over Time", "date");
            plotter.SetX(sim.SimTime[0]);
            plotter.Plot(asset.Symbol, pcnt);
        }

        public static void AddOrderLog(this Plotter plotter, SimulatorCore sim)
        {
            plotter.SelectChart("Strategy Orders", "date");
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

            plotter.SelectChart("Strategy Positions", "entry date");
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