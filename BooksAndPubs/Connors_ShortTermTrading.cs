//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Connors_ShortTermTrading
// Description: Strategy, as published in Larry Connors and Cesar Alvarez book
//              'Short Term Trading Strategies That Work'.
// History:     2019iii28, FUB, created
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

//#define INCLUDE_TRIN_STRATEGY

#region libraries
using System;
using System.Globalization;
using System.Linq;
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region common algorithm core
    public abstract class Connors_ShortTermTrading_Core : Algorithm
    {
        #region internal data
        protected virtual string MARKET => "SPY";
        protected virtual string VOLATILITY => "$VIX";

        protected virtual OrderType ORDER_TYPE => OrderType.closeThisBar;

#if INCLUDE_TRIN_STRATEGY
        private virtual string TRIN => "#SPXTRIN";
#endif

        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Connors_ShortTermTrading_Core()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        protected abstract int Rules(Instrument market);

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            var market = AddDataSource(MARKET);
            var volatility = AddDataSource(VOLATILITY);
#if INCLUDE_TRIN_STRATEGY
            AddDataSource(TRIN);
#endif

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                if (!HasInstrument(market) || !HasInstrument(volatility))
                    continue;

                if (!_alloc.Allocation.ContainsKey(market.Instrument))
                    _alloc.Allocation[market.Instrument] = 0.0;

                int buySell = Rules(market.Instrument);

                _alloc.LastUpdate = SimTime[0];

                //----- enter positions

                if (market.Instrument.Position == 0 && buySell != 0)
                {
                    int numShares = buySell * (int)Math.Floor(NetAssetValue[0] / market.Instrument.Close[0]);
                    _alloc.Allocation[market.Instrument] += buySell;
                    market.Instrument.Trade(numShares, OrderType.closeThisBar);
                }

                //----- exit positions

                else if (market.Instrument.Position != 0 && buySell != 0)
                {
                    _alloc.Allocation[market.Instrument] = 0.0;
                    market.Instrument.Trade(-market.Instrument.Position, ORDER_TYPE);
                }

                //----- output

                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, market.Instrument);
                    _plotter.AddStrategyHoldings(this, market.Instrument);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing && TradingDays > 0)
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

    // Chapter 9: The 2-period RSI - The Trader's Holy Grail of Indicators?
    #region The 2-period RSI under 5 on the S&P 500
    public class Connors_ShortTermTrading_RsiUnder5 : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' 2-Period RSI Under 5";

        [OptimizerParam(0, 20, 1)]
        public virtual int ENTRY_MAX_RSI { get; set; } = 5;

        protected override int Rules(Instrument market)
        {
            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketSma5 = market.Close.SMA(5);
            var marketRsi2 = market.Close.RSI(2);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0]
                && marketRsi2[0] < ENTRY_MAX_RSI)
                {
                    return 1;
                }
            }

            //----- exit positions

            else
            {
                if (market.Close[0] > marketSma5[0])
                {
                    return -1;
                }
            }

            return 0;
        }
    }
    #endregion
    #region Cumulative RSIs Strategy
    public class Connors_ShortTermTrading_CumulativeRsi : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' Cumulative RSIs";

        [OptimizerParam(1, 5, 1)]
        public virtual int CUM_RSI_DAYS { get; set; } = 2;

        [OptimizerParam(20, 80, 5)]
        public virtual int ENTRY_MAX_CUM_RSI { get; set; } = 35;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_RSI { get; set; } = 65;

        protected override int Rules(Instrument market)
        {
            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketRsi2 = market.Close.RSI(2);
            var marketCumRsi = marketRsi2.Sum(CUM_RSI_DAYS);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0]
                && marketCumRsi[0] < ENTRY_MAX_CUM_RSI)
                {
                    return 1;
                }
            }

            //----- exit positions

            else
            {
                if (marketRsi2[0] > EXIT_MIN_RSI)
                {
                    return -1;
                }
            }

            return 0;
        }
    }
    #endregion

    #region Chapter 10: Double 7's Strategy
    public class Connors_ShortTermTrading_Double7 : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' Double 7's";

        [OptimizerParam(5, 10, 1)]
        public virtual int DOUBLE_DAYS { get; set; } = 7;

        protected override int Rules(Instrument market)
        {
            var marketSma200 = market.Close.SMA(200);
            var marketHi7 = market.TypicalPrice().Highest(DOUBLE_DAYS);
            var marketLo7 = market.TypicalPrice().Lowest(DOUBLE_DAYS);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0]
                && marketLo7[0] < marketLo7[1])
                {
                    return 1;
                }
            }

            //----- exit positions

            else
            {
                if (marketHi7[0] > marketHi7[1])
                {
                    return -1;
                }
            }

            return 0;
        }
    }
    #endregion

    // Chapter 12: 5 Strategies to Time the Market
    #region 1. VIX Stretches Strategy
    public class Connors_ShortTermTrading_VixStretches : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' VIX Stretches";

        [OptimizerParam(2, 5, 1)]
        public virtual int LE1_MIN_VIX_DAYS { get; set; } = 3;

        [OptimizerParam(1, 10, 1)]
        public virtual int LE1_MIN_VIX_PCNT { get; set; } = 5;

        [OptimizerParam(50, 90, 5)]
        public virtual int LX_MIN_MKT_RSI { get; set; } = 65;

        protected override int Rules(Instrument market)
        {
            Instrument volatility = FindInstrument(VOLATILITY);

            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketRsi2 = market.Close.RSI(2);
            var volSma10 = volatility.Close.SMA(10);

            var volStretch = Enumerable.Range(0, LE1_MIN_VIX_DAYS)
                .Aggregate(true, (prev, idx) => prev
                    && volatility.Close[idx] > volSma10[idx] * (1.0 + LE1_MIN_VIX_PCNT / 100.0));

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0] && volStretch)
                    return 1;
            }

            //----- exit positions

            else
            {
                if (marketRsi2[0] > LX_MIN_MKT_RSI)
                    return -1;
            }

            return 0;
        }
    }
    #endregion
    #region 2. VIX RSI Strategy
    public class Connors_ShortTermTrading_VixRsi : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' VIX RSI";

        [OptimizerParam(75, 100, 5)]
        public virtual int LE2_MIN_VIX_RSI { get; set; } = 90;

        [OptimizerParam(0, 50, 5)]
        public virtual int LE2_MAX_MKT_RSI { get; set; } = 30;

        [OptimizerParam(50, 90, 5)]
        public virtual int LX_MIN_MKT_RSI { get; set; } = 65;

        protected override int Rules(Instrument market)
        {
            Instrument volatility = FindInstrument(VOLATILITY);

            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketRsi2 = market.Close.RSI(2);
            var volRsi2 = volatility.Close.RSI(2);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0]
                && marketRsi2[0] < LE2_MAX_MKT_RSI
                && volRsi2[0] > LE2_MIN_VIX_RSI
                && volatility.Open[0] > volatility.Close[1])
                {
                    return 1;
                }
            }

            //----- exit positions

            else
            {
                if (marketRsi2[0] > LX_MIN_MKT_RSI)
                    return -1;
            }

            return 0;
        }
    }
    #endregion
    #region 3. The TRIN
