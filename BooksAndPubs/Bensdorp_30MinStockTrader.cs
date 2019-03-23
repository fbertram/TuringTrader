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
            EndTime = DateTime.Now - TimeSpan.FromDays(5);

            foreach (var n in UNIVERSE)
                AddDataSource(n);
            AddDataSource(BENCHMARK);

            Deposit(1e6);
            CommissionPerShare = 0.015;

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
                            sma150 = i.Close.SMA(150),
                            adx7 = i.AverageDirectionalMovement(7).ADX,
                            atr10 = i.TrueRange().SMA(10),
                            rsi3 = i.Close.RSI(3),
                        });

                // * daily close must be > 150-day SMA
                // * 7-day ADX > 45
                // * 10-day ATR % > 4 %
                // * 3-day RSI < 30
                var filtered = _universe
                    .Where(i => i.Close[0] > indicators[i].sma150[0]
                        && indicators[i].adx7[0] > 45.0
                        && indicators[i].atr10.Divide(i.Close)[0] > 0.04
                        && indicators[i].rsi3[0] < 30)
                    .ToList();

                if (NextSimTime.DayOfWeek < SimTime[0].DayOfWeek)
                {
                    //----- time-based close

                    // exit after 4 days regardless
                    // FIXME: unclear, what 'after 4 days' means. we interpret
                    // it to be 'before entering new trades'
                    foreach (var i in Positions.Keys)
                        i.Trade(-i.Position).Comment = "time exit";

                    //----- determine instruments to trade

                    // Each week, sort all stocks that meet that criteria by RSI
                    // Then buy the 10 LOWEST RSI scores at the Monday open with 
                    // a LIMIT order 4 % BELOW the Friday close.
                    var entries = filtered
                        .OrderBy(i => indicators[i].rsi3[0])
                        .Take(10)
                        .ToList();

                    var tradeParameters = entries
                        .ToDictionary(
                            i => i,
                            i => new
                            {
                                // FIXME: is it possible to have stopLoss > entryPrice?
                                // this probably shouldn't happen, as we filtered
                                // stocks to have an ATR of more than 4%
                                entryPrice = i.Close[0] * 0.96,
                                stopLoss = i.Close[0] - 2.5 * indicators[i].atr10[0],
                            });

                    //----- open positions

                    double totalRisk = 0.10;
                    foreach (var i in entries)
                    {
                        // FIXME: this is incomplete. we need to keep track
                        // of both, total risk, and total capital.

                        // risk 2 % equity per trade(entry - stop loss = "risk")
                        // max 10 simultaneous positions, or max 10 % of equity.
                        double riskPerTrade = Math.Min(0.02, totalRisk);
                        totalRisk = Math.Max(0.0, totalRisk - riskPerTrade);

                        double riskPerShare = Math.Max(0.0, tradeParameters[i].entryPrice - tradeParameters[i].stopLoss);
                        int targetShares = (int)Math.Floor(riskPerTrade * NetAssetValue[0] / riskPerShare);

                        i.Trade(targetShares, 
                            OrderType.limitNextBar, 
                            tradeParameters[i].entryPrice);
                    }
                }
                else
                {
                    //----- stop loss & profit target

                    // no stop loss on day1
                    // next day set stop loss at -2.5 * 10-day ATR
                    // exit at the next day open when the close shows a profit of 3 %, 
                    foreach (var position in Positions.Keys)
                    {
                        var entry = Log
                            .Where(l => (SimTime[0] - l.BarOfExecution.Time).TotalDays < 5.0
                                && l.Symbol == position.Symbol
                                && l.OrderTicket.Quantity > 0)
                            .First();

                        double entryPrice = entry.FillPrice;
                        double stopLoss = entryPrice - 2.5 * indicators[position].atr10[0];
                        double profitTarget = entryPrice * 1.03;

                        // NOTE: in order to have the worst case backtest result,
                        // we place the stop-loss order before the profit target
                        // order. that way, the stop-loss will have a higher priority
                        // in case both events happen on the same day.
                        position.Trade(-position.Position, 
                                OrderType.stopNextBar, 
                                stopLoss,
                                i => i.Position > 0)
                            .Comment = "stop loss";

                        // FIXME: slight deviation from rules. we are not exiting on
                        // the next open after hitting the profit target, but placing
                        // a limit order with the profit target. to make sure only
                        // one of the two competing orders is executed, we use an
                        // order condition.
                        position.Trade(-position.Position, 
                                OrderType.limitNextBar, 
                                profitTarget,
                                i => i.Position > 0)
                            .Comment = "profit target";
                    }
                }

                //----- output

                // plot to chart
                _plotter.SelectChart(Name, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot("nav", NetAssetValue[0]);
                _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);
            }

            //========== post processing ==========

            // print order log
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
                _plotter.Plot("$ Profit", trade.Quantity * (trade.Exit.FillPrice - trade.Entry.FillPrice));
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
                    "TFCF.stock",
                    "TFCFA.stock",
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