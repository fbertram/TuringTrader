//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Connors_ShortTermTradingStrategies
// Description: Strategy, as published in Larry Connors and Cesar Alvarez book
//              'Short Term Trading Strategies That Work'.
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
    // recommended instruments/ markets
    // SPY (S&P 500)
    // QQQ (Nasdaq)
    // FXI (China)
    // EWZ (Brazil)

    //----- 2-period RSI
    #region 2-period RSI under 5
    public class Connors_ShortTermTrading_RsiUnder5 : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";

        [OptimizerParam(0, 20, 1)]
        public virtual int ENTRY_MAX_RSI { get; set; } = 5;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

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

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketSma5 = _market.Close.SMA(5);
                var marketRsi2 = _market.Close.RSI(2);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0] 
                    && marketRsi2[0] < ENTRY_MAX_RSI)
                    {
                        _market.Trade(
                            (int)Math.Floor(NetAssetValue[0] / _market.Close[0]), 
                            OrderType.closeThisBar);
                    }
                }

                //----- exit positions

                else
                {
                    if (_market.Close[0] > marketSma5[0])
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
    #region Cumulative RSI
    public class Connors_ShortTermTrading_CumulativeRsi : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";

        [OptimizerParam(1, 5, 1)]
        public virtual int CUM_RSI_DAYS { get; set; } = 2;

        [OptimizerParam(20, 80, 5)]
        public virtual int ENTRY_MAX_CUM_RSI { get; set; } = 35;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_RSI { get; set; } = 65;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

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

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketRsi2 = _market.Close.RSI(2);
                var marketCumRsi = marketRsi2.Sum(CUM_RSI_DAYS);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && marketCumRsi[0] < ENTRY_MAX_CUM_RSI)
                    {
                        _market.Trade((int)Math.Floor(NetAssetValue[0] / _market.Close[0]));
                    }
                }

                //----- exit positions

                else
                {
                    if (marketRsi2[0] > EXIT_MIN_RSI)
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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

    //----- Double 7's
    #region Double 7's
    public class Connors_ShortTermTrading_Double7 : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";

        [OptimizerParam(5, 10, 1)]
        public virtual int DOUBLE_DAYS { get; set; } = 7;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

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

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketHi7 = _market.TypicalPrice().Highest(DOUBLE_DAYS);
                var marketLo7 = _market.TypicalPrice().Lowest(DOUBLE_DAYS);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && marketLo7[0] < marketLo7[1])
                    {
                        _market.Trade((int)Math.Floor(NetAssetValue[0] / _market.Close[0]));
                    }
                }

                //----- exit positions

                else
                {
                    if (marketHi7[0] > marketHi7[1])
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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

    //----- 5 Strategies to time the market
    #region 1) VIX Stretches
    public class Connors_ShortTermTrading_VixStretches : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";
        private static readonly string VOLATILITY = "$VIX.index";

        [OptimizerParam(2, 5, 1)]
        public virtual int ENTRY_MIN_VOL_STRETCH_DAYS { get; set; } = 3;

        [OptimizerParam(1, 10, 1)]
        public virtual int VOL_STRETCH_PCNT {get; set; } = 5;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_RSI { get; set; } = 65;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        private Instrument _volatility;
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(MARKET);
            AddDataSource(VOLATILITY);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);
                _volatility = _volatility ?? FindInstrument(VOLATILITY);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketRsi2 = _market.Close.RSI(2);

                var volSma10 = _volatility.Close.SMA(10);

                // we cap the excess volatility at 1e-10, and 
                // sum it up over 3 days. if sum is larger
                // than 2.5e-10, we must have 3 or more up days
                var volStretch = _volatility.Close
                    .Subtract(volSma10.Multiply(1.0 + VOL_STRETCH_PCNT / 100.0))
                    .Min(1e-10)
                    .Sum(ENTRY_MIN_VOL_STRETCH_DAYS);
                    
                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && volStretch[0] > (ENTRY_MIN_VOL_STRETCH_DAYS - 0.5) * 1e-10)
                    {
                        _market.Trade((int)Math.Floor(NetAssetValue[0] / _market.Close[0]));
                    }
                }

                //----- exit positions

                else
                {
                    if (marketRsi2[0] > EXIT_MIN_RSI)
                        _market.Trade(-_market.Position);
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
    #region 2) VIX RSI
    public class Connors_ShortTermTrading_VixRsi : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";
        private static readonly string VOLATILITY = "$VIX.index";

        [OptimizerParam(75, 100, 5)]
        public virtual int ENTRY_MIN_VIX_RSI { get; set; } = 90;

        [OptimizerParam(0, 50, 5)]
        public virtual int ENTRY_MAX_MKT_RSI { get; set; } = 30;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_MKT_RSI { get; set; } = 65;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        private Instrument _volatility;
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(MARKET);
            AddDataSource(VOLATILITY);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);
                _volatility = _volatility ?? FindInstrument(VOLATILITY);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketRsi2 = _market.Close.RSI(2);
                var volRsi2 = _volatility.Close.RSI(2);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && marketRsi2[0] < ENTRY_MAX_MKT_RSI
                    && volRsi2[0] > ENTRY_MIN_VIX_RSI
                    && _volatility.Open[0] > _volatility.Close[1])
                    {
                        _market.Trade(
                            (int)Math.Floor(NetAssetValue[0] / _market.Close[0]),
                            OrderType.closeThisBar);
                    }
                }

                //----- exit positions

                else
                {
                    if (marketRsi2[0] > EXIT_MIN_MKT_RSI)
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
    #region 3) TRIN
    public class Connors_ShortTermTrading_Trin : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";
        private static readonly string TRIN = "#SPXTRIN.index";

        [OptimizerParam(45, 75, 5)]
        public virtual int ENTRY_MAX_RSI { get; set; } = 50;

        [OptimizerParam(1, 5, 1)]
        public virtual int ENTRY_MIN_TRIN_UP_DAYS { get; set; } = 3;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_RSI { get; set; } = 65;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        private Instrument _trin;
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(MARKET);
            AddDataSource(TRIN);

            Deposit(INITIAL_CAPITAL);
            CommissionPerShare = 0.015;

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);
                _trin = _trin ?? FindInstrument(TRIN);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketRsi2 = _market.Close.RSI(2);

                // we cap the TRIN at 1.0, and sum it up
                // over 3 days. if the sum is larger than 2.5,
                // the TRIN must have beeen above 1.0 for 3
                // or more days
                var trinUpDays = _trin.Close.Min(1.0).Sum(ENTRY_MIN_TRIN_UP_DAYS);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && marketRsi2[0] < ENTRY_MAX_RSI
                    && trinUpDays[0] > ENTRY_MIN_TRIN_UP_DAYS - 0.5)
                    {
                        _market.Trade(
                            (int)Math.Floor(NetAssetValue[0] / _market.Close[0]),
                            OrderType.closeThisBar);
                    }
                }

                //----- exit positions

                else
                {
                    if (marketRsi2[0] > EXIT_MIN_RSI)
                        _market.Trade(-_market.Position);
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
    #region 4) Cumulative RSI (one more)
    public class Connors_ShortTermTrading_MoreCumulativeRsi : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";

        [OptimizerParam(1, 5, 1)]
        public virtual int RSI_CUM_DAYS { get; set; } = 2;

        [OptimizerParam(20, 80, 5)]
        public virtual int ENTRY_MAX_CUM_RSI { get; set; } = 45;

        [OptimizerParam(50, 90, 5)]
        public virtual int EXIT_MIN_RSI { get; set; } = 65;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

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

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketRsi3 = _market.Close.RSI(3);
                var marketCumRsi = marketRsi3.Sum(RSI_CUM_DAYS);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] > marketSma200[0]
                    && marketCumRsi[0] < ENTRY_MAX_CUM_RSI)
                    {
                        _market.Trade((int)Math.Floor(NetAssetValue[0] / _market.Close[0]));
                    }
                }

                //----- exit positions

                else
                {
                    if (marketRsi3[0] > EXIT_MIN_RSI)
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
    #region 5) Short Side
    public class Connors_ShortTermTradingStrategies_ShortSide : Algorithm
    {
        #region settings
        private static readonly string MARKET = "$SPX.index";

        [OptimizerParam(2, 7, 1)]
        public virtual int ENTRY_UP_DAYS { get; set; } = 4;
        #endregion
        #region internal data
        private static readonly double INITIAL_CAPITAL = 1e6;

        private Plotter _plotter = new Plotter();
        private Instrument _market;
        #endregion

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

            foreach (var s in SimTimes)
            {
                _market = _market ?? FindInstrument(MARKET);

                //----- calculate indicators

                var marketSma200 = _market.Close.SMA(200);
                var marketSma5 = _market.Close.SMA(5);

                // we cap the return at 1e-10, and sum it
                // up over 4 required days. if sum is larger
                // than 3e-10, we must have 4 up days
                var marketUpDays = _market.Close.Return().Min(1e-10).Sum(ENTRY_UP_DAYS);

                //----- enter positions

                if (_market.Position == 0)
                {
                    if (_market.Close[0] < marketSma200[0]
                    && marketUpDays[0] > (ENTRY_UP_DAYS - 1.0) * 1e-10)
                    {
                        _market.Trade(-(int)Math.Floor(NetAssetValue[0] / _market.Close[0]));
                    }
                }

                //----- exit positions

                else
                {
                    if (_market.Close[0] < marketSma5[0]
                    || _market.Close[0] > marketSma200[0]) // this line is not in the book
                    {
                        _market.Trade(-_market.Position);
                    }
                }

                //----- output

                if (!IsOptimizing)
                {
                    // plot to chart
                    _plotter.SelectChart(Name + ": " + OptimizerParamsAsString, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("nav", NetAssetValue[0]);
                    _plotter.Plot(_market.Symbol, _market.Close[0]);
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
}

//==============================================================================
// end of file