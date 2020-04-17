//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        SteadyOptionos_AnchorTrade
// Description: SteadyOptions' Anchor Trade
// History:     2020iv11, FUB, created
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
//------------------------------------------------------------------------------
//    Developed by Chris Welsh,
//    published and discussed on https://steadyoptions.com/
//
// rules: total leverage about +50%
// (1) buy deep in the money call with expiry more than one year out
//     - delta around .90
//     - this could also be a buy & hold ETF position
// (2) buy at-the-money put, expiry close to one year out
//     - 5% out of the money
//     - position size to fully hedge (1), and also cover (3)
//     - should not be more expensive than $13 - $17
// (3) sell at-the-money put, expiry monthly
//     - position size about half of (2)
//     - 24 DTE
//     - two strikes in the money
//     - roll after earning 50% of time value
//     - .55 delta
//     - expected income ~$0.80/week
// (4) invest remaining cash in BIL
//
// possible improvements:
// (a) position (2) around 5% OTM
// (b) roll up (2) around 7.5%, latest by 10%
// (c) don't use (2) to hedge (3), but create separate ATM hedge
// (d) roll up (c) around 5% gain
//
// example position as of 04/11/2020 (SPY ~278)
// this position was probably set up in mid January, when 
// SPY was at $325 with possible adjustments on the way to
// SPY reaching $338 around February 20th.
//
//     Jan 15, 2021, 225 C    7      $25.47     $17,829.00  - main position
//     Jan 15, 2021, 321 P    7     $101.94     $71,358.00  - main hedge
//     Apr 15, 2020, 220 P    -4     $12.64     ($5,056.00) - income position
//     Jan 15, 2021, 327 P    4     $107.64     $43,056.00  - income hedge
//     BIL                    147    $91.47     $13,446.09
//     Cash                                      $1,051.30
//     Total                                   $141,684.39
//
// example as of April 5th 2020
// SPY closed 04/03 @ 248.19 and opened 04/06 @ 257.84
//     1.       Buy to open 8 contracts June 18, 2021 170 Calls for $94.85 
//              (about 66% leverage, 7 contracts would give 45% leverage, 
//              9 contracts 87% leverage) – Total cost: $75,880
//     2.       Sell to open 8 contracts March 19, 2021 Puts for $25.81 
//              (Use the June 2021 calls to ensure long term capital gains 
//              while using the March 2021 puts as its closest to 365 days and 
//              if the markets move up quickly and the hedge has to be 
//              adjusted, it is cheaper and investors have the ability to roll 
//              up and out as well) – Total cost: $20,648;
//     3.       Buy to open 4 contracts of the March 19, 2021 260 puts for 
//              $31.04 (to hedge the short puts) – Total cost: $12,416;
//     4.       Sell to open 4 contracts of the April 22, 2020 262 puts 
//              for $14.36 – Total credit: $5,744;
//     5.       Buy 230 shares of BIL for $91.62 – Total cost: $21,072.60; and
//     6.       Hold $727.40 in cash.
//
// see SteadyOptions
// https://steadyoptions.com/forum/topic/1215-welcome-to-anchor-trades/
// https://steadyoptions.com/articles/leveraged-anchor-is-boosting-performance-r364/
// https://steadyoptions.com/articles/revisiting-anchor-thanks-to-orats-wheel-r396/
// https://steadyoptions.com/articles/revisiting-anchor-part-2-r411/
// https://steadyoptions.com/articles/leveraged-anchor-update-r426/
// https://steadyoptions.com/articles/leveraged-anchor-implementation-r442/
// https://steadyoptions.com/articles/leveraged-anchor-a-three-month-review-r462/
// https://steadyoptions.com/articles/anchor-analysis-and-options-r564/
// 
//==============================================================================

//#define FAKE_DATA
// if defined: run on synthetic fake data, instead of actual quotes

//#define SPX_OPTIONS
// if defined: use SPX, else XSP

#define MAIN_POS
// if defined: implement main position

#define MAIN_POS_CALL
// if defined: implement main position as ITM call

#define MAIN_HEDGE
// if defined: implement main hedge

//#define MAIN_HEDGE_FREE
// if defined: make cash deposits to pay for hedge

#define INCOME_POS
// if defined: implement income position

