//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Clenow_StocksOnTheMove
// Description: Strategy, as published in Andreas F. Clenow's book
//              'Stocks on the Move'.
//              http://www.followingthetrend.com/
// History:     2018xii14, FUB, created
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

// USE_CLENOWS_RANGE
// defined: match simulation range to Clenow's book
// undefined: simulate from 2007 to last week
//#define USE_CLENOWS_RANGE

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
    public class Clenow_StocksOnTheMove : AlgorithmPlusGlue
    {
        public override string Name => "Clenow's Stocks on the Move";

        #region inputs
        /// <summary>
        /// length of momentum calculation (in days)
        /// </summary>
        [OptimizerParam(63, 252, 21)]
        public virtual int MOM_PERIOD { get; set; } = 90;

        /// <summary>
        /// maximum daily move (in percent)
        /// </summary>
        [OptimizerParam(10, 25, 5)]
        public virtual int MAX_MOVE { get; set; } = 15;

        /// <summary>
        /// length of SMA for instrument trend filter (in days)
        /// </summary>
        [OptimizerParam(63, 252, 21)]
        public virtual int INSTR_TREND { get; set; } = 100;

        /// <summary>
        /// length of ATR calculation (in days)
        /// </summary>
        [OptimizerParam(5, 25, 5)]
        public virtual int ATR_PERIOD { get; set; } = 20;

        /// <summary>
        /// length of SMA for index trend filter (in days)
        /// </summary>
        [OptimizerParam(63, 252, 21)]
        public virtual int INDEX_TREND { get; set; } = 200;

        /// <summary>
        /// length of SMA for index trend filter (in days)
        /// </summary>
        [OptimizerParam(5, 20, 5)]
        public virtual int INDEX_FLT { get; set; } = 10;

        /// <summary>
        /// percentage of instruments from the top (in %)
        /// </summary>
        [OptimizerParam(5, 50, 5)]
        public virtual int TOP_PCNT { get; set; } = 20;

        /// <summary>
        /// target risk per stock (in basis points)
        /// </summary>
        [OptimizerParam(5, 50, 5)]
        public virtual int RISK_PER_STOCK { get; set; } = 10;

        /// <summary>
        /// target risk for portfolio (in basis points)
        /// </summary>
        public virtual int RISK_TOTAL { get; set; } = 10000;

        /// <summary>
        /// maximum weight per stock (in percent)
        /// </summary>
        public virtual int MAX_PER_STOCK { get; set; } = 100;

        /// <summary>
        /// traded stock universe
        /// </summary>
        protected virtual Universe UNIVERSE { get; set; }  = Universes.STOCKS_US_LG_CAP;

        /// <summary>
        /// day of weekly rebalancing
        /// </summary>
        protected virtual bool IsTradingDay 
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;

        /// <summary>
        /// supplemental money-management code
        /// </summary>
        /// <param name="w"></param>
        protected virtual void ManageWeights(Dictionary<Instrument, double> w) { }
        #endregion
        #region private data
        private readonly string BENCHMARK = Assets.STOCKS_US_LG_CAP;
        private readonly string SP500 = "$SPX";
        #endregion

        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

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

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Clenow is not considering commissions

            var all = AddDataSources(UNIVERSE.Constituents);
            var sp500 = AddDataSource(SP500);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            double? sp500Initial = null;

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                if (!HasInstrument(benchmark))
                    continue;

                sp500Initial = sp500Initial ?? sp500.Instrument.Open[0];

                // calculate indicators exactly once per bar
                // we are doing this on all available instruments,
                // as we don't know when they will become S&P500 constituents
                var indicators = Instruments
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            regression = i.Close.LogRegression(MOM_PERIOD),
                            maxMove = i.Close.SimpleMomentum(1).AbsValue().Highest(MOM_PERIOD),
                            avg100 = i.Close.SMA(INSTR_TREND),
                            atr20 = i.AverageTrueRange(ATR_PERIOD),
                        }); ;

                // index filter: only buy any shares, while S&P-500 is trading above its 200-day moving average
                // NOTE: the 10-day SMA on the benchmark is _not_ mentioned in
                //       the book. We added it here, to compensate for the
                //       simplified re-balancing schedule.
                bool allowNewEntries = sp500.Instrument.Close.SMA(INDEX_FLT)[0] 
                    > sp500.Instrument.Close.SMA(INDEX_TREND)[0];

                // determine current S&P 500 constituents
                var constituents = Instruments
                    .Where(i => i.IsConstituent(UNIVERSE))
                    .ToList();

                // trade once per week
                // this is a slight simplification from Clenow's suggestion to adjust positions
                // every week, and adjust position sizes only every other week
                if (IsTradingDay)
                {
                    // rank by volatility-adjusted momentum and pick top 20% (top-100)
                    var topRankedInstruments = constituents
                        // FIXME: how exactly are we multiplying the regression slope with R2?
                        .OrderByDescending(i => (Math.Exp(252.0 * indicators[i].regression.Slope[0]) - 1.0) * indicators[i].regression.R2[0])
                        //.OrderByDescending(i => indicators[i].regression.Slope[0] * indicators[i].regression.R2[0])
                        .Take((int)Math.Round(TOP_PCNT / 100.0 * constituents.Count))
                        .ToList();

                    // disqualify
                    //    - trading below 100-day moving average
                    //    - maximum move > 15%
                    // FIXME: is maxMove > 1.0???
                    var availableInstruments = topRankedInstruments
                        .Where(i => i.Close[0] > indicators[i].avg100[0]
                            && indicators[i].maxMove[0] < MAX_MOVE / 100.0)
                        .ToList();

                    // allocate capital until we run out of cash
                    var weights = Instruments
                        .ToDictionary(
                            i => i,
                            i => 0.0);

                    double availableCapital = 1.0;
                    int portfolioRisk = 0;
                    foreach (var i in availableInstruments)
                    {
                        // Clenow does not limit the total portfolio risk
                        if (portfolioRisk > RISK_TOTAL)
                            continue;

                        var currentWeight = NetAssetValue[0] > 0
                            ? i.Position * i.Close[0] / NetAssetValue[0]
                            : 0.0;
                        var newWeight = Math.Min(Math.Min(availableCapital, MAX_PER_STOCK / 100.0),
                            RISK_PER_STOCK * 0.0001 / indicators[i].atr20[0] * i.Close[0]);

                        var w = allowNewEntries
                            ? newWeight
                            : Math.Min(currentWeight, newWeight);

                        weights[i] = w;
                        availableCapital -= w;
                        portfolioRisk += RISK_PER_STOCK;
                    }

                    // perform customized money-management
                    ManageWeights(weights);

                    // submit trades
                    Alloc.LastUpdate = SimTime[0];
                    Alloc.Allocation.Clear();
                    foreach (var i in Instruments)
                    {
                        if (weights[i] > 0.005)
                            Alloc.Allocation[i] = weights[i];

                        var targetShares = (int)Math.Round(NetAssetValue[0] * weights[i] / i.Close[0]);
                        i.Trade(targetShares - i.Position, OrderType.openNextBar);
                    }

                    if (!IsOptimizing && (EndTime - SimTime[0]).TotalDays < 30)
                    {
                        string message = constituents
                            .Where(i => weights[i] != 0.0)
                            .Aggregate(string.Format("{0:MM/dd/yyyy}: ", SimTime[0]),
                                (prev, i) => prev + string.Format("{0}={1:P2} ", i.Symbol, weights[i]));

                        Output.WriteLine(message);
                    }
                }
                else // if (IsTradingDay)
                {
                    Alloc.AdjustForPriceChanges(this);
                }

                // create charts
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    _plotter.AddStrategyHoldings(this, constituents);

                    // plot strategy exposure
                    _plotter.SelectChart("Strategy Exposure", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Stock Exposure", constituents.Sum(i => i.Position * i.Close[0]) / NetAssetValue[0]);
                    _plotter.Plot("Number of Stocks", constituents.Where(i => i.Position != 0).Count());
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

#if true
                    _plotter.SelectChart("Clenow-style Chart", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(Name, NetAssetValue[0] / Globals.INITIAL_CAPITAL);
                    _plotter.Plot(sp500.Instrument.Name, sp500.Instrument.Close[0] / sp500Initial);
                    _plotter.Plot(sp500.Instrument.Name + " 200-day moving average", sp500.Instrument.Close.SMA(200)[0] / sp500Initial);
                    _plotter.Plot("Cash", Cash / NetAssetValue[0]);
#endif
                }

                if (IsDataSource)
                {
                    var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                    yield return Bar.NewOHLC(
                        this.GetType().Name, SimTime[0],
                        v, v, v, v, 0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddAverageHoldings(this);
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
    }
}

//==============================================================================
// end of file