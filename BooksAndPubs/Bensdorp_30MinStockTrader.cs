//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Bensdorp_30MinStockTrader
// Description: Strategy, as published in Laurens Bensdorp's book
//              'The 30-Minute Stock Trader'.
// History:     2019iii19, FUB, created
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
    /// <summary>
    /// Mean-Reversion Long Strategy
    /// </summary>
    public abstract class Bensdorp_30MinStockTrader_MRL : Algorithm
    {
        #region inputs
        //[OptimizerParam(126, 252, 21)]
        public int SMA_DAYS = 150; // default = 150

        [OptimizerParam(0, 100, 5)]
        public int MIN_ADX = 45; // default = 45

        [OptimizerParam(0, 100, 5)]
        public int MAX_RSI = 30; // default = 30

        [OptimizerParam(100, 500, 50)]
        public int MIN_ATR = 400; // default = 4%

        //[OptimizerParam(1, 10, 1)]
        public int MAX_ENTRIES = 10; // default = 10

        [OptimizerParam(200, 500, 50)]
        public int STOP_LOSS = 250; // default = 2.5 * ATR

        //[OptimizerParam(100, 500, 50)]
        public int RISK_PER_TRADE = 200; // default = 2%

        //[OptimizerParam(100, 2000, 100)]
        public int CAP_PER_TRADE = 1000; // default = 10%

        [OptimizerParam(100, 500, 100)]
        public int PROFIT_TARGET = 300; // default = 3%
        #endregion
        #region internal data
        private static readonly string BENCHMARK = "$SPXTR.index";
        protected abstract List<string> UNIVERSE { get; }
        private Plotter _plotter = new Plotter();
        private Instrument _benchmark;
        private List<Instrument> _universe;
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            foreach (var n in UNIVERSE)
                AddDataSource(n);
            AddDataSource(BENCHMARK);

            Deposit(1e6);
            CommissionPerShare = 0.015;

            var entryParameters = Enumerable.Empty<Instrument>()
                .ToDictionary(
                    i => i,
                    i => new
                    {
                        entryPrice = 0.0,
                        stopLoss = 0.0,
                    });

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                //----- find instruments

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);
                _universe = Instruments
                    .Where(i => i != _benchmark)
                     .ToList();

                //----- calculate indicators

                // make sure we calculate indicators for the 
                // full universe on every bar
                var indicators = _universe
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            sma150 = i.Close.SMA(SMA_DAYS),
                            adx7 = i.ADX(7),
                            atr10 = i.AverageTrueRange(10),
                            rsi3 = i.Close.RSI(3),
                        });

                // * daily close must be > 150-day SMA
                // * 7-day ADX > 45
                // * 10-day ATR % > 4 %
                // * 3-day RSI < 30
                var filtered = _universe
                    .Where(i => i.Close[0] > indicators[i].sma150[0]
                        && indicators[i].adx7[0] > MIN_ADX
                        && indicators[i].atr10.Divide(i.Close)[0] > MIN_ATR/10000.0
                        && indicators[i].rsi3[0] < MAX_RSI)
                    .ToList();

                if (NextSimTime.DayOfWeek < SimTime[0].DayOfWeek)
                {
                    //----- time-based close

                    // exit after 4 days regardless
                    foreach (var i in Positions.Keys)
                        i.Trade(-i.Position).Comment = "time exit";

                    //----- determine instruments to trade

                    // each week, sort all stocks that meet that criteria by RSI
                    // buy the 10 LOWEST RSI scores at the Monday open with 
                    // a LIMIT order 4 % BELOW the Friday close.
                    var entries = filtered
                        .OrderBy(i => indicators[i].rsi3[0])
                        .Take(MAX_ENTRIES)
                        .ToList();

                    //----- open positions

                    foreach (var i in entries)
                    {
                        // save our entry parameters, so that we may access
                        // them later to manage exits
                        entryParameters[i] = new
                        {
                            entryPrice = i.Close[0] * (1.0 - MIN_ATR / 10000.0),
                            stopLoss = i.Close[0] - STOP_LOSS / 100.0 * indicators[i].atr10[0],
                        };

                        // calculate target shares in two ways:
                        // * fixed-fractional risk (with entry - stop-loss = "risk"), and
                        // * fixed percentage of total equity
                        double riskPerShare = Math.Max(0.0, entryParameters[i].entryPrice - entryParameters[i].stopLoss);
                        int sharesRiskLimited = (int)Math.Floor(RISK_PER_TRADE/10000.0 * NetAssetValue[0] / riskPerShare);
                        int sharesCapLimited = (int)Math.Floor(CAP_PER_TRADE/10000.0 * NetAssetValue[0] / i.Close[0]);
                        int targetShares = Math.Min(sharesRiskLimited, sharesCapLimited);

                        // place trade as limit order
                        i.Trade(targetShares, 
                            OrderType.limitNextBar,
                            entryParameters[i].entryPrice);
                    }
                }
                else
                {
                    //----- stop loss & profit target

                    // no stop loss on day 1
                    // next day set stop loss at -2.5 * 10-day ATR
                    // exit at the next day open when the close shows a profit of 3 %, 
                    foreach (var position in Positions.Keys)
                    {
                        double entryPrice = entryParameters[position].entryPrice;
                        double stopLoss = entryParameters[position].stopLoss;
                        double profitTarget = entryPrice * (1.0 + PROFIT_TARGET/10000.0);

                        if (position.Close[0] >= profitTarget)
                        {
                            position.Trade(-position.Position, 
                                    OrderType.openNextBar)
                                .Comment = "profit target";
                        }
                        else
                        {
                            position.Trade(-position.Position,
                                    OrderType.stopNextBar,
                                    stopLoss,
                                    i => i.Position > 0)
                                .Comment = "stop loss";
                        }
                    }
                }

                //----- output

                // plot to chart
                _plotter.SelectChart(Name + " " + OptimizerParamsAsString, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot("nav", NetAssetValue[0]);
                _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);
            }

            //========== post processing ==========

            // print order log