#define INCOME_HEDGE
// if defined: implement income hedge

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class SteadyOptions_AnchorTrade : Algorithm
    {
        public override string Name => "SteadyOptions' Anchor Trade";

        #region inputs
#if FAKE_DATA
        private readonly string UNDERLYING_NICK = "$SPX";
        private readonly string OPTION_NICK = "$SPX.fake.options";
        private readonly string BENCHMARK = "$SPXTR";
#elif SPX_OPTIONS
        private string UNDERLYING_NICK = "$SPX";
        private string OPTION_NICK = "$SPX.weekly.options";
        private readonly string BENCHMARK = "$SPXTR";
#else
        private string UNDERLYING_NICK = "$XSP";
        private string OPTION_NICK = "$XSP.options";
        private readonly string BENCHMARK = "$SPXTR";
#endif
        private readonly string VIX_1Y = "$VIX1Y";
        //private readonly string PARKING_NICK = "BIL";
        private OrderType ORDER_TYPE = OrderType.closeThisBar;
        #endregion
        #region internal data
        private Plotter _plotter = new Plotter();
        private Instrument _underlying = null;
        private Instrument _vix1y = null;
        #endregion

        #region MaintainMainPosition
        public int MAIN_W = 170;
#if MAIN_POS_CALL
        public int MAIN_DELTA = 90;
        public int MAIN_DTE = 365 + 31;
#endif
        private Instrument _mainPosition = null;
        private int _mainPositionTargetLots { get; set; } = 0;
        private int _mainPositionCurrentLots
        {
            get
            {
                if (_mainPosition == null
                || _mainPosition.Position == 0
                || (_mainPosition.IsOption && NextSimTime.Date > _mainPosition.OptionExpiry))
                    return 0;

                return _mainPositionTargetLots;
            }
        }
        private void MaintainMainPosition()
        {
            if (_mainPositionCurrentLots == 0)
            {
                // target shares: calculated from target exposure,
                // (not from price paid for call)
                _mainPositionTargetLots = (int)(Math.Round(NetAssetValue[0] * MAIN_W / 100.0 
                    / _underlying.Close[0] / 100.0));

                if (_mainPositionTargetLots < 1)
                {
                    Output.WriteLine("{0:MM/dd/yyyy}, main position: target lots == 0", SimTime[0]);
                    return;
                }

#if MAIN_POS
#if MAIN_POS_CALL
                // target date: 1 year out
                var targetExpiry = SimTime[0].Date + TimeSpan.FromDays(MAIN_DTE);

                // find all weekly expiries
                var weeklyExpiries = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday
                        || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday)
                    .Select(o => o.OptionExpiry.Date)
                    .Distinct()
                    .ToList();

                // select the closest to our target date
                var expiryDate = weeklyExpiries
                    .OrderBy(e => Math.Abs((e - targetExpiry).TotalDays))
                    .FirstOrDefault();

                // select contract with Delta closest to target
                _mainPosition = OptionChain(OPTION_NICK)
                    .Where(o => !o.OptionIsPut
                        && o.OptionExpiry == expiryDate)
                    .OrderBy(c => Math.Abs(c.BlackScholesImplied(0.0).Delta - MAIN_DELTA / 100.0))
                    .FirstOrDefault();

                if (_mainPosition == null)
                {
                    if (!IsOptimizing)
                        Output.WriteLine(string.Format("{0:MM/dd/yyyy}, main position: no suitable contract found", SimTime[0]));
                    return;
                }

                // buy long call to participate in the market
                _mainPosition.Trade(_mainPositionTargetLots, ORDER_TYPE)
                    .Comment = string.Format("main position, spot = {0:C2}", _underlying.Close[0]);
#else
                _mainPosition = _underlying;
                _mainPosition.Trade(100 * _mainPositionTargetLots, ORDER_TYPE)
                    .Comment = string.Format("main position, spot = {0:C2}", _underlying.Close[0]);
#endif
#endif
            }
        }
        #endregion
        #region MaintainMainHedge
        [OptimizerParam(90, 96, 2)]
        public int MAIN_HEDGE_STRIKE = 92; //95;
        [OptimizerParam(100, 125, 5)]
        public int MAIN_HEDGE_ITM = 120;
        [OptimizerParam(84, 94, 2)]
        public int MAIN_HEDGE_ROLL = 86; //90;
        private Instrument _mainHedge = null;
        private int _mainHedgeTargetLots { get; set; } = 0;
        private int _mainHedgeCurrentLots
        {
            get
            {
                if (_mainHedge == null
                || _mainHedge.Position == 0
                || NextSimTime.Date > _mainHedge.OptionExpiry)
                    return 0;

                return _mainHedgeTargetLots;
            }
        }
        private void MaintainMainHedge()
        {
            //----- close existing hedge position
            if (_mainHedgeCurrentLots > 0)
            {
                var minStrike = _underlying.Close[0] * MAIN_HEDGE_ROLL / 100.0;

                if ((_mainHedge.OptionStrike < minStrike)
                //|| (_mainHedge.OptionStrike < tryStrike && vixCur < vixLow * 1.01) // attempt to roll when puts are cheap
                )
                {
#if MAIN_HEDGE
                    _mainHedge.Trade(-_mainHedgeCurrentLots, ORDER_TYPE)
                        .Comment = string.Format("roll main hedge, min strike = {0:C2}", minStrike);

#if MAIN_HEDGE_FREE
                    Withdraw(100.0 * _mainHedgeCurrentLots * _mainHedge.Bid[0]);
#endif
#endif
                    _mainHedge = null;
                }
            }

            //----- open new hedge position
            if (_mainHedgeCurrentLots == 0 && _mainPositionTargetLots > 0)
            {
                // size to fully hedge main position
                _mainHedgeTargetLots = _mainPositionTargetLots;

                if (_mainHedgeTargetLots < 1)
                {
                    Output.WriteLine("{0:MM/dd/yyyy}, main hedge: target lots == 0", SimTime[0]);
                    return;
                }

#if MAIN_HEDGE
                // target date: 1 year out
                var targetExpiry = SimTime[0].Date + TimeSpan.FromDays(365);

                // find all weekly expiries
                var weeklyExpiries = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday
                        || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday)
                    .Select(o => o.OptionExpiry.Date)
                    .Distinct()
                    .ToList();

                // select the closest to our target date
                var expiryDate = weeklyExpiries
                    .OrderBy(e => Math.Abs((e - targetExpiry).TotalDays))
                    .FirstOrDefault();

                // target strike: 5% OTM
                // unless previous hedge expired ITM
                var targetStrike = _mainHedge == null /*|| _mainHedge.OptionStrike < _underlying.Close[0]*/
                    ? _underlying.Close[0] * MAIN_HEDGE_STRIKE / 100.0
                    : Math.Min(_mainHedge.OptionStrike, _underlying.Close[0] * MAIN_HEDGE_ITM / 100.0);

                // select the closes strike
                _mainHedge = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionIsPut
                        && o.OptionExpiry == expiryDate)
                    .OrderBy(p => Math.Abs(p.OptionStrike - targetStrike))
                    .FirstOrDefault();

                if (_mainHedge == null)
                {
                    if (!IsOptimizing)
                        Output.WriteLine(string.Format("{0:MM/dd/yyyy}, main hedge: no suitable contract found", SimTime[0]));
                    return;
                }

                // buy puts to fully hedge the main position
                _mainHedge.Trade(_mainHedgeTargetLots, ORDER_TYPE)
                    .Comment = string.Format("main hedge, spot = {0:C2}", _underlying.Close[0]);

