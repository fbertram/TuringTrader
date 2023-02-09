//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Plotter
// Description: Plotter class w/ additional helpers for v2 engine.
// History:     2022x31, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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

// RESOLVE_CHILD_HOLDINGS: if defined, resolve asset holdings of child strategies
#define RESOLVE_CHILD_HOLDINGS

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Plotter class with additional helper functions for v2 engine.
    /// </summary>
    public class Plotter : Simulator.Plotter
    {
        private Algorithm Algorithm => (Algorithm)ParentAlgorithm;
        public Plotter(Algorithm algorithm) : base(algorithm)
        { }

        /// <summary>
        /// Add trade log. This log includes one entry for every trade executed.
        /// Each entry summarizes the instrument traded, along with order size,
        /// fill prices, order amount, and trade friction.
        /// </summary>
        public void AddTradeLog()
        {
            SelectChart("Trade Log", "Date");
            foreach (var trade in Algorithm.Account.TradeLog)
            {
                SetX(string.Format("{0:MM/dd/yyyy}", trade.OrderTicket.SubmitDate));
                // Plot("action", entry.Action);
                // Plot("type", entry.InstrumentType);
                Plot("instr", Algorithm.Asset(trade.OrderTicket.Name).Ticker);
                Plot("qty", string.Format("{0:P2}", trade.OrderSize));
                Plot("fill", string.Format("{0:C2}", trade.FillPrice));
                Plot("gross", string.Format("{0:C2}", trade.OrderAmount + trade.FrictionAmount));
                Plot("friction", string.Format("{0:C2}", trade.FrictionAmount));
                Plot("net", string.Format("{0:C2}", trade.OrderAmount));
                //plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
        }

        /// <summary>
        /// Add target allocation. The target allocation includes one row
        /// for each asset held. Each row has the asset's symbol, full name,
        /// target allocation, and last price.
        /// </summary>
        public void AddTargetAllocation()
        {
            var holdings = new Dictionary<string, double>();
            var names = new Dictionary<string, string>();
            var prices = new Dictionary<string, double>();

            void addAssetAllocation(Algorithm algo, double scale = 1.0)
            {
                foreach (var kv in algo.Positions)
                {
                    var asset = algo.Asset(kv.Key);

#if RESOLVE_CHILD_HOLDINGS
                    // optional code to resolve holdings of child strategies
                    if (asset.Meta.Generator != null)
                    {
                        addAssetAllocation(asset.Meta.Generator, kv.Value * scale);
                    }
                    else
#endif
                    {
                        var ticker = asset.Ticker;

                        if (!holdings.ContainsKey(ticker))
                        {
                            holdings[ticker] = 0.0;
                            names[ticker] = asset.Description;
                            prices[ticker] = asset.Close[0];
                        }

                        holdings[ticker] += kv.Value * scale;
                    }
                }
            }
            addAssetAllocation(Algorithm);

            SelectChart(Simulator.Plotter.SheetNames.HOLDINGS, "Symbol");
            foreach (var ticker in holdings.Keys.OrderByDescending(ticker => holdings[ticker]))
            {
                SetX(ticker);
                Plot("Name", names[ticker]);
                Plot("Allocation", string.Format("{0:P2}", holdings[ticker]));
                Plot("Price", string.Format("{0:C2}", prices[ticker]));
            }

            // BUGBUG: this is incorrect as it does not consider the child strategies
            SelectChart(Simulator.Plotter.SheetNames.LAST_REBALANCE, "Key");
            SetX("LastRebalance");
            Plot("Value", Algorithm.Account.TradeLog.Last().OrderTicket.SubmitDate);
        }

        /// <summary>
        /// Add historical asset allocations. This log contains one row
        /// for each rebalancing day. Each row summarizes the symbol held,
        /// along with the target allocation for that symbol.
        /// </summary>
        public void AddHistoricalAllocations()
        {
            SelectChart(Simulator.Plotter.SheetNames.HOLDINGS_HISTORY, "Date");

            var lastDate = default(DateTime);
            var lastAlloc = "";

            foreach (var trade in Algorithm.Account.TradeLog)
            {
                if (trade.OrderTicket.SubmitDate != lastDate)
                {
                    if (lastAlloc != "")
                    {
                        //SetX(lastDate);
                        SetX(string.Format("{0:MM/dd/yyyy}", lastDate));
                        Plot("Allocation", lastAlloc);
                    }

                    lastDate = trade.OrderTicket.SubmitDate;
                    lastAlloc = "";
                }

                lastAlloc = lastAlloc
                    + ((lastAlloc == "") ? "" : ", ")
                    + string.Format("{0}={1:P2}", Algorithm.Asset(trade.OrderTicket.Name).Ticker, trade.OrderTicket.TargetAllocation);
            }

            if (lastAlloc != "")
            {
                //SetX(lastDate);
                SetX(string.Format("{0:MM/dd/yyyy}", lastDate));
                Plot("Allocation", lastAlloc);
            }
        }

        /// <summary>
        /// Add parameters. This page summarizes strategy parameters and 
        /// current values. This is especially helpful while optimizing
        /// strategy parameters.
        /// </summary>
        public void AddParameters()
        {
            SelectChart("Parameters", "Name");
            foreach (var param in Algorithm.OptimizerParams.Keys)
            {
                SetX(Algorithm.OptimizerParams[param].Name);
                Plot("Value", Algorithm.OptimizerParams[param].Value);

            }
        }

        /// <summary>
        /// Add OHLCV bars for algorithm's NAV. This is useful for creating
        /// backfills.
        /// </summary>
        public void AddNavOHLCV()
        {
#if false
            SelectChart("OHLCV", "Date");
            foreach (var param in par
            {
                plotter.SetX(algo.OptimizerParams[param].Name);
                plotter.Plot("Value", algo.OptimizerParams[param].Value);

            }
#endif
        }
    }
}

//==============================================================================
// end of file
