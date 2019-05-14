//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Connors_HighProbEtfTrading
// Description: Strategy, as published in Larry Connors and Cesar Alvarez book
//              'High Probability ETF Trading'.
// History:     2019iii28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace BooksAndPubs
{
    #region common algorithm core
    public abstract class Connors_HighProbEtfTrading_Core : Algorithm
    {
        #region settings
        protected virtual string MARKET { get; } = "$SPX";

        [OptimizerParam(0, 1, 1)]
        public virtual int AGGRESSIVE_ON { get; set; } = 0;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

        protected abstract double Rules(Instrument i);

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(MARKET);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            //========== simulation loop ==========

            var entryPrices = new Dictionary<Instrument, double>();

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                double percentToBuySell = Rules(_market);

                //----- entries

                if (_market.Position == 0 && percentToBuySell != 0.0
                || _market.Position > 0 && percentToBuySell > 0
                || _market.Position < 0 && percentToBuySell < 0)
                {
                    int sharesToBuySell = (int)(Math.Sign(percentToBuySell) * Math.Floor(
                        Math.Abs(percentToBuySell) * NetAssetValue[0] / _market.Close[0]));

                    _market.Trade(sharesToBuySell, OrderType.closeThisBar);
                }

                //----- exits

                if (_market.Position > 0 && percentToBuySell < 0
                || _market.Position < 0 && percentToBuySell > 0)
                {
                    // none of the algorithms attempt to gradually
                    // exit positions, so this is good enough
                    _market.Trade(-_market.Position, OrderType.closeThisBar);
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);

                    // placeholder for 2nd sheet
                    _plotter.SelectChart(Name + " positions", "entry date");

                    _plotter.SelectChart(Name + " % invested", "entry date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("% long", Positions.Keys.Sum(i => Math.Max(0, i.Position) * i.Close[0]) / NetAssetValue[0]);
                    _plotter.Plot("% short", Positions.Keys.Sum(i => -Math.Min(0, i.Position) * i.Close[0]) / NetAssetValue[0]);
                    _plotter.Plot("% total", Positions.Keys.Sum(i => Math.Abs(i.Position) * i.Close[0]) / NetAssetValue[0]);
                }
            }

            //========== post processing ==========

            //----- print position log, grouped as LIFO

            if (!IsOptimizing)
            {
                var tradeLog = LogAnalysis
                    .GroupPositions(Log, true)
                    .OrderBy(i => i.Entry.BarOfExecution.Time);

                _plotter.SelectChart(Name + " positions", "entry date");
                foreach (var trade in tradeLog)
                {
                    _plotter.SetX(trade.Entry.BarOfExecution.Time);
                    _plotter.Plot("exit date", trade.Exit.BarOfExecution.Time);
                    _plotter.Plot("Symbol", trade.Symbol);
                    _plotter.Plot("Quantity", trade.Quantity);
                    _plotter.Plot("% Profit", (trade.Quantity > 0 ? 1.0 : -1.0) * (trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0));
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
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
    #endregion

    //===== 7 strategies
    #region 3-Day High/Low
    public class Connors_HighProbEtfTrading_3DayHighLow : Connors_HighProbEtfTrading_Core
    {
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
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && up3))  // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            ||       (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region RSI 25 & RSI 75
    public class Connors_HighProbEtfTrading_Rsi25Rsi75 : Connors_HighProbEtfTrading_Core
    {
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
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && rsi4[0] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi4[0] > EXIT_MIN_RSI_LONG)
            ||       (i.Position < 0 && rsi4[0] < EXIT_MAX_RSI_SHORT))
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi4[0] < ENTRY2_MAX_RSI_LONG)    // long
            ||  (i.Position < 0 && rsi4[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region R3
    public class Connors_HighProbEtfTrading_R3 : Connors_HighProbEtfTrading_Core
    {
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
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && rsiUp))  // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            ||       (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region %b
    public class Connors_HighProbEtfTrading_PercentB : Connors_HighProbEtfTrading_Core
    {
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
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && bHi)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
            }

            //----- exit positions

            else if ((i.Position > 0 && percentB[0] > EXIT_MIN_BB_LONG / 100.0)   // long
            ||       (i.Position < 0 && percentB[0] < EXIT_MAX_BB_SHORT / 100.0)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && percentB[0] < ENTRY_MAX_BB_LONG / 100.0)    // long
            ||  (i.Position < 0 && percentB[0] > ENTRY_MIN_BB_SHORT / 100.0))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region MDU and MDD
    public class Connors_HighProbEtfTrading_MduMdd : Connors_HighProbEtfTrading_Core
    {
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

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
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

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region RSI 10/6 & RSI 90/94
    public class Connors_HighProbEtfTrading_Rsi1006Rsi9094 : Connors_HighProbEtfTrading_Core
    {
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
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && rsi2[0] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0] ? 1.0 : -1.0;
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            ||       (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi2[0] < ENTRY2_MAX_RSI_LONG)    // long
            ||  (i.Position < 0 && rsi2[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return i.Position > 0 ? 1.0 : -1.0;
            }

            return 0.0;
        }
    }
    #endregion
    #region TPS
    public class Connors_HighProbEtfTrading_Tps : Connors_HighProbEtfTrading_Core
    {
        [OptimizerParam(10, 40, 5)]
        public int ENTRY_MAX_RSI_LONG = 25;

        [OptimizerParam(60, 80, 5)]
        public int EXIT_MIN_RSI_LONG = 70;

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

            if ((i.Position == 0 && i.Close[0] > sma200[0] && rsi2[0] < ENTRY_MAX_RSI_LONG  && rsi2[1] < ENTRY_MAX_RSI_LONG)   // long
            ||  (i.Position == 0 && i.Close[0] < sma200[0] && rsi2[0] > ENTRY_MIN_RSI_SHORT && rsi2[1] > ENTRY_MIN_RSI_SHORT)) // short
            {
                _numPositions[i] = 1;
                _entryPrices[i] = i.Close[0];

                return i.Close[0] > sma200[0]
                    ? ENTRY_SIZE[_numPositions[i] - 1]
                    : -ENTRY_SIZE[_numPositions[i] - 1];
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            ||       (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return i.Position > 0 ? -1 : 1;
            }

            //----- aggressive version: increase position size

            else if (_numPositions[i] < 4
            && ((i.Position > 0 && i.Close[0] <_entryPrices[i])    // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
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
    }
    #endregion
}

//==============================================================================
// end of file