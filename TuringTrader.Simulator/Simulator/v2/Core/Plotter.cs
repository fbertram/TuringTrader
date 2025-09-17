//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Plotter
// Description: Plotter class w/ additional helpers for v2 engine.
// History:     2022x31, FUB, created
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

// RESOLVE_CHILD_HOLDINGS: if defined, resolve asset holdings of child strategies
#define RESOLVE_CHILD_HOLDINGS

// USE_POSITIONS_FLATTENED: if defined, use Algorithm.PositionsFlattened
#define USE_POSITIONS_FLATTENED
#define USE_TRADELOG_FLATTENED

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
        /// <summary>
        /// Our parent algorithm
        /// </summary>
        protected Algorithm Algorithm => (Algorithm)ParentAlgorithm;

        /// <summary>
        /// Create new plotter object.
        /// </summary>
        /// <param name="algorithm">parent algorithm</param>
        public Plotter(Algorithm algorithm) : base(algorithm)
        { }

        /// <summary>
        /// Add trade log. This log includes one entry for every trade executed.
        /// Each entry summarizes the instrument traded, along with order size,
        /// fill prices, order amount, and trade friction.
        /// </summary>
        public void AddTradeLog()
        {
            if (Algorithm.Account.TradeLog == null || Algorithm.Account.TradeLog.Count == 0)
                return;

            // TODO: rewrite to support RESOLVE_CHILD_HOLDINGS???
            SelectChart("Trade Log", "submitted");
            foreach (var trade in Algorithm.Account.TradeLog)
            {
                SetX(string.Format("{0:MM/dd/yyyy}", trade.OrderTicket.SubmitDate));
                Plot("executed", string.Format("{0:MM/dd/yyyy}", trade.ExecDate));
                Plot("instr", Algorithm.Asset(trade.OrderTicket.Name).Ticker);
                Plot("type", trade.OrderTicket.OrderType);
                Plot("target", string.Format("{0:P2}", trade.OrderTicket.TargetAllocation));
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
            if (Algorithm.Positions.Count == 0)
                return;

#if USE_POSITIONS_FLATTENED
            var holdings = Algorithm.PositionsFlattened;

            DateTime lastRebalDateFlattened(Algorithm algo, DateTime parentLastRebal)
            {
                var algoLastRebal = algo.Account.TradeLog != null && algo.Account.TradeLog.Count > 0
                    ? algo.Account.TradeLog.Last().OrderTicket.SubmitDate : default;
                var levelLastRebal = parentLastRebal > algoLastRebal
                    ? parentLastRebal : algoLastRebal;

                foreach (var kv in algo.Positions)
                {
                    var childStrategy = algo.Asset(kv.Key).Meta.Generator;

                    if (childStrategy != null)
                        levelLastRebal = lastRebalDateFlattened(childStrategy, levelLastRebal);
                }

                return levelLastRebal;
            }
            var lastRebalanceDate = lastRebalDateFlattened(Algorithm, default);
#else
            var holdings = new Dictionary<string, double>();
            var names = new Dictionary<string, string>();
            var prices = new Dictionary<string, double>();

            // NOTE: the v2 wrapper for v1 child algorithms doesn't advance SimDate.
            //       As a workaround, we get lastSimDate from the top-level v2 algorithm.
            var lastSimDate = Algorithm.SimDate;
            var lastRebalanceDate = Algorithm.Account.TradeLog != null && Algorithm.Account.TradeLog.Count > 0
                ? Algorithm.Account.TradeLog.Last().OrderTicket.SubmitDate
                : default;

            void addAssetAllocation(Algorithm algo, double scale = 1.0)
            {
                foreach (var kv in algo.Positions)
                {
                    var asset = algo.Asset(kv.Key);

#if RESOLVE_CHILD_HOLDINGS
                    // optional code to resolve holdings of child strategies
                    if (asset.Meta.Generator != null)
                    {
                        if (asset.Meta.Generator.Account.TradeLog != null && asset.Meta.Generator.Account.TradeLog.Count > 0)
                        {
                            var lastRebalanceDateChild = asset.Meta.Generator.Account.TradeLog.Last().OrderTicket.SubmitDate;
                            if (lastRebalanceDateChild > lastRebalanceDate) lastRebalanceDate = lastRebalanceDateChild;
                        }

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
                            prices[ticker] = asset.Close[lastSimDate];
                        }

                        holdings[ticker] += kv.Value * scale;
                    }
                }
            }
            addAssetAllocation(Algorithm);
#endif

            foreach (var nickname in holdings.Keys.OrderByDescending(ticker => holdings[ticker]))
            {
                SelectChart(Simulator.Plotter.SheetNames.HOLDINGS, "Symbol");
                SetX(Algorithm.Asset(nickname).Ticker);
                Plot("Name", Algorithm.Asset(nickname).Description);
                Plot("Allocation", string.Format("{0:P2}", holdings[nickname]));
                Plot("Price", string.Format("{0:C2}", Algorithm.Asset(nickname).Close[0]));
            }

            SelectChart(Simulator.Plotter.SheetNames.LAST_REBALANCE, "Key");
            SetX("LastRebalance");
            Plot("Value", lastRebalanceDate);
        }

        /// <summary>
        /// Add historical asset allocations. This log contains one row
        /// for each rebalancing day. Each row summarizes the symbol held,
        /// along with the target allocation for that symbol.
        /// </summary>
        public void AddHistoricalAllocations()
        {
            if (Algorithm.Account.TradeLog == null || Algorithm.Account.TradeLog.Count == 0)
                return;

#if USE_TRADELOG_FLATTENED
            var tradelog = Algorithm.TradeLogFlattened;

            if (tradelog == null || tradelog.Count == 0)
                return;

            var targetHoldings = new Dictionary<string, double>();
            var targetDate = tradelog.First().OrderTicket.SubmitDate;

            for (int i = 0; i < tradelog.Count; i++)
            {
                var receipt = tradelog[i];

                // BUGBUG: We assume that from one log entry to the next,
                //         the asset allocation won't change - even if it
                //         is a different day. Of course, that isn't true,
                //         as prices and asset allocations fluctuate daily.
                //         However, that shouldn't matter too much, if we
                //         assume that typical algorithms adjust all assets
                //         held on the same trading day, effectively fixing
                //         this issue.
                if (receipt.OrderTicket.SubmitDate != targetDate || i == tradelog.Count - 1)
                {
                    SelectChart(Simulator.Plotter.SheetNames.HOLDINGS_HISTORY, "Date");
                    SetX(targetDate);

                    var row = "";
                    foreach (var kv in targetHoldings.OrderByDescending(kv => kv.Value))
                    {
                        if (kv.Value != 0.0)
                            row += string.Format("{0}{1}={2:P2}", row.Length > 0 ? ", " : "", Algorithm.Asset(kv.Key).Ticker, kv.Value);
                    }

                    Plot("Allocation", row);

                    targetDate = receipt.OrderTicket.SubmitDate;
                }

                targetHoldings[receipt.OrderTicket.Name] = receipt.OrderTicket.TargetAllocation;
            }
#else
            var allEodAllocations = new Dictionary<Algorithm, List<Tuple<DateTime, Dictionary<string, double>>>>();
            var allTradeDates = new HashSet<DateTime>();

            // collect EOD asset allocations
            // - one row for each day in the trade log
            // - assets referenced by their nickname
            void collectEodAllocation(Algorithm algo)
            {
                var eodAllocation = new List<Tuple<DateTime, Dictionary<string, double>>>();

                if (algo.Account.TradeLog != null)
                {
                    foreach (var trade in algo.Account.TradeLog)
                    {
                        // new date: copy previous allocation
                        // BUGBUG: this is inaccurate. Due to the fluctuation
                        //         of asset prices, the new line has deviated
                        //         from the previous allocation.
                        //         However, because a typical strategy adjusts
                        //         all its assets weights simultaneously, this
                        //         shouldn't matter too much.
                        if (eodAllocation.Count == 0)
                            eodAllocation.Add(Tuple.Create(
                                trade.OrderTicket.SubmitDate,
                                new Dictionary<string, double>()));
                        else if (eodAllocation.Last().Item1 != trade.OrderTicket.SubmitDate)
                            eodAllocation.Add(Tuple.Create(
                                trade.OrderTicket.SubmitDate,
                                new Dictionary<string, double>(eodAllocation.Last().Item2)));

                        // adjust the asset allocation according to the order
                        eodAllocation.Last().Item2[trade.OrderTicket.Name] = trade.OrderTicket.TargetAllocation;

#if RESOLVE_CHILD_HOLDINGS
                        // if an asset is referring to a child strategy,
                        // collect that child strategy's allocations
                        if (algo.Asset(trade.OrderTicket.Name).Meta.Generator != null
                        && !allEodAllocations.ContainsKey(algo.Asset(trade.OrderTicket.Name).Meta.Generator))
                            collectEodAllocation(algo.Asset(trade.OrderTicket.Name).Meta.Generator);
#endif

                        // record each day with a trade
                        if (!allTradeDates.Contains(trade.OrderTicket.SubmitDate))
                            allTradeDates.Add(trade.OrderTicket.SubmitDate);
                    }
                }
                allEodAllocations[algo] = eodAllocation;
            }
            collectEodAllocation(Algorithm);

            // get asset allocation for specific date
            // - assets referenced by their nickname
            // - all child strategies resolved
            Tuple<DateTime, Dictionary<string, double>> getAllocation(Algorithm algo, DateTime date)
            {
                // BUGBUG: this is inaccurate. Due to the fluctuation
                //         of asset prices, the new line has deviated
                //         from the previous allocation.
                //         In this case, this may make a notible difference,
                //         when the asset is a child strategy that is
                //         then replaced with its internal holdings.
                var eodAlloc = allEodAllocations[algo]
                    .Where(a => a.Item1 <= date)
                    .LastOrDefault();

                if (eodAlloc == null) return Tuple.Create(date, new Dictionary<string, double>());

                var resolvedAlloc = Tuple.Create(eodAlloc.Item1, new Dictionary<string, double>());

                foreach (var asset in eodAlloc.Item2)
                {
#if RESOLVE_CHILD_HOLDINGS
                    if (algo.Asset(asset.Key).Meta.Generator != null)
                    {
                        // asset is child strategy: resolve
                        var childAlloc = getAllocation(algo.Asset(asset.Key).Meta.Generator, date);

                        foreach (var child in childAlloc.Item2)
                        {
                            if (!resolvedAlloc.Item2.ContainsKey(child.Key))
                                resolvedAlloc.Item2[child.Key] = 0.0;
                            resolvedAlloc.Item2[child.Key] += asset.Value * child.Value;
                        }

                        // if the child strategy's last trade is more recent,
                        // use that date as the allocation's latest
                        if (resolvedAlloc.Item1 < childAlloc.Item1)
                            resolvedAlloc = Tuple.Create(childAlloc.Item1, resolvedAlloc.Item2);
                    }
                    else
#endif
                    {
                        // atomic asset, copy as-is
                        if (!resolvedAlloc.Item2.ContainsKey(asset.Key))
                            resolvedAlloc.Item2[asset.Key] = 0.0;
                        resolvedAlloc.Item2[asset.Key] += asset.Value;
                    }
                }

                return resolvedAlloc;
            }

            // get asset allocation for specific date
            // - assets referenced by their ticker symbol
            // - all child strategies resolved
            Dictionary<string, double> getTickerAllocation(DateTime date)
            {
                var alloc = getAllocation(Algorithm, date);

                if (alloc.Item1 != date) return null;

                var tickerAlloc = new Dictionary<string, double>();

                foreach (var kv in alloc.Item2)
                {
                    if (kv.Value == 0.0) continue; // ignore flat positions

                    var asset = Algorithm.Asset(kv.Key);

                    if (!tickerAlloc.ContainsKey(asset.Ticker))
                        tickerAlloc[asset.Ticker] = 0.0;
                    tickerAlloc[asset.Ticker] += kv.Value;
                }

                return tickerAlloc;
            }

            SelectChart(Simulator.Plotter.SheetNames.HOLDINGS_HISTORY, "Date");
            foreach (var date in allTradeDates.OrderBy(d => d))
            {
                var alloc = getTickerAllocation(date);

                if (alloc != null)
                {
                    SetX(date);

                    string row = "";
                    foreach (var kv in alloc.OrderByDescending(kv => kv.Value))
                        row += string.Format("{0}{1}={2:P2}", row.Length > 0 ? ", " : "", kv.Key, kv.Value);

                    Plot("Allocation", row);
                }
            }
#endif
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