#if MAIN_HEDGE_FREE
                Deposit(100.0 * _mainHedgeTargetLots * _mainHedge.Ask[0]);
#endif
#endif
            }
        }
        #endregion
        #region MaintainIncomePosition
        [OptimizerParam(100, 200, 25)]
        public int INCOME_POS_STRIKE = 150; // 500? about two strikes ITM
        [OptimizerParam(14, 35, 7)]
        public int INCOME_POS_DTE = 14; // 28? about 1 month out
        [OptimizerParam(50, 80, 5)]
        public int INCOME_POS_PT = 65; // profit target ~70%
        private Instrument _incomePosition = null;
        private double _incomePositionInitialTV = 0.0;
        private double _incomePositionPremiumRecv = 0.0;
        private int _incomePositionTargetLots { get; set; } = 0;
        private int _incomePositionCurrentLots
        {
            get
            {
                if (_incomePosition == null
                || _incomePosition.Position == 0
                || NextSimTime.Date > _incomePosition.OptionExpiry)
                    return 0;
                
                return _incomePositionTargetLots;
            }
        }

        private void MaintainIncomePosition()
        {
            //----- close income position
            if (_incomePositionCurrentLots > 0)
            {
                var timeValue = _incomePosition.Ask[0]
                    - Math.Max(0.0, _incomePosition.OptionStrike - _underlying.Close[0]);

                //if (timeValue < INCOME_PT / 100.0 * _incomePositionInitialTV)
                if (_incomePosition.Ask[0] < INCOME_POS_PT / 100.0 * _incomePositionPremiumRecv)
                {
                    // buy back
#if INCOME_POS
                    _incomePosition.Trade(_incomePositionCurrentLots, ORDER_TYPE)
                        .Comment = "roll income position";
#endif
                    _incomePosition = null;
                }
            }

            //----- open new income position
            if (_incomePositionCurrentLots == 0 && _mainPositionTargetLots > 0)
            {
                // position size is half of main position
                _incomePositionTargetLots = (int)Math.Floor(_mainPositionTargetLots / 2.0 + 0.5);

                if (_incomePositionTargetLots < 1)
                {
                    Output.WriteLine("{0:MM/dd/yyyy}, income position: target lots == 0", SimTime[0]);
                    return;
                }

#if INCOME_POS
                // target date: 1 month out
                var targetExpiry = SimTime[0].Date + TimeSpan.FromDays(INCOME_POS_DTE);

                // find all weekly expiries
                var weeklyExpiries = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday
                        || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday)
                    .Select(o => o.OptionExpiry.Date)
                    .Distinct()
                    .ToList();

                // select the closest to our target date
                var expiryDate = weeklyExpiries
                    .OrderBy(e => Math.Abs((e - targetExpiry).TotalDays))
                    .FirstOrDefault();

                // target strike: ATM
                var targetStrike = FindInstrument(UNDERLYING_NICK).Close[0] 
                    * (1.0 + INCOME_POS_STRIKE / 1e4);

                // select the closest strike
                _incomePosition = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionIsPut
                        && o.OptionExpiry == expiryDate)
                    .OrderBy(p => Math.Abs(p.OptionStrike - targetStrike))
                    .FirstOrDefault();

                if (_incomePosition == null)
                {
                    if (!IsOptimizing)
                        Output.WriteLine(string.Format("{0:MM/dd/yyyy}, income position: no suitable contract found", SimTime[0]));
                    return;
                }

                _incomePositionPremiumRecv = _incomePosition.Bid[0];
                _incomePositionInitialTV = _incomePosition.Bid[0]
                    -Math.Max(0.0, _incomePosition.OptionStrike - _underlying.Close[0]);

                // sell put for income
                _incomePosition.Trade(-_incomePositionTargetLots, ORDER_TYPE)
                    .Comment = "income position";
