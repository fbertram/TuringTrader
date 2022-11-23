//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Clenow_StocksOnTheMove
// Description: Strategy, as published in Andreas F. Clenow's book
//              'Stocks on the Move'.
//              http://www.followingthetrend.com/
// History:     2018xii14, FUB, created
//              2022x31, FUB, ported to v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
// if defined, set simulation range to match charts in Clenow's book
//#define USE_CLENOWS_RANGE

// OPTIONAL_CHARTS
// if defined, render optional chart showing NAV, 200-day SMA, and exposure
//#define OPTIONAL_CHARTS

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    public class Clenow_StocksOnTheMove : Algorithm
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
        //[OptimizerParam(100, 1000, 100)]
        public virtual int RISK_TOTAL { get; set; } = 10000;

        /// <summary>
        /// maximum weight per stock (in percent)
        /// </summary>
        //[OptimizerParam(10, 100, 10)]
        public virtual int MAX_PER_STOCK { get; set; } = 100;

        /// <summary>
        /// traded stock universe
        /// </summary>
        protected virtual string UNIVERSE { get; set; } = "$SPX";

        /// <summary>
        /// day of weekly rebalancing
        /// </summary>
        protected virtual bool IS_TRADING_DAY
            => IsFirstBar || (SimDate.DayOfWeek <= DayOfWeek.Wednesday && NextSimDate.DayOfWeek > DayOfWeek.Wednesday);

        /// <summary>
        /// supplemental money-management code
        /// </summary>
        /// <param name="w"></param>
        protected virtual void MANAGE_WEIGHTS(Dictionary<string, double> w)
        {
#if false
            // NOTE: Clenow does not mention the use of a safe instrument.
            //       This code is just an example of what we could do here:
            var idleCapital = 1.0 - w.Sum(kv => kv.Value);
            w["BIL"] = Math.Min(1.0, Math.Max(0.0, idleCapital));
#endif
        }

        /// <summary>
        /// allow new entries: this covers both new positions, and increasing of existing positions.
        /// </summary>
        /// <returns>true, if new entries are allowed</returns>
        protected virtual bool ALLOW_NEW_ENTRIES
        {
            get => Asset("$SPX").Close.SMA(INDEX_FLT)[0] > Asset("$SPX").Close.SMA(INDEX_TREND)[0];
        }
        #endregion
        #region strategy logic
        public override void Run()
        {
            //========== initialization ==========

#if USE_CLENOWS_RANGE
            // matching Clenow's charts
            StartDate = StartDate ?? DateTime.Parse("1999-01-01T16:00-05:00"); // 4pm in New York
            EndDate = EndDate ?? DateTime.Parse("2014-12-31T16:00-05:00");
#else
            StartDate = StartDate ?? DateTime.Parse("2007-01-01T16:00-05:00"); // 4pm in New York
            EndDate = EndDate ?? DateTime.Now;
#endif
            WarmupPeriod = TimeSpan.FromDays(365);

            //========== simulation loop ==========

            // loop through all bars
            SimLoop(() =>
            {
                if (IS_TRADING_DAY)
                {
                    // we start with the current S&P 500 universe
                    var constituents = Universe(UNIVERSE);

                    //----- rank assets
                    //    - by volatility-adjusted momentum
                    var rankedStocks = constituents
                        .OrderByDescending(name =>
                        {
                            // NOTE: the book is not clear on the type of regression
                            //       and how to multiply the slope with R2. Other 
                            //       possibilities exist:
                            //       - var regr = Asset(name).Close.LinRegression(MOM_PERIOD);
                            //       - return regr.Slope[0] * regr.R2[0];
                            var regr = Asset(name).Close.LogRegression(MOM_PERIOD);
                            return (Math.Exp(252.0 * regr.Slope[0]) - 1.0) * regr.R2[0];
                        })
                        .ToList();

                    //----- disqualify assets
                    //    - not ranked within top 20% (top 100)
                    //    - trading below 100-day moving average
                    //    - maximum move > 15%
                    var investibleStocks = rankedStocks
                        .Take((int)Math.Round(TOP_PCNT / 100.0 * constituents.Count))
                        .Where(name => Asset(name).Close[0] > Asset(name).Close.SMA(100)[0])
                        // NOTE: it is not fully clear how to determine the maximum move.
                        //       Other plausible options include:
                        //     - Asset(name).TrueRange().Div(Asset(name).Close).Highest(MOM_PERIOD)[0] < MAX_MOVE / 100.0
                        .Where(name => Asset(name).Close.RelReturn().AbsValue().Highest(MOM_PERIOD)[0] < MAX_MOVE / 100.0)
                        .ToList();

                    //----- money management
                    // initially, assume to close all open positions
                    var weights = Positions
                        .ToDictionary(
                            kv => kv.Key,
                            kv => 0.0);

                    // allocate capital to the ranked assets until we run out of cash
                    var availableCapital = 1.0;
                    var portfolioRisk = 0;
                    foreach (var name in investibleStocks)
                    {
                        // NOTE: Clenow does not limit the total portfolio risk
                        //       we add this condition here to support proprietary
                        //       improvements upon Clenow's original strategy
                        if (portfolioRisk > RISK_TOTAL)
                            continue;

                        // we size the positions as a fixed-fraction risk allocation,
                        // based on the average true range
                        // NOTE: Clenow does not limit the maximum position size of 
                        //       an asset, as long as there is capital available.
                        //       We add a limit here to support proprietary improvements
                        //       upon Clenow's original strategy.
                        var rawWeight = Math.Min(
                            Math.Min(availableCapital, MAX_PER_STOCK / 100.0),
                            RISK_PER_STOCK * 0.0001 * Asset(name).Close[0] / Asset(name).AverageTrueRange(20)[0]);

                        // only buy any shares, while S&P-500 is trading above its 200-day moving average
                        // NOTE: the 10-day SMA on the benchmark is NOT mentioned in
                        //       Clenow's book. We added it here, to compensate for the
                        //       simplified re-balancing schedule.
                        var w = ALLOW_NEW_ENTRIES ? rawWeight : Math.Min(Asset(name).Position, rawWeight);

                        weights[name] = w;
                        availableCapital -= w;
                        portfolioRisk += RISK_PER_STOCK;
                    }

                    // NOTE: Clenow does not perform any additional management.
                    //       We add this hook here to support proprietary
                    //       improvements upon Clenow's original strategy.
                    MANAGE_WEIGHTS(weights);

                    //----- order management
                    //    - place orders after the close
                    //    - execution at next day's open
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, OrderType.openNextBar);
                }

                // create charts
                if (!IsOptimizing)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot(Asset("$SPXTR").Description, Asset("$SPXTR").Close[0]);

#if OPTIONAL_CHARTS
                    // this code matches the chart seen
                    // on page 118 of the book
                    Plotter.SelectChart("Clenow-style Chart", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue / 1000.00);
                    var spx = Asset("$SPXTR").Close;
                    Plotter.Plot(spx.Name, spx[0] / spx[(DateTime)StartDate]);
                    var ema = Asset("$SPXTR").Close.SMA(200);
                    Plotter.Plot(ema.Name, ema[0] / ema[(DateTime)StartDate]);
                    Plotter.Plot("Cash", Cash);
#endif
                }
            });

            //========== post processing ==========

            if (!IsOptimizing)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file