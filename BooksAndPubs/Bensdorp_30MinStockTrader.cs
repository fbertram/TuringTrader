//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Bensdorp_30MinStockTrader
// Description: Strategy, as published in Laurens Bensdorp's book
//              'The 30-Minute Stock Trader'.
// History:     2019iii19, FUB, created
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

// USE_BENSDORPS_RANGE
// defined: match simulation range to Bensdorp's book
// undefined: simulate from 2007 to last week
//#define USE_BENSDORPS_RANGE

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    //---------- core strategies
    #region Weekly Rotation
    public class Bensdorp_30MinStockTrader_WR : SubclassableAlgorithm
    {
        public override string Name => "Bensdorp's Weekly Rotation";

        #region inputs
        protected Universe UNIVERSE = Globals.LARGE_CAP_UNIVERSE;

        [OptimizerParam(0, 100, 5)]
        public virtual int MAX_RSI { get; set; } = 50;

        [OptimizerParam(1, 10, 1)]
        public virtual int MAX_ENTRIES { get; set; } = 10;
        #endregion
        #region internal data
        private static readonly string BENCHMARK = Globals.STOCK_MARKET;

        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        private Instrument _benchmark;
        #endregion
        #region ctor
        public Bensdorp_30MinStockTrader_WR()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

#if USE_BENSDORPS_RANGE
            // matching range in the book
            StartTime = DateTime.Parse("01/02/1995", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);
            EndTime = DateTime.Parse("11/23/2016", CultureInfo.InvariantCulture);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            AddDataSources(UNIVERSE.Constituents);
            AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                //----- find instruments

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);
                var universe = Instruments
                    .Where(i => i.IsConstituent(UNIVERSE))
                    .ToList();

                //----- calculate indicators

                // calculate indicators for all known instruments,
                // as they might enter the universe any time
                var indicators = Instruments
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            rsi = i.Close.RSI(3),
                            roc = i.Close.Momentum(200),
                        });

                var smaBand = _benchmark.Close.SMA(200).Multiply(0.98); // 2% below 200-day SMA

                // filter universe to potential candidates
                var filtered = universe
                    .Where(i => _benchmark.Close[0] > smaBand[0]
                        && indicators[i].rsi[0] < MAX_RSI)
                    .ToList();

                if (NextSimTime.DayOfWeek < SimTime[0].DayOfWeek) // open positions on Monday
                {
                    // sort by momentum
                    var ranked = universe
                        .Where(i => _benchmark.Close[0] > smaBand[0])
                        .OrderByDescending(i => indicators[i].roc[0])
                        .ToList();

                    // enter: top-ranked momentum and low RSI
                    var entry = ranked
                        .Where(i => indicators[i].rsi[0] < MAX_RSI)
                        .Take(MAX_ENTRIES)
                        .ToList();

                    // hold: top-ranked momentum
                    var hold = ranked
                        .Take(MAX_ENTRIES)
                        .ToList();

                    // keep those we have identified as 'hold'
                    var nextHoldings = Instruments
                        .Where(i => i.Position != 0 
                            && hold.Contains(i))
                        .ToList();

                    // fill up, until we reach MAX_ENTRIES
                    nextHoldings = nextHoldings
                        //.Concat(entry.Take(MAX_ENTRIES - nextHoldings.Count))
                        .Concat(entry.Where(i => !nextHoldings.Contains(i)).Take(MAX_ENTRIES - nextHoldings.Count))
                        .ToList();

                    _alloc.LastUpdate = SimTime[0];
                    _alloc.Allocation.Clear();
                    foreach (var i in Instruments)
                    {
                        double targetPercentage = nextHoldings.Contains(i)
                            ? 1.0 / MAX_ENTRIES
                            : 0.0;
                        int targetShares = (int)Math.Floor(NetAssetValue[0] * targetPercentage / i.Close[0]);

                        if (targetPercentage != 0.0)
                            _alloc.Allocation[i] = targetPercentage;

                        i.Trade(targetShares - i.Position);
                    }
                }

                //----- output

                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    //_plotter.AddStrategyHoldings(this, universe);

                    // plot strategy exposure
                    _plotter.SelectChart("Exposure Chart", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Exposure", Instruments.Sum(i => i.Position * i.Close[0]) / NetAssetValue[0]);

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
    #endregion
    #region Mean Reversion
    public abstract class Bensdorp_30MinStockTrader_MRx : Algorithm
    {
        public override string Name => ENTRY_DIR > 0 ? "MRL Strategy" : "MRS Strategy";

        #region inputs
        protected Universe UNIVERSE = Globals.LARGE_CAP_UNIVERSE;

        public abstract int ENTRY_DIR { get; set; }

        public abstract int SMA_DAYS { get; set; }

        [OptimizerParam(0, 100, 5)]
        public abstract int MIN_ADX { get; set; }

        [OptimizerParam(200, 500, 50)]
        public abstract int MIN_ATR { get; set; }

        [OptimizerParam(0, 100, 5)]
        public abstract int MINMAX_RSI { get; set; }

        [OptimizerParam(200, 500, 50)]
        public abstract int STOP_LOSS { get; set; }

        [OptimizerParam(200, 500, 50)]
        public abstract int PROFIT_TARGET { get; set; }

        public abstract int MAX_CAP { get; set; }

        public abstract int MAX_RISK { get; set; }

        [OptimizerParam(1, 10, 1)]
        public abstract int MAX_ENTRIES { get; set; }

        public abstract int MAX_HOLD_DAYS { get; set; }
        #endregion
        #region internal data
        private static readonly string BENCHMARK = "$SPX";
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter;
        private Instrument _benchmark;
        #endregion
        #region ctor
        public Bensdorp_30MinStockTrader_MRx()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

#if USE_BENSDORPS_RANGE
            // matching range in the book
            StartTime = DateTime.Parse("01/02/1995", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);
            EndTime = DateTime.Parse("11/23/2016", CultureInfo.InvariantCulture);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            AddDataSources(UNIVERSE.Constituents);
            AddDataSource(BENCHMARK);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            var entryParameters = Enumerable.Empty<Instrument>()
                .ToDictionary(
                    i => i,
                    i => new
                    {
                        entryDate = default(DateTime),
                        entryPrice = default(double),
                        stopLoss = default(double),
                        profitTarget = default(double),
                    });

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                //----- find instruments

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);
                var universe = Instruments
                    .Where(i => i.IsConstituent(UNIVERSE))
                    .ToList();

                //----- calculate indicators

                // make sure to calculate indicators for all
                // known instruments, as they may enter the universe
                // at any time
                var indicators = Instruments
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            sma150 = i.Close.SMA(SMA_DAYS),
                            adx7 = i.ADX(7),
                            atr10 = i.TrueRange().Divide(i.Close).SMA(10),
                            rsi3 = i.Close.RSI(3),
                        });

                // filter universe to potential candidates
                var filtered = universe
                    .Where(i =>
                        (ENTRY_DIR > 0
                            ? i.Close[0] > indicators[i].sma150[0]                // long: above sma
                            : i.Close[0] > i.Close[1] && i.Close[1] > i.Close[2]) // short: 2 up-days
                        && indicators[i].adx7[0] > MIN_ADX
                        && indicators[i].atr10[0] > MIN_ATR / 10000.0
                        && (ENTRY_DIR > 0
                            ? indicators[i].rsi3[0] < MINMAX_RSI   // long: maximum
                            : indicators[i].rsi3[0] > MINMAX_RSI)) // short: minimum
                    .ToList();

                //----- manage existing positions

                int numOpenPositions = Positions.Keys.Count();
                foreach (var pos in Positions.Keys)
                {
                    // time-based exit
                    if (entryParameters[pos].entryDate <= SimTime[MAX_HOLD_DAYS - 1])
                    {
                        pos.Trade(-pos.Position, OrderType.closeThisBar).Comment = "time exit";
                        numOpenPositions--;
                    }
                    else if (ENTRY_DIR > 0
                        ? pos.Close[0] >= entryParameters[pos].profitTarget  // long
                        : pos.Close[0] <= entryParameters[pos].profitTarget) // short
                    {
                        pos.Trade(-pos.Position,
                                OrderType.openNextBar)
                            .Comment = "profit target";
                        numOpenPositions--;
                    }
                    else
                    {
                        pos.Trade(-pos.Position,
                                OrderType.stopNextBar,
                                entryParameters[pos].stopLoss)
                            .Comment = "stop loss";
                    }
                }

                //----- open new positions

                if (NextSimTime.DayOfWeek < SimTime[0].DayOfWeek) // open positions on Monday
                {
                    // sort candidates by RSI to find entries
                    var entries = ENTRY_DIR > 0
                        ? filtered // long
                            .Where(i => i.Position == 0)
                            .OrderBy(i => indicators[i].rsi3[0])
                            .Take(MAX_ENTRIES - numOpenPositions)
                            .ToList()
                        : filtered // short
                            .Where(i => i.Position == 0)
                            .OrderByDescending(i => indicators[i].rsi3[0])
                            .Take(MAX_ENTRIES - numOpenPositions)
                            .ToList();

                    foreach (var i in entries)
                    {
                        // save our entry parameters, so that we may access
                        // them later to manage exits
                        double entryPrice = ENTRY_DIR > 0
                                ? i.Close[0] * (1.0 - MIN_ATR / 10000.0) // long
                                : i.Close[0];                            // short

                        double stopLoss = ENTRY_DIR > 0
                                ? entryPrice * (1.0 - STOP_LOSS / 100.0 * indicators[i].atr10[0])  // long
                                : entryPrice * (1.0 + STOP_LOSS / 100.0 * indicators[i].atr10[0]); // short

                        double profitTarget = ENTRY_DIR > 0
                            ? entryPrice * (1.0 + PROFIT_TARGET / 10000.0)  // long
                            : entryPrice * (1.0 - PROFIT_TARGET / 10000.0); // short

                        entryParameters[i] = new
                        {
                            entryDate = NextSimTime,
                            entryPrice,
                            stopLoss,
                            profitTarget,
                        };

                        // calculate target shares in two ways:
                        // * fixed-fractional risk (with entry - stop-loss = "risk"), and
                        // * fixed percentage of total equity
                        double riskPerShare = ENTRY_DIR > 0
                            ? Math.Max(0.10, entryPrice - stopLoss)  // long
                            : Math.Max(0.10, stopLoss - entryPrice); // short

                        int sharesRiskLimited = (int)Math.Floor(MAX_RISK / 100.0 / MAX_ENTRIES * NetAssetValue[0] / riskPerShare);
                        int sharesCapLimited = (int)Math.Floor(MAX_CAP / 100.0 / MAX_ENTRIES * NetAssetValue[0] / entryParameters[i].entryPrice);
                        int targetShares = (ENTRY_DIR > 0 ? 1 : -1) * Math.Min(sharesRiskLimited, sharesCapLimited);

                        // enter positions with limit order
                        i.Trade(targetShares,
                            OrderType.limitNextBar,
                            entryParameters[i].entryPrice);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(Name, NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Name, _benchmark.Close[0]);

                    // placeholder, to make sure positions land on sheet 2
                    _plotter.SelectChart("Strategy Positions", "entry date");

                    // additional indicators
                    _plotter.SelectChart("Strategy Leverage", "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("leverage", Instruments.Sum(i => i.Position * i.Close[0]) / NetAssetValue[0]);
                }
            }

            //========== post processing ==========

            //----- print position log, grouped as LIFO

            if (!IsOptimizing)
            {
                var tradeLog = LogAnalysis
                    .GroupPositions(Log, true)
                    .OrderBy(i => i.Entry.BarOfExecution.Time);

                _plotter.SelectChart("Strategy Positions", "entry date");
                foreach (var trade in tradeLog)
                {
                    _plotter.SetX(trade.Entry.BarOfExecution.Time);
                    _plotter.Plot("exit date", trade.Exit.BarOfExecution.Time);
                    _plotter.Plot("Symbol", trade.Symbol);
                    _plotter.Plot("Quantity", trade.Quantity);
                    _plotter.Plot("% Profit", (ENTRY_DIR > 0 ? 1.0 : -1.0) * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0));
                    _plotter.Plot("Exit", trade.Exit.OrderTicket.Comment ?? "");
                    //_plotter.Plot("$ Profit", trade.Quantity * (trade.Exit.FillPrice - trade.Entry.FillPrice));
                }
            }

            //----- optimization objective

            double cagr = Math.Exp(252.0 / Math.Max(1, TradingDays) * Math.Log(NetAssetValue[0] / INITIAL_CAPITAL)) - 1.0;
            FitnessValue = cagr / Math.Max(1e-10, Math.Max(0.01, NetAssetValueMaxDrawdown));

            if (!IsOptimizing)
                Output.WriteLine("CAGR = {0:P2}, DD = {1:P2}, Fitness = {2:F4}", cagr, NetAssetValueMaxDrawdown, FitnessValue);
        }
