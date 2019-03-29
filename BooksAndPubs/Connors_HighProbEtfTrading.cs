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
    #region Universes
    partial class Universes
    {
        public static List<string> SPX = new List<string>() { "$SPX.index" };
        public static List<string> Connors20 = new List<string>()
        {
            //--- US market indices
            "SPY.etf", // SPDR S&P 500
            "QQQ.etf", // Invesco QQQ Trust, Series 1
            "DIA.etf", // SPDR Dow Jones Industrial Average ETF (was Diamonds Trust, Series 1)
            "IWM.etf", // iShares Russell 2000 Index
            //--- US sectors
            "XHB.etf", // SPDR S&P Homebuilders
            "XLB.etf", // SPDR Materials
            "XLE.etf", // SPDR Energy Select
            "XLF.etf", // SPDR Financial Select
            "XLI.etf", // SPDR Industrial
            "XLV.etf", // SPDR Health Care
            //--- countries
            "EEM.etf", // iShares MSCI Emerging Markets Index
            "EFA.etf", // iShares MSCI EAFE Index
            "EWH.etf", // iShares MSCI Hong Kong Index
            "EWJ.etf", // iShares MSCI Japan Index
            "EWT.etf", // iShares MSCI Taiwan Index
            "EWZ.etf", // iShares MSCI Brazil Index
            "FXI.etf", // iShares FTSE/ Xinhua China 25 Index
            "ILF.etf", // iShares S&P Latin America 40 Index
            //--- hard assets
            "GLD.etf", // SPDR Gold Trust
            "IYR.etf", // iShares Dow Jones US Real Estate
        };
        public static List<string> USMarkets = new List<string>()
        {
            "SPY.etf", // S&P 500
            "QQQ.etf", // Nasdaq 100
            "DIA.etf", // Dow Jones Industrial Average
            "IWM.etf", // Russell 2000
        };
        public static List<string> Sectors = new List<string>()
        {
            "$SPXLTR.index", // communication services
            "$SPXDTR.index", // consumer discretionary
            "$SPXSTR.index", // consumer staples
            "$SPXETR.index", // energy
            "$SPXFTR.index", // finance
            "$SPXATR.index", // health care
            "$SPXITR.index", // industrial
            "$SPXTTR.index", // information technology
            "$SPXMTR.index", // materials
            "$SPXRTR.index", // real estate
            "$SPXUTR.index", // utilities
        };
    }
    #endregion
    #region common algorithm core
    public abstract class Connors_HighProbEtfTrading_Core : Algorithm
    {
        #region settings
        protected virtual List<string> UNIVERSE { get; } = Universes.USMarkets;
        protected virtual string BENCHMARK { get; } = "$SPX.index";

        [OptimizerParam(10, 100, 10)]
        public virtual int MAX_ENTRY_SIZE { get; set; } = 25;

        [OptimizerParam(0, 1, 1)]
        public virtual int AGGRESSIVE_ON { get; set; } = 1;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _benchmark;
        #endregion

        protected abstract Order Rules(Instrument i);

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            foreach (var n in UNIVERSE)
                AddDataSource(n);
            AddDataSource(BENCHMARK);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            //========== simulation loop ==========

            var entryPrices = new Dictionary<Instrument, double>();

            foreach (var s in SimTimes)
            {
                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);
                IEnumerable<Instrument> universe = Instruments
                    .Where(i => UNIVERSE.Contains(i.Nickname))
                    .ToList();

                if (universe.Count() == 0)
                    continue;

                List<Order> orders = new List<Order>();

                //----- create entries/ exits

                foreach (var i in universe)
                {
                    var newOrder = Rules(i);

                    if (newOrder != null)
                        orders.Add(newOrder);
                }

                //----- adjust position size & submit orders

                int numEntries = orders
                    .Where(o => o.Comment != null)
                    .Count();

                double dollarsAllocated = Positions.Keys.Sum(i => Math.Abs(i.Position) * i.Close[0]);
                double dollarsAvailable = NetAssetValue[0] - dollarsAllocated;
                double dollarsPerEntry = Math.Max(0.0, dollarsAvailable / Math.Max(1, numEntries));
                dollarsPerEntry = Math.Min(NetAssetValue[0] * MAX_ENTRY_SIZE / 100.0, dollarsPerEntry);

                foreach (var o in orders)
                {
                    // fill in order quantity of entries
                    if (o.Comment != null)
                        o.Quantity = (int)Math.Floor(o.Quantity * dollarsPerEntry / o.Instrument.Close[0]);

                    QueueOrder(o);
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);

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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };

            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            ||       (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi4[0] > EXIT_MIN_RSI_LONG)
            ||       (i.Position < 0 && rsi4[0] < EXIT_MAX_RSI_SHORT))
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi4[0] < ENTRY2_MAX_RSI_LONG)    // long
            ||  (i.Position < 0 && rsi4[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            ||       (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && percentB[0] > EXIT_MIN_BB_LONG / 100.0)   // long
            ||       (i.Position < 0 && percentB[0] < EXIT_MAX_BB_SHORT / 100.0)) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && percentB[0] < ENTRY_MAX_BB_LONG / 100.0)    // long
            ||  (i.Position < 0 && percentB[0] > ENTRY_MIN_BB_SHORT / 100.0))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            || (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && i.Close[0] < _entryPrices[i])   // long
            || (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && i.Close[0] > sma5[0])  // long
            ||       (i.Position < 0 && i.Close[0] < sma5[0])) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 2
            && ((i.Position > 0 && rsi2[0] < ENTRY2_MAX_RSI_LONG)    // long
            ||  (i.Position < 0 && rsi2[0] > ENTRY2_MIN_RSI_SHORT))) // short
            {
                // make sure we increase position size only once
                _numPositions[i]++;

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
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

        protected override Order Rules(Instrument i)
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

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Close[0] > sma200[0] ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            //----- exit positions

            else if ((i.Position > 0 && rsi2[0] > EXIT_MIN_RSI_LONG)   // long
            ||       (i.Position < 0 && rsi2[0] < EXIT_MAX_RSI_SHORT)) // short
            {
                return new Order
                {
                    Instrument = i,
                    Quantity = -i.Position,
                    Type = OrderType.closeThisBar,
                    Comment = null, // don't put anything here!
                };
            }

            //----- aggressive version: increase position size
            // note that this is different from the book. instead of
            // scaling up in a 1-2-3-4 pattern, these are all equal

            else if (AGGRESSIVE_ON > 0 && _numPositions[i] < 4
            && ((i.Position > 0 && i.Close[0] <_entryPrices[i])    // long
            ||  (i.Position < 0 && i.Close[0] > _entryPrices[i]))) // short
            {
                // increase position size up to 3 times
                _numPositions[i]++;
                _entryPrices[i] = i.Close[0]; // update entry price!

                return new Order
                {
                    Instrument = i,
                    Quantity = i.Position > 0 ? 1 : -1,
                    Type = OrderType.closeThisBar,
                    Comment = "entry", // use this to identify entry orders
                };
            }

            return null;
        }
    }
    #endregion
}

//==============================================================================
// end of file