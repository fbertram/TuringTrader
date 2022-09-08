//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Connors_HighProbEtfTrading
// Description: Strategy, as published in Larry Connors and Cesar Alvarez book
//              'High Probability ETF Trading'.
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region common algorithm core
    public abstract class Connors_HighProbEtfTrading_Core : AlgorithmPlusGlue
    {
        #region settings
        protected virtual string MARKET => "SPY";

        [OptimizerParam(0, 1, 1)]
        public virtual int AGGRESSIVE_ON { get; set; } = 0;

        public virtual OrderType ORDER_TYPE => OrderType.closeThisBar;
        #endregion

        protected abstract double Rules(Instrument i);

        #region public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
            WarmupStartTime = StartTime - TimeSpan.FromDays(63);

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            var market = AddDataSource(MARKET);

            //========== simulation loop ==========

            var entryPrices = new Dictionary<Instrument, double>();

            foreach (var s in SimTimes)
            {
                if (!Alloc.Allocation.ContainsKey(market.Instrument))
                    Alloc.Allocation[market.Instrument] = 0.0;

                double percentToBuySell = Rules(market.Instrument);

                //----- entries

                if (market.Instrument.Position >= 0 && percentToBuySell > 0
                || market.Instrument.Position <= 0 && percentToBuySell < 0)
                {
                    int sharesToBuySell = (int)(Math.Sign(percentToBuySell) * Math.Floor(
                        Math.Abs(percentToBuySell) * NetAssetValue[0] / market.Instrument.Close[0]));

                    Alloc.Allocation[market.Instrument] += percentToBuySell;
                    market.Instrument.Trade(sharesToBuySell, ORDER_TYPE);
                }

                //----- exits

                if (market.Instrument.Position > 0 && percentToBuySell < 0
                || market.Instrument.Position < 0 && percentToBuySell > 0)
                {
                    // none of the algorithms attempt to gradually
                    // exit positions, so this is good enough
                    Alloc.Allocation[market.Instrument] = 0.0;
                    market.Instrument.Trade(-market.Instrument.Position, OrderType.closeThisBar);
                }

                //----- output

                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, market.Instrument);
                    _plotter.AddStrategyHoldings(this, market.Instrument);
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);
                }

                var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    string.Format("{0}-{1}", this.GetType().Name, market.Instrument.Symbol),
                    SimTime[0],
                    v, v, v, v, 0);
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
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
    #endregion

    //===== 7 strategies
    #region 3-Day High/Low
    public class Connors_HighProbEtfTrading_3DayHighLow : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' 3-Day High/Low";

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var sma5 = i.Close.SMA(5);
            var down3 = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev
                    && i.High[idx] < i.High[idx + 1]
                    && i.Low[idx] < i.Low[idx + 1]);
            var up3 = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev
                    && i.High[idx] > i.High[idx + 1]
                    && i.Low[idx] > i.Low[idx + 1]);

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && down3) // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && up3))  // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            || (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            || (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region RSI 25 & RSI 75
    public class Connors_HighProbEtfTrading_Rsi25Rsi75 : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' RSI 25 & RSI 75";

        [OptimizerParam(10, 30, 5)]
        public int ENTRY_MAX_RSI_LONG = 25;

        [OptimizerParam(10, 30, 5)]
        public int ENTRY2_MAX_RSI_LONG = 20;

        [OptimizerParam(50, 75, 5)]
        public int EXIT_MIN_RSI_LONG = 55;

        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            int ENTRY_MIN_RSI_SHORT = 100 - ENTRY_MAX_RSI_LONG;
            int ENTRY2_MIN_RSI_SHORT = 100 - ENTRY2_MAX_RSI_LONG;
            int EXIT_MAX_RSI_SHORT = 100 - EXIT_MIN_RSI_LONG;

            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var rsi4 = i.Close.RSI(4);

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && rsi4[0] < ENTRY_MAX_RSI_LONG)   // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && rsi4[0] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi4[0] > EXIT_MIN_RSI_LONG)
            || (i.Position < 0 && rsi4[0] < EXIT_MAX_RSI_SHORT))
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi4[0] < ENTRY2_MAX_RSI_LONG)    // long
            || (i.Position < 0 && rsi4[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region R3
    public class Connors_HighProbEtfTrading_R3 : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' R3";

        [OptimizerParam(50, 70, 5)]
        public int ENTRY_MAX_RSI_2_LONG = 60;

        [OptimizerParam(5, 30, 5)]
        public int ENTRY_MAX_RSI_0_LONG = 10;

        [OptimizerParam(55, 85, 5)]
        public int EXIT_MIN_RSI_LONG = 70;

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            int ENTRY_MIN_RSI_2_SHORT = 100 - ENTRY_MAX_RSI_2_LONG;
            int ENTRY_MIN_RSI_0_SHORT = 100 - ENTRY_MAX_RSI_0_LONG;
            int EXIT_MAX_RSI_SHORT = 100 - EXIT_MIN_RSI_LONG;

            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var rsi2 = i.Close.RSI(2);

            var rsiDown = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev && rsi2[idx] < rsi2[idx + 1])
                && rsi2[0] < ENTRY_MAX_RSI_0_LONG
                && rsi2[2] < ENTRY_MAX_RSI_2_LONG;

            var rsiUp = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev && rsi2[idx] > rsi2[idx + 1])
                && rsi2[0] > ENTRY_MIN_RSI_0_SHORT
                && rsi2[2] > ENTRY_MIN_RSI_2_SHORT;

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && rsiDown) // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && rsiUp))  // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            || (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            || (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region %b
    public class Connors_HighProbEtfTrading_PercentB : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' %b";

        [OptimizerParam(10, 30, 5)]
        public int ENTRY_MAX_BB_LONG = 20;

        [OptimizerParam(70, 90, 5)]
        public int EXIT_MIN_BB_LONG = 80;

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            int ENTRY_MIN_BB_SHORT = 100 - ENTRY_MAX_BB_LONG;
            int EXIT_MAX_BB_SHORT = 100 - EXIT_MIN_BB_LONG;

            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var percentB = i.Close.BollingerBands(5, 1.0).PercentB;
            var bLo = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev && percentB[idx] < ENTRY_MAX_BB_LONG / 100.0);
            var bHi = Enumerable.Range(0, 3)
                .Aggregate(true, (prev, idx) => prev && percentB[idx] > ENTRY_MIN_BB_SHORT / 100.0);

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && bLo)  // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && bHi)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && percentB[0] > EXIT_MIN_BB_LONG / 100.0)   // long
            || (i.Position < 0 && percentB[0] < EXIT_MAX_BB_SHORT / 100.0)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && percentB[0] < ENTRY_MAX_BB_LONG / 100.0)    // long
            || (i.Position < 0 && percentB[0] > ENTRY_MIN_BB_SHORT / 100.0))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region MDU and MDD
    public class Connors_HighProbEtfTrading_MduMdd : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' MDU and MDD";

        [OptimizerParam(3, 6, 1)]
        public int ENTRY_MIN_UP_DN = 4;

        [OptimizerParam(4, 7, 1)]
        public int UP_DN_WINDOW = 5;

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var sma5 = i.Close.SMA(5);
            var daysDn = Enumerable.Range(0, UP_DN_WINDOW)
                .Aggregate(0, (prev, idx) => prev + (i.Close[idx] < i.Close[idx + 1] ? 1 : 0));
            var daysUp = Enumerable.Range(0, UP_DN_WINDOW)
                .Aggregate(0, (prev, idx) => prev + (i.Close[idx] > i.Close[idx + 1] ? 1 : 0));

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && i.Close[0] < sma5[0] && daysDn >= ENTRY_MIN_UP_DN)  // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && i.Close[0] > sma5[0] && daysUp >= ENTRY_MIN_UP_DN)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            || (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            || (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region RSI 10/6 & RSI 90/94
    public class Connors_HighProbEtfTrading_Rsi1006Rsi9094 : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' RSI 10/6 & RSI 90/94";

        [OptimizerParam(5, 10, 1)]
        public int ENTRY_MAX_RSI_LONG = 10;

        [OptimizerParam(3, 7, 1)]
        public int ENTRY2_MAX_RSI_LONG = 6;

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        protected override double Rules(Instrument i)
        {
            int ENTRY_MIN_RSI_SHORT = 100 - ENTRY_MAX_RSI_LONG;
            int ENTRY2_MIN_RSI_SHORT = 100 - ENTRY2_MAX_RSI_LONG;

            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var sma5 = i.Close.SMA(5);
            var rsi2 = i.Close.RSI(2);

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && rsi2[0] < ENTRY_MAX_RSI_LONG)   // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && rsi2[0] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return (i.Close[0] > sma200[0] ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            || (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi2[0] < ENTRY2_MAX_RSI_LONG)    // long
            || (i.Position < 0 && rsi2[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return (i.Position > 0 ? 1.0 : -1.0) / (1.0 + AGGRESSIVE_ON);
            }

            return 0.0;
        }
    }
    #endregion
    #region TPS
    public class Connors_HighProbEtfTrading_Tps : Connors_HighProbEtfTrading_Core
    {
        public override string Name => "Connors' TPS";

        new public int AGGRESSIVE_ON => 1;

        [OptimizerParam(10, 40, 5)]
        public virtual int ENTRY_MAX_RSI_LONG { get; set; } = 25;

        [OptimizerParam(60, 80, 5)]
        public virtual int EXIT_MIN_RSI_LONG { get; set; } = 70;

        private Dictionary<Instrument, double> _entryPrices = new Dictionary<Instrument, double>();
        private Dictionary<Instrument, int> _numPositions = new Dictionary<Instrument, int>();

        private readonly double[] ENTRY_SIZE = { 0.1, 0.2, 0.3, 0.4 };

        protected override double Rules(Instrument i)
        {
            int ENTRY_MIN_RSI_SHORT = 100 - ENTRY_MAX_RSI_LONG;
            int EXIT_MAX_RSI_SHORT = 100 - EXIT_MIN_RSI_LONG;

            //----- calculate indicators

            var sma200 = i.Close.SMA(200);
            var rsi2 = i.Close.RSI(2);

            if (i.Position == 0)
                _numPositions[i] = 0;

            //----- enter positions

            if ((i.Position == 0 && i.Close[0] > sma200[0] && rsi2[0] < ENTRY_MAX_RSI_LONG && rsi2[1] < ENTRY_MAX_RSI_LONG)   // long
            || (i.Position == 0 && i.Close[0] < sma200[0] && rsi2[0] > ENTRY_MIN_RSI_SHORT && rsi2[1] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0]
                    ? ENTRY_SIZE[_numPositions[i] - 1]
                    : -ENTRY_SIZE[_numPositions[i] - 1];
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            || (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (_numPositions[i] < 4
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])    // long
            || (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // increase position size up to 3 times
                _numPositions[i]++;
                _entryPrices[i] = i.Close[0]; // update entry price!

                return i.Position > 0
                    ? ENTRY_SIZE[_numPositions[i] - 1]
                    : -ENTRY_SIZE[_numPositions[i] - 1];
            }

            return 0.0;
        }

        //protected override string MARKET => Assets.SPUU; // 2x leveraged
        //protected override string MARKET => Assets.SPXL; // 3x leveraged
    }
    #endregion
}

//==============================================================================
// end of file