#       endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    public abstract class Bensdorp_30MinStockTrader_MRL : Bensdorp_30MinStockTrader_MRx
    {
        public override int ENTRY_DIR { get; set; } = 1;   // 1 = long
        public override int SMA_DAYS { get; set; } = 150; // 150 days
        public override int MIN_ADX { get; set; } = 45;
        public override int MIN_ATR { get; set; } = 400; // 4%
        public override int MINMAX_RSI { get; set; } = 30;  // long: maximum
        public override int STOP_LOSS { get; set; } = 250; // 2.5 x ATR
        public override int PROFIT_TARGET { get; set; } = 300; // 3%
        public override int MAX_CAP { get; set; } = 100; // 100%
        public override int MAX_RISK { get; set; } = 20;  // 20%
        public override int MAX_ENTRIES { get; set; } = 10;  // 10
        public override int MAX_HOLD_DAYS { get; set; } = 4;   // 4 days
    }

    public abstract class Bensdorp_30MinStockTrader_MRS : Bensdorp_30MinStockTrader_MRx
    {
        public override int ENTRY_DIR { get; set; } = -1;  // -1 = short
        public override int SMA_DAYS { get; set; } = 150; // 150 days
        public override int MIN_ADX { get; set; } = 50;
        public override int MIN_ATR { get; set; } = 500; // 5%
        public override int MINMAX_RSI { get; set; } = 85;  // short: minimum
        public override int STOP_LOSS { get; set; } = 250; // 2.5 x ATR
        public override int PROFIT_TARGET { get; set; } = 400; // 4%
        public override int MAX_CAP { get; set; } = 100; // 100%
        public override int MAX_RISK { get; set; } = 20;  // 20%
        public override int MAX_ENTRIES { get; set; } = 10;  // 10
        public override int MAX_HOLD_DAYS { get; set; } = 2;   // 2 days
    }
    #endregion
}

//==============================================================================
// end of file