#endif
            }
        }
        #endregion
        #region MaintainIncomeHedge
        [OptimizerParam(94, 100, 2)]
        public int INCOME_HEDGE_STRIKE = 100; // 100?
        public int INCOME_HEDGE_ITM => MAIN_HEDGE_ITM;
        [OptimizerParam(84, 94, 2)]
        public int INCOME_HEDGE_ROLL = 84; // 95?
        private Instrument _incomeHedge = null;
        private int _incomeHedgeTargetLots { get; set; } = 0;
        private int _incomeHedgeCurrentLots
        {
            get
            {
                if (_incomeHedge == null
                || _incomeHedge.Position == 0
                || NextSimTime.Date > _incomeHedge.OptionExpiry)
                    return 0;

                return _incomeHedgeTargetLots;
            }
        }
        private void MaintainIncomeHedge()
        {
            //----- close existing hedge position
            if (_incomeHedgeCurrentLots > 0)
            {
                var minStrike = FindInstrument(UNDERLYING_NICK).Close[0] * INCOME_HEDGE_ROLL / 100.0;

                if (_incomeHedge.OptionStrike < minStrike)
                {
#if INCOME_HEDGE
                    _incomeHedge.Trade(-_incomeHedgeCurrentLots, ORDER_TYPE)
                        .Comment = string.Format("roll income hedge, minStrike = {0:C2}", minStrike);
#endif
                    _incomeHedge = null;
                }
            }

            //----- open new hedge position
            if (_incomeHedgeCurrentLots == 0 && _mainPositionTargetLots > 0)
            {
                // position size is same as the income position
                _incomeHedgeTargetLots = _incomePositionTargetLots;

                if (_incomeHedgeTargetLots < 1)
                {
                    Output.WriteLine("{0:MM/dd/yyyy}, income hedge: target lots == 0", SimTime[0]);
                    return;
                }

#if INCOME_HEDGE
                // target date: 1 year out
                var targetExpiry = SimTime[0].Date + TimeSpan.FromDays(365);

                // find all weekly expiries
                var weeklyExpiries = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday
                        || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday)
                    .Select(o => o.OptionExpiry.Date)
                    .Distinct()
                    .ToList();

                // select the closest to our target date
                var expiryDate = weeklyExpiries
                    .OrderBy(e => Math.Abs((e - targetExpiry).TotalDays))
                    .FirstOrDefault();

                // target strike: 5% OTM
                var targetStrike = _incomeHedge == null /*|| _incomeHedge.OptionStrike < _underlying.Close[0]*/
                    ? _underlying.Close[0] * INCOME_HEDGE_STRIKE / 100.0
                    : Math.Min(_incomeHedge.OptionStrike, _underlying.Close[0] * INCOME_HEDGE_ITM / 100.0);

                // select the closes strike
                _incomeHedge = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionIsPut
                        && o.OptionExpiry == expiryDate)
                    .OrderBy(p => Math.Abs(p.OptionStrike - targetStrike))
                    .FirstOrDefault();

                if (_incomeHedge == null)
                {
                    if (!IsOptimizing)
                        Output.WriteLine(string.Format("{0:MM/dd/yyyy}, income hedge: no suitable contract found", SimTime[0]));
                    return;
                }

                // buy put to hedge the income position
                _incomeHedge.Trade(_incomeHedgeTargetLots, ORDER_TYPE)
                    .Comment = string.Format("income hedge, spot = {0:C2}", _underlying.Close[0]);
