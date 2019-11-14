//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Clenow_StocksOnTheMove
// Description: Strategy, as published in Andreas F. Clenow's book
//              'Stocks on the Move'.
//              http://www.followingthetrend.com/
// History:     2018xii14, FUB, created
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

// USE_CLENOWS_RANGE
// defined: match simulation range to Clenow's book
// undefined: simulate from 2007 to last week
//#define USE_CLENOWS_RANGE

// REPRODUCE_CLENOWS_CHART
// defined: produce chart matching the book
// undefined: produce TuringTrader-style report
//#define REPRODUCE_CLENOWS_CHART

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
    public class Clenow_StocksOnTheMove : Algorithm
    {
        public override string Name => "Clenow's Stocks on the Move";

        #region inputs
        [OptimizerParam(63, 252, 21)]
        public int MOM_PERIOD = 90;

        [OptimizerParam(110, 125, 5)]
        public int MAX_MOVE = 115;

        [OptimizerParam(63, 252, 21)]
        public int INSTR_FLT = 100;

        [OptimizerParam(5, 25, 5)]
        public int ATR_PERIOD = 20;

        [OptimizerParam(63, 252, 21)]
        public int INDEX_FLT = 200;

        [OptimizerParam(5, 50, 5)]
        public int TOP_PCNT = 20;

        [OptimizerParam(5, 50, 5)]
        public int RISK_PER_STOCK = 10;
        #endregion
        #region private data
        private readonly string BENCHMARK = Globals.STOCK_MARKET;
        private Universe UNIVERSE = Globals.LARGE_CAP_UNIVERSE;
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Clenow_StocksOnTheMove()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            // set simulation time frame
#if USE_CLENOWS_RANGE
            // matching Clenow's charts
            StartTime = DateTime.Parse("01/01/1999", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(180);
            EndTime = DateTime.Parse("12/31/2014", CultureInfo.InvariantCulture);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            // set account value
            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Clenow is not considering commissions

            // add instruments
            AddDataSource(BENCHMARK);
            AddDataSources(UNIVERSE.Constituents);

            //========== simulation loop ==========

            Instrument benchmark = null;
            double? benchmark0 = null;

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                benchmark = benchmark ?? FindInstrument(BENCHMARK);
                if (benchmark0 == null && TradingDays == 1)
                    benchmark0 = benchmark.Open[0];

                // calculate indicators
                // store to list, to make sure indicators are evaluated
                // exactly once per bar
                // do this on all available instruments, to make sure we
                // have valid data available when instruments become
                // constituents of our universe
                var instrumentIndicators = Instruments
                    .Select(i => new
                    {
                        instrument = i,
                        regression = i.Close.LogRegression(MOM_PERIOD),
                        maxMove = i.TrueRange().Divide(i.Close).Highest(MOM_PERIOD),
                        avg100 = i.Close.SMA(INSTR_FLT),
                        atr20 = i.AverageTrueRange(ATR_PERIOD),
                    })
                    .ToList();

                // index filter: only buy any shares, while S&P-500 is trading above its 200-day moving average
                // NOTE: the 10-day SMA on the benchmark is _not_ mentioned in
                //       the book. We added it here, to compensate for the
                //       simplified re-balancing schedule.
                bool allowNewEntries = FindInstrument(BENCHMARK).Close.SMA(10)[0] 
                    > FindInstrument(BENCHMARK).Close.SMA(INDEX_FLT)[0];

                // trade once per week
                // this is a slight simplification from Clenow's suggestion to adjust positions
                // every week, and adjust position sizes only every other week
                // CAUTION: no indicator calculations within this block!
                if (SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday)
                {
                    // select our universe constituents
                    var universeConstituents = instrumentIndicators
                        .Where(e => e.instrument.IsConstituent(UNIVERSE))
                        .ToList();

                    // rank by volatility-adjusted momentum and pick top 20%
                    var topRankedInstruments = universeConstituents
                        .OrderByDescending(e => (Math.Exp(252.0 * e.regression.Slope[0]) - 1.0) * e.regression.R2[0])
                        .Take((int)Math.Round(TOP_PCNT / 100.0 * universeConstituents.Count))
                        .ToList();

                    // disqualify
                    //    - trading below 100-day moving average
                    //    - maximum move > 15%
                    var availableInstruments = topRankedInstruments
                        .Where(e => e.instrument.Close[0] > e.avg100[0]
                            && e.maxMove[0] < MAX_MOVE / 100.0)
                        .ToList();

                    // calculate position sizes
                    var positionSizes = availableInstruments
                        .Select(e => new
                        {
                            instrument = e.instrument,
                            positionSize = RISK_PER_STOCK * 0.0001 / e.atr20[0] * e.instrument.Close[0],
                        })
                        .ToList();

                    // assign equity, until we run out of cash
                    var instrumentRelativeEquity = Instruments.ToDictionary(i => i, i => 0.0);
                    double availableEquity = 1.0;
                    foreach (var i in positionSizes)
                    {
                        if (i.positionSize <= availableEquity)
                        {
                            instrumentRelativeEquity[i.instrument] = i.positionSize;
                            availableEquity -= i.positionSize;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // loop through all instruments and submit trades
                    _alloc.LastUpdate = SimTime[0];
                    _alloc.Allocation.Clear();
                    foreach (Instrument instrument in instrumentRelativeEquity.Keys)
                    {
                        if (instrumentRelativeEquity[instrument] > 0.005)
                            _alloc.Allocation[instrument] = instrumentRelativeEquity[instrument];

                        int currentShares = instrument.Position;

                        int targetSharesPreFilter = (int)Math.Round(NetAssetValue[0] * instrumentRelativeEquity[instrument] / instrument.Close[0]);
                        int targetShares = allowNewEntries
                            ? targetSharesPreFilter
                            : Math.Min(currentShares, targetSharesPreFilter);

                        instrument.Trade(targetShares - currentShares, OrderType.openNextBar);
                    }

                    string message = instrumentRelativeEquity
                        .Where(i => i.Value != 0.0)
                        .Aggregate(string.Format("{0:MM/dd/yyyy}: ", SimTime[0]),
                            (prev, next) => prev + string.Format("{0}={1:P2} ", next.Key.Symbol, next.Value));
                    if (!IsOptimizing && (EndTime - SimTime[0]).TotalDays < 30)
                        Output.WriteLine(message);
                }

                // create plots on Sheet 1
                if (TradingDays > 0)
                {
#if REPRODUCE_CLENOWS_CHART
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(Name, NetAssetValue[0] / INITIAL_FUNDS);
                    _plotter.Plot(benchmark.Name, benchmark.Close[0] / benchmark0);
                    _plotter.Plot(benchmark.Name + " 200-day moving average", benchmark.Close.SMA(200)[0] / benchmark0);
                    _plotter.Plot("Cash", Cash / NetAssetValue[0]);
#else
                    _plotter.AddNavAndBenchmark(this, benchmark);
                    //_plotter.AddStrategyHoldings(this, universeConstituents);

                    // plot strategy exposure
                    _plotter.SelectChart("Strategy Exposure", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Exposure", Instruments.Sum(i => i.Position * i.Close[0]) / NetAssetValue[0]);
                    _plotter.Plot("Number of Stocks", (double)Positions.Count);
#endif
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
#if REPRODUCE_CLENOWS_CHART
            _plotter.OpenWith("SimpleChart");
#else
            _plotter.OpenWith("SimpleReport");
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file