#if INCLUDE_TRIN_STRATEGY
    public class Connors_ShortTermTrading_Trin : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' TRIN";

    [OptimizerParam(45, 75, 5)]
        public virtual int LE3_MAX_MKT_RSI { get; set; } = 50;

        [OptimizerParam(1, 5, 1)]
        public virtual int LE3_MIN_TRIN_UP { get; set; } = 3;

        [OptimizerParam(50, 90, 5)]
        public virtual int LX_MIN_MKT_RSI { get; set; } = 65;

        protected override int Rules()
        {
            //----- calculate indicators

            var marketSma200 = _market.Close.SMA(200);
            var marketRsi2 = _market.Close.RSI(2);

            var trinDaysUp = Enumerable.Range(0, LE3_MIN_TRIN_UP)
                .Aggregate(true, (prev, idx) => prev && _trin.Close[idx] > 1.0);

            //----- enter positions

            if (_market.Position == 0)
            {
                if (_market.Close[0] > marketSma200[0]
                && marketRsi2[0] < LE3_MAX_MKT_RSI
                && trinDaysUp)
                {
                    return 1;
                }
            }

            //----- exit positions

            else
            {
                if (marketRsi2[0] > LX_MIN_MKT_RSI)
                    return -1;
            }

            return 0;
        }
    }
#endif
    #endregion
    #region 4. One More Market Timing Strategy with Cumulative RSIs
    public class Connors_ShortTermTrading_MoreCumulativeRsi : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' More Cumulative RSI";

        [OptimizerParam(1, 5, 1)]
        public virtual int LE4_RSI_CUM_DAYS { get; set; } = 2;

        [OptimizerParam(20, 80, 5)]
        public virtual int LE4_MAX_CUM_RSI { get; set; } = 45;

        [OptimizerParam(50, 90, 5)]
        public virtual int LX_MIN_MKT_RSI { get; set; } = 65;

        protected override int Rules(Instrument market)
        {
            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketRsi3 = market.Close.RSI(3);
            var marketCumRsi = marketRsi3.Sum(LE4_RSI_CUM_DAYS);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] > marketSma200[0] && marketCumRsi[0] < LE4_MAX_CUM_RSI)
                    return 1;
            }

            //----- exit positions

            else
            {
                if (marketRsi3[0] > LX_MIN_MKT_RSI)
                    return -1;
            }

            return 0;
        }
    }
    #endregion
    #region 5. Trading on the Short Side - The S&P Short Strategy
    public class Connors_ShortTermTrading_ShortSide : Connors_ShortTermTrading_Core
    {
        public override string Name => "Connors' Short";

        [OptimizerParam(2, 7, 1)]
        public virtual int LE5_MIN_MKT_UP { get; set; } = 4;

        protected override int Rules(Instrument market)
        {
            //----- calculate indicators

            var marketSma200 = market.Close.SMA(200);
            var marketSma5 = market.Close.SMA(5);

            var marketUpDays = Enumerable.Range(0, LE5_MIN_MKT_UP)
                .Aggregate(true, (prev, idx) => prev
                    && market.Close[idx] > market.Close[idx + 1]);

            //----- enter positions

            if (market.Position == 0)
            {
                if (market.Close[0] < marketSma200[0]
                && marketUpDays)
                {
                    return -1;
                }
            }

            //----- exit positions

            else
            {
                if (market.Close[0] < marketSma5[0]
                || market.Close[0] > marketSma200[0]) // this line is not in the book
                {
                    return 1;
                }
            }

            return 0;
        }
    }
    #endregion
}

//==============================================================================
// end of file