#if false
            _plotter.SelectChart(Name + " orders", "date");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("type", entry.InstrumentType);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                //_plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                //_plotter.Plot("commission", -entry.Commission);
                _plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
#endif

            // print position log
            var tradeLog = LogAnalysis
                .GroupPositions(Log)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            _plotter.SelectChart(Name + " positions", "entry date");
            foreach (var trade in tradeLog)
            {
                _plotter.SetX(trade.Entry.BarOfExecution.Time);
                _plotter.Plot("exit date", trade.Exit.BarOfExecution.Time);
                _plotter.Plot("Symbol", trade.Symbol);
                _plotter.Plot("Quantity", trade.Quantity);
                _plotter.Plot("% Profit", trade.Exit.FillPrice / trade.Entry.FillPrice - 1.0);
                _plotter.Plot("Exit", trade.Exit.OrderTicket.Comment ?? "");
                //_plotter.Plot("$ Profit", trade.Quantity * (trade.Exit.FillPrice - trade.Entry.FillPrice));
            }
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    #region S&P 100 (OEX)
    public class Bensdorp_30MinStockTrader_MRL_OEX : Bensdorp_30MinStockTrader_MRL
    {
        protected override List<string> UNIVERSE
        {
            get
            {
                return new List<string>()
                {
                    // Trade all US stocks, but filter out 
                    // * ETFs; 
                    // * stocks < $10; and 
                    // * average daily volume< 500,000 over last 50 days.

                    // here, we use S&P 100, as of 03/20/2019
                    "AAPL.stock",
                    "ABBV.stock",
                    "ABT.stock",
                    "ACN.stock",
                    "ADBE.stock",
                    "AGN.stock",
                    "AIG.stock",
                    "ALL.stock",
                    "AMGN.stock",
                    "AMZN.stock",
                    "AXP.stock",
                    "BA.stock",
                    "BAC.stock",
                    "BIIB.stock",
                    "BK.stock",
                    "BKNG.stock",
                    "BLK.stock",
                    "BMY.stock",
                    "BRK.B.stock",
                    "C.stock",
                    "CAT.stock",
                    "CELG.stock",
                    "CHTR.stock",
                    "CL.stock",
                    "CMCSA.stock",
                    "COF.stock",
                    "COP.stock",
                    "COST.stock",
                    "CSCO.stock",
                    "CVS.stock",
                    "CVX.stock",
                    "DHR.stock",
                    "DIS.stock",
                    "DUK.stock",
                    "DWDP.stock",
                    "EMR.stock",
                    "EXC.stock",
                    "F.stock",
                    "FB.stock",
                    "FDX.stock",
                    "GD.stock",
                    "GE.stock",
                    "GILD.stock",
                    "GM.stock",
                    "GOOG.stock",
                    "GOOGL.stock",
                    "GS.stock",
                    "HAL.stock",
                    "HD.stock",
                    "HON.stock",
                    "IBM.stock",
                    "INTC.stock",
                    "JNJ.stock",
                    "JPM.stock",
                    "KHC.stock",
                    "KMI.stock",
                    "KO.stock",
                    "LLY.stock",
                    "LMT.stock",
                    "LOW.stock",
                    "MA.stock",
                    "MCD.stock",
                    "MDLZ.stock",
                    "MDT.stock",
                    "MET.stock",
                    "MMM.stock",
                    "MO.stock",
                    "MRK.stock",
                    "MS.stock",
                    "MSFT.stock",
                    "NEE.stock",
                    "NFLX.stock",
                    "NKE.stock",
                    "NVDA.stock",
                    "ORCL.stock",
                    "OXY.stock",
                    "PEP.stock",
                    "PFE.stock",
                    "PG.stock",
                    "PM.stock",
                    "PYPL.stock",
                    "QCOM.stock",
                    "RTN.stock",
                    "SBUX.stock",
                    "SLB.stock",
                    "SO.stock",
                    "SPG.stock",
                    "T.stock",
                    "TGT.stock",
                    "TXN.stock",
                    "UNH.stock",
                    "UNP.stock",
                    "UPS.stock",
                    "USB.stock",
                    "UTX.stock",
                    "V.stock",
                    "VZ.stock",
                    "WBA.stock",
                    "WFC.stock",
                    "WMT.stock",
                    "XOM.stock",
                };
            }
        }
    }
    #endregion
    #region Nasdaq 100 (NDX)
    public class Bensdorp_30MinStockTrader_MRL_NDX : Bensdorp_30MinStockTrader_MRL
    {
        protected override List<string> UNIVERSE
        {
            get
            {
                return new List<string>()
                {
                    // Trade all US stocks, but filter out 
                    // * ETFs; 
                    // * stocks < $10; and 
                    // * average daily volume< 500,000 over last 50 days.

                    // here, we use Nasdaq 100, as of 03/21/2019
                    "AAL.stock",
                    "AAPL.stock",
                    "ADBE.stock",
                    "ADI.stock",
                    "ADP.stock",
                    "ADSK.stock",
                    "ALGN.stock",
                    "ALXN.stock",
                    "AMAT.stock",
                    "AMD.stock",
                    "AMGN.stock",
                    "AMZN.stock",
                    "ASML.stock",
                    "ATVI.stock",
                    "AVGO.stock",
                    "BIDU.stock",
                    "BIIB.stock",
                    "BKNG.stock",
                    "BMRN.stock",
                    "CDNS.stock",
                    "CELG.stock",
                    "CERN.stock",
                    "CHKP.stock",
                    "CHTR.stock",
                    "CMCSA.stock",
                    "COST.stock",
                    "CSCO.stock",
                    "CSX.stock",
                    "CTAS.stock",
                    "CTRP.stock",
                    "CTSH.stock",
                    "CTXS.stock",
                    "DLTR.stock",
                    "EA.stock",
                    "EBAY.stock",
                    "EXPE.stock",
                    "FAST.stock",
                    "FB.stock",
                    "FISV.stock",
                    "GILD.stock",
                    "GOOG.stock",
                    "GOOGL.stock",
                    "HAS.stock",
                    "HSIC.stock",
                    "IDXX.stock",
                    "ILMN.stock",
                    "INCY.stock",
                    "INTC.stock",
                    "INTU.stock",
                    "ISRG.stock",
                    "JBHT.stock",
                    "JD.stock",
                    "KHC.stock",
                    "KLAC.stock",
                    "LBTYA.stock",
                    "LBTYK.stock",
                    "LRCX.stock",
                    "LULU.stock",
                    "MAR.stock",
                    "MCHP.stock",
                    "MDLZ.stock",
                    "MELI.stock",
                    "MNST.stock",
                    "MSFT.stock",
                    "MU.stock",
                    "MXIM.stock",
                    "MYL.stock",
                    "NFLX.stock",
                    "NTAP.stock",
                    "NTES.stock",
                    "NVDA.stock",
                    "NXPI.stock",
                    "ORLY.stock",
                    "PAYX.stock",
                    "PCAR.stock",
                    "PEP.stock",
                    "PYPL.stock",
                    "QCOM.stock",
                    "REGN.stock",
                    "ROST.stock",
                    "SBUX.stock",
                    "SIRI.stock",
                    "SNPS.stock",
                    "SWKS.stock",
                    "SYMC.stock",
                    "TFCF.stock.x",
                    "TFCFA.stock.x",
                    "TMUS.stock",
                    "TSLA.stock",
                    "TTWO.stock",
                    "TXN.stock",
                    "UAL.stock",
                    "ULTA.stock",
                    "VRSK.stock",
                    "VRSN.stock",
                    "VRTX.stock",
                    "WBA.stock",
                    "WDAY.stock",
                    "WDC.stock",
                    "WLTW.stock",
                    "WYNN.stock",
                    "XEL.stock",
                    "XLNX.stock",
                };
            }
        }
    }
    #endregion
}

//==============================================================================
// end of file