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
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    //---------- core strategies
    #region Weekly Rotation
    public class Bensdorp_30MinStockTrader_WR : SubclassableAlgorithm
    {
        public override string Name => "Bensdorp's Weekly Rotation";

        #region inputs
        protected Universe UNIVERSE = Universes.STOCKS_US_LG_CAP;
        //protected Universe UNIVERSE = Universes.STOCKS_US_TOP_100;

        [OptimizerParam(0, 100, 5)]
        public virtual int MAX_RSI { get; set; } = 50;

        [OptimizerParam(1, 10, 1)]
        public virtual int MAX_ENTRIES { get; set; } = 10;
        #endregion
        #region internal data
        private static readonly string BENCHMARK = Assets.STOCKS_US_LG_CAP;
        private static readonly string SPX = Assets.STOCKS_US_LG_CAP;

        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
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
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/02/1995", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("11/23/2016", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);
#else
            StartTime = SubclassedStartTime ?? Globals.START_TIME;
            EndTime = SubclassedEndTime ?? Globals.END_TIME;
            WarmupStartTime = Globals.WARMUP_START_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            AddDataSources(UNIVERSE.Constituents);
            var spx = AddDataSource(SPX);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            double nn = 0.0;
            foreach (var s in SimTimes)
            {
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

                if (!HasInstrument(benchmark))
                    continue;

                var smaBand = spx.Instrument.Close.SMA(200).Multiply(0.98); // 2% below 200-day SMA

                // open positions on Monday
                if (NextSimTime.DayOfWeek < SimTime[0].DayOfWeek) // open positions on Monday
                {
                    // we are not entirely sure how Bensdorp wants this strategy to work.
                    // we see three alternatives
#if false
                    // solution A
                    // create one list of 10 stocks, none of which are overbought,
                    // ranked by momentum
                    // good: new entries are guaranteed to be on keep-list on day 1
                    //       also, we always hold 10 stocks
                    // bad: we might exit stocks with top momentum, as soon as they become overbought
                    // => this strategy seems to never hold stocks longer than 60 days,
                    //    conflicting with the statements made in the book
                    var nextHoldings = universe
                        .Where(i => spx.Instrument.Close[0] > smaBand[0])
                        .Where(i => indicators[i].rsi[0] < MAX_RSI)
                        .OrderByDescending(i => indicators[i].roc[0])
                        .Take(MAX_ENTRIES)
                        .ToList();
#endif
#if false
                    // solution B
                    // create separate list for new entries and for keepers
                    // good: this makes sure that we almost always hold 10 stocks
                    // bad: a new stock entered might not meet the hold requirements,
                    //    as it might not have top-10 momentum. this adds somewhat of
                    //    a mean-reversion component to the strategy
                    // => this strategy seems to work very well over the book's backtesting period.
                    //    overall, higher return and higher drawdown than C, worse Sharpe ratio
                    var keep = universe
                        .Where(i => spx.Instrument.Close[0] > smaBand[0])
                        .OrderByDescending(i => indicators[i].roc[0])
                        .Take(MAX_ENTRIES)
                        .Where(i => i.Position != 0)
                        .ToList();
                    var enter = universe
                        .Where(i => spx.Instrument.Close[0] > smaBand[0])
                        .Where(i => i.Position == 0 && indicators[i].rsi[0] < MAX_RSI)
                        .OrderByDescending(i => indicators[i].roc[0])
                        .Take(MAX_ENTRIES - keep.Count)
                        .ToList();
                    var nextHoldings = keep
                        .Concat(enter)
                        .ToList();
#endif
#if true
                    // solution C
                    // draw new entries and keeps both from top-10 ranked stocks
                    // good: new entries are guaranteed to be on the keep list on day 1
                    // bad: the enter list might be empty, if all top-10 stocks are overbought
                    //   driving down our exposure and therewith return
                    var top10 = universe
                        .Where(i => spx.Instrument.Close[0] > smaBand[0])
                        .OrderByDescending(i => indicators[i].roc[0])
                        .Take(MAX_ENTRIES)
                        .ToList();
                    var keep = top10
                        .Where(i => i.Position != 0)
                        .ToList();
                    var enter = top10
                        .Where(i => i.Position == 0 && indicators[i].rsi[0] < MAX_RSI)
                        .ToList();
                    var nextHoldings = keep
                        .Concat(enter)
                        .ToList();
#endif

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

                    nn = nextHoldings.Count / 10.0;
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
                    //_plotter.Plot("Choices", nn);

                    if (IsSubclassed) AddSubclassedBar(10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL);
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
    public abstract class Bensdorp_30MinStockTrader_MRx : SubclassableAlgorithm
    {
        public override string Name => ENTRY_DIR > 0 ? "MRL Strategy" : "MRS Strategy";

        #region inputs
        //protected Universe UNIVERSE = Universes.STOCKS_US_TOP_100; // 100 stocks
        //protected Universe UNIVERSE = Universes.STOCKS_US_LG_CAP; // 500 stocks
        //protected Universe UNIVERSE = Universes.STOCKS_US_LG_MID_SMALL_CAP; // 1500 stocks
        protected Universe UNIVERSE = Universes.STOCKS_US_ALL; // 3000 stocks

        public abstract int ENTRY_DIR { get; }

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
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/02/1995", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("11/23/2016", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);
#else
            StartTime = SubclassedStartTime ?? Globals.START_TIME;
            EndTime = SubclassedEndTime ?? Globals.END_TIME;
            WarmupStartTime = Globals.WARMUP_START_TIME;
#endif

            AddDataSources(UNIVERSE.Constituents);
            AddDataSource(BENCHMARK);

            Deposit(Globals.INITIAL_CAPITAL);
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
#if false
                    // we don't like to have these filter rules here. Instead,
                    // this should be solved by proper universe selection
                    .Where(i =>
                        i.Close[0] > 1.00
                        && i.Volume.ToDouble().SMA(50)[0] > 0.5e6
                        && i.Close.Multiply(i.Volume.ToDouble()).SMA(50)[0] > 2.5e6)
#endif
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
                var filtered = ENTRY_DIR > 0
                    ? (universe
                        .Where(i =>                                       // - long -
                            i.Close[0] > indicators[i].sma150[0]          // close above 150-day SMA
                            && indicators[i].adx7[0] > MIN_ADX            // 7-day ADX above 45
                            && indicators[i].atr10[0] > MIN_ATR / 10000.0 // 10-day ATR above 4%
                            && indicators[i].rsi3[0] < MINMAX_RSI)        // 3-day RSI below 30
                        .ToList())
                    : (universe
                        .Where(i =>                                            // - short -
                            i.Close[0] > i.Close[1] && i.Close[1] > i.Close[2] // 2 up-days
                            && indicators[i].adx7[0] > MIN_ADX                 // 7-day ADX above 50
                            && indicators[i].atr10[0] > MIN_ATR / 10000.0      // 10-day ATR above 5%
                            && indicators[i].rsi3[0] > MINMAX_RSI)             // 3-day RSI above 85
                        .ToList());

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

                //----- output

                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    //_plotter.AddStrategyHoldings(this, universe);

                    // plot strategy exposure
                    _plotter.SelectChart("Exposure Chart", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Exposure", Instruments.Sum(i => i.Position * i.Close[0]) / NetAssetValue[0]);

                    if (IsSubclassed) AddSubclassedBar(10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                //_plotter.AddTargetAllocation(_alloc);
                //_plotter.AddOrderLog(this);
                //_plotter.AddPositionLog(this);
                //_plotter.AddPnLHoldTime(this);
                //_plotter.AddMfeMae(this);
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

    public class Bensdorp_30MinStockTrader_MRL : Bensdorp_30MinStockTrader_MRx
    {
        public override int ENTRY_DIR => 1;   // 1 = long
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

    public class Bensdorp_30MinStockTrader_MRS : Bensdorp_30MinStockTrader_MRx
    {
        public override int ENTRY_DIR => -1;  // -1 = short
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

    #region Combo: WR + MRS
    public class Bensdorp_30MinStockTrader_WR_MRS_Combo : LazyPortfolio
    {
        public override string Name => "Bensdorp's WR + MRS";

        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("algorithm:Bensdorp_30MinStockTrader_WR",  0.50),
            Tuple.Create("algorithm:Bensdorp_30MinStockTrader_MRS", 0.50),
        };
        public override string BENCH => Assets.STOCKS_US_LG_CAP;

        public override DateTime START_TIME => Globals.START_TIME;
        public override DateTime END_TIME => Globals.END_TIME;
    }
    #endregion
    #region Combo: MRL + MRS
    public class Bensdorp_30MinStockTrader_MRL_MRS_Combo : LazyPortfolio
    {
        public override string Name => "Bensdorp's MRL + MRS";

        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("algorithm:Bensdorp_30MinStockTrader_MRL", 0.50),
            Tuple.Create("algorithm:Bensdorp_30MinStockTrader_MRS", 0.50),
        };
        public override string BENCH => Assets.STOCKS_US_LG_CAP;

        public override DateTime START_TIME => Globals.START_TIME;
        public override DateTime END_TIME => Globals.END_TIME;
    }
    #endregion
}

//==============================================================================
// end of file