#endif
            }
        }
        #endregion

        #region CheckParametersValid
        public override bool CheckParametersValid()
        {
            if (MAIN_HEDGE_STRIKE <= MAIN_HEDGE_ROLL
            || INCOME_HEDGE_STRIKE <= INCOME_HEDGE_ROLL)
                return false;

            return true;
        }
        #endregion
        #region FillModel
        protected override double FillModel(Order orderTicket, Bar barOfExecution, double theoreticalPrice)
        {
            if (orderTicket.Instrument != null && orderTicket.Instrument.IsOption)
                return 0.5 * (barOfExecution.Bid + barOfExecution.Ask);

            return theoreticalPrice;
        }
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //---------- initialization

            //WarmupStartTime = DateTime.Parse("06/01/2011", CultureInfo.InvariantCulture);
#if FAKE_DATA
            // data range for fake data
            StartTime = DateTime.Parse("06/01/2011", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("04/09/2020", CultureInfo.InvariantCulture);
#elif SPX_OPTIONS
            // SPX date range
            StartTime = DateTime.Parse("02/01/2007", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("11/28/2018", CultureInfo.InvariantCulture);
#else
            // XSP date range
            //StartTime = DateTime.Parse("08/01/2006", CultureInfo.InvariantCulture);
            StartTime = DateTime.Parse("11/19/2007", CultureInfo.InvariantCulture); // availability of monthlies
            EndTime = DateTime.Parse("08/01/2018", CultureInfo.InvariantCulture);
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            var spy = AddDataSource(UNDERLYING_NICK);
            AddDataSource(OPTION_NICK);
            var vix = AddDataSource(VIX_1Y);
            var bench = AddDataSource(BENCHMARK);

            //---------- simulation

            foreach (var simTime in SimTimes)
            {
                if (!HasInstrument(spy) || !HasInstrument(OPTION_NICK) || !HasInstrument(vix) || !HasInstrument(bench))
                    continue;

                _underlying = _underlying ?? spy.Instrument;
                _vix1y = _vix1y ?? vix.Instrument;

                MaintainMainPosition();
                MaintainMainHedge();
                MaintainIncomePosition();
                MaintainIncomeHedge();

                if (TradingDays > 0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);

                    _plotter.SelectChart("Hedge Strikes", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Underlying", spy.Instrument.Close[0]);
                    _plotter.Plot("Main Hedge", _mainHedge != null ? _mainHedge.OptionStrike : 0.0);
                    _plotter.Plot("Income Hedge", _incomeHedge != null ? _incomeHedge.OptionStrike : 0.0);

                    _plotter.SelectChart("Position Breakdown", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot("Main Position", _mainPosition != null
                        ? (_mainPosition.IsOption 
                            ? 100.0 * _mainPosition.Position * _mainPosition.Bid[0] 
                            : _mainPosition.Position * _mainPosition.Close[0]) 
                        : 0.0);
                    _plotter.Plot("Main Hedge", _mainHedge != null ? 100.0 * _mainHedgeCurrentLots * _mainHedge.Bid[0] : 0.0);
                    _plotter.Plot("Income Position", _incomePosition != null ? 100.0 * _incomePositionCurrentLots * _incomePosition.Ask[0] : 0.0);
                    _plotter.Plot("Income Hedge", _incomeHedge != null ? 100.0 * _incomeHedgeCurrentLots * _incomeHedge.Bid[0] : 0.0);
                    _plotter.Plot("Cash", Cash);
                }
            }

            //---------- post-processing

            _plotter.AddOrderLog(this);
            _plotter.AddParameters(this);

            FitnessValue = this.CalcFitness();
        }
        #endregion
        #region override public void Report()
        override public void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
}

//==============================================================================
// end of file
