//==============================================================================
// Project:     Trading Simulator
// Name:        Antonacci_DualMomentumInvesting
// Description: Strategy, as published in Andreas F Clenow's book
//              'Stocks on the Move'.
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class Clenow_StocksOnTheMove : Algorithm
    {
        #region private data
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.Index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        #endregion
        #region list of trading instruments
        private readonly List<string> _tradingInstruments = new List<string>()
        {
            "AAL.Stock",
            "AAPL.Stock",
            "ABBV.Stock",
            "ABT.Stock",
            "ABX.Stock",
            "ACN.Stock",
            "ADBE.Stock",
            "ADI.Stock",
            "ADP.Stock",
            "ADSK.Stock",
            "AIG.Stock",
            "AKAM.Stock",
            "ALL.Stock",
            "ALXN.Stock",
            "AMAT.Stock",
            "AMGN.Stock",
            "AMZN.Stock",
            "APA.Stock",
            "APC.Stock",
            "ATVI.Stock",
            "AVGO.Stock",
            "AXP.Stock",
            "BA.Stock",
            "BAC.Stock",
            "BBBY.Stock",
            "BHP.Stock",
            "BIDU.Stock",
            "BIIB.Stock",
            "BK.Stock",
            "BMY.Stock",
            "BP.Stock",
            "BRK.A.Stock",
            "BRK.B.Stock",
            "C.Stock",
            "CA.Stock",
            "CAT.Stock",
            "CELG.Stock",
            "CERN.Stock",
            "CF.Stock",
            "CHK.Stock",
            "CHKP.Stock",
            "CHRW.Stock",
            "CHTR.Stock",
            "CL.Stock",
            "CLF.Stock",
            "CMCSA.Stock",
            "CMI.Stock",
            "COF.Stock",
            "COP.Stock",
            "COST.Stock",
            "CRM.Stock",
            "CSCO.Stock",
            "CTSH.Stock",
            "CTXS.Stock",
            "CVS.Stock",
            "CVX.Stock",
            "DE.Stock",
            "DIS.Stock",
            "DISCA.Stock",
            "DISCK.Stock",
            "DISH.Stock",
            "DLTR.Stock",
            "DTV.Stock",
            "DVN.Stock",
            "EA.Stock",
            "EBAY.Stock",
            "EMR.Stock",
            "EOG.Stock",
            "ESRX.Stock",
            "EXC.Stock",
            "EXPD.Stock",
            "F.Stock",
            "FAST.Stock",
            "FB.Stock",
            "FCX.Stock",
            "FDX.Stock",
            "FISV.Stock",
            "FOX.Stock",
            "FOXA.Stock",
            "GD.Stock",
            "GE.Stock",
            "GILD.Stock",
            "GLW.Stock",
            "GM.Stock",
            "GOOG.Stock",
            "GOOGL.Stock",
            "GRMN.Stock",
            "GS.Stock",
            "HAL.Stock",
            "HD.Stock",
            "HES.Stock",
            "HON.Stock",
            "HPQ.Stock",
            "HSIC.Stock",
            "IBM.Stock",
            "ILMN.Stock",
            "INTC.Stock",
            "INTU.Stock",
            "ISRG.Stock",
            "ITUB.Stock",
            "JCP.Stock",
            "JNJ.Stock",
            "JPM.Stock",
            "KLAC.Stock",
            "KMI.Stock",
            "KO.Stock",
            "LBTYA.Stock",
            "LBTYK.Stock",
            "LLY.Stock",
            "LMT.Stock",
            "LOW.Stock",
            "LRCX.Stock",
            "LVS.Stock",
            "M.Stock",
            "MA.Stock",
            "MAR.Stock",
            "MAT.Stock",
            "MCD.Stock",
            "MDLZ.Stock",
            "MDT.Stock",
            "MET.Stock",
            "MMM.Stock",
            "MNST.Stock",
            "MO.Stock",
            "MON.Stock.X",
            "MOS.Stock",
            "MRK.Stock",
            "MS.Stock",
            "MSFT.Stock",
            "MU.Stock",
            "MYL.Stock",
            "NEM.Stock",
            "NFLX.Stock",
            "NKE.Stock",
            "NLY.Stock",
            "NOV.Stock",
            "NSC.Stock",
            "NTAP.Stock",
            "NVDA.Stock",
            "NWSA.Stock",
            "NXPI.Stock",
            "ORCL.Stock",
            "ORLY.Stock",
            "OXY.Stock",
            "PAYX.Stock",
            "PBR.Stock",
            "PCAR.Stock",
            "PEP.Stock",
            "PFE.Stock",
            "PG.Stock",
            "PM.Stock",
            "PRU.Stock",
            "QCOM.Stock",
            "REGN.Stock",
            "RIG.Stock",
            "ROST.Stock",
            "RTN.Stock",
            "SBAC.Stock",
            "SBUX.Stock",
            "SINA.Stock",
            "SIRI.Stock",
            "SLB.Stock",
            "SO.Stock",
            "SPG.Stock",
            "SRCL.Stock",
            "STX.Stock",
            "SYMC.Stock",
            "T.Stock",
            "TGT.Stock",
            "TRIP.Stock",
            "TSCO.Stock",
            "TSLA.Stock",
            "TWX.Stock.X",
            "TXN.Stock",
            "UNH.Stock",
            "UNP.Stock",
            "UPS.Stock",
            "USB.Stock",
            "UTX.Stock",
            "V.Stock",
            "VALE.Stock",
            "VIAB.Stock",
            "VLO.Stock",
            "VOD.Stock",
            "VRSK.Stock",
            "VRTX.Stock",
            "VZ.Stock",
            "WBA.Stock",
            "WDC.Stock",
            "WFC.Stock",
            "WMT.Stock",
            "WYNN.Stock",
            "X.Stock",
            "XLNX.Stock",
            "XOM.Stock",
            "ALTR.Stock.X",
            "ANR.Stock.X",
            "BHI.Stock.X",
            "BRCM.Stock.X",
            "CAM.Stock.X",
            "CMCSK.Stock.X",
            "COV.Stock.X",
            "DELL.Stock.X",
            "DD.Stock.X",
            "DOW.Stock.X",
            "EMC.Stock.X",
            "EP.Stock.X",
            "GMCR.Stock.X",
            "LLTC.Stock.X",
            "LMCA.Stock.X",
            "LMCK.Stock.X",
            "LVNTA.Stock.X",
            "MHS.Stock.X",
            "MJN.Stock.X",
            "PCLN.Stock.X",
            "POT.Stock.X",
            "PSE.Stock.X",
            "QVCA.Stock.X",
            "SNDK.Stock.X",
            "SPLS.Stock.X",
            "STJ.Stock.X",
            "TWC.Stock.X",
            "VIP.Stock.X",
            "WAG.Stock.X",
            "WFM.Stock.X",
            "WLP.Stock.X",
            "WLT.Stock.X",
            "YHOO.Stock.X",
        };
        #endregion

        public override void Run()
        {
            //---------- initialization

            // set simulation time frame
            WarmupStartTime = DateTime.Parse("01/01/2005");
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Parse("11/30/2018, 4pm");

            // set account value
            Deposit(_initialFunds);
            CommissionPerShare = 0.015;

            // add instruments
            AddDataSource(_spx);
            foreach (string nickname in _tradingInstruments)
                AddDataSource(nickname);

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                // determine active instruments
                var activeInstruments = Instruments
                    .Where(i => _tradingInstruments.Contains(i.Nickname)
                            && i.Time[0] == simTime)
                    .ToList();

                // calculate indicators
                // store to list, to make sure indicators are evaluated
                // exactly once per bar
                var instrumentEvaluation = activeInstruments
                    .Select(i => new
                    {
                        instrument = i,
                        regression = i.Close.LogRegression(90),
                        maxMove = i.Close.LogReturn().Highest(90),
                        avg100 = i.Close.EMA(100),
                        atr20 = i.AverageTrueRange(20),
                    })
                    .ToList();

                var indexFilter = FindInstrument(_spx).Close
                    .Divide(FindInstrument(_spx).Close.EMA(200));

                // rank instruments by volatility-adjusted momentum,
                // determine position size as percentage of NAV, set
                // position size to zero for disqualified instruments
                var instrumentRanking = instrumentEvaluation
                    .OrderByDescending(e => e.regression.Slope[0] * e.regression.R2[0])
                    .Select(e => new
                    {
                        instrument = e.instrument,
                        positionSize = (e.maxMove[0] < 0.15 && e.instrument.Close[0] > e.avg100[0])
                            ? 0.001 / e.atr20[0] * e.instrument.Close[0]
                            : 0.0,
                    })
                    .ToList();

                // assign equity, until we run out of cash
                var instrumentEquity = Enumerable.Range(0, instrumentRanking.Count)
                    .ToDictionary(
                        i => instrumentRanking[i].instrument,
                        i => Math.Min(
                            instrumentRanking[i].positionSize,
                            instrumentRanking.Take(i).Sum(r => r.positionSize)));

                // trade once per week
                if (SimTime[0].DayOfWeek >= DayOfWeek.Wednesday && SimTime[1].DayOfWeek < DayOfWeek.Wednesday)
                {
                    foreach (Instrument instrument in instrumentEquity.Keys)
                    {
                        int currentShares = instrument.Position;
                        int targetShares = (int)Math.Round(NetAssetValue[0] * instrumentEquity[instrument] / instrument.Close[0]);

                        if (targetShares > currentShares)
                        {
                            // only buy more shares, if S&P is trading above its EMA(200)
                            if (indexFilter[0] > 0.0)
                                instrument.Trade(targetShares - currentShares);
                        }
                        else
                        {
                            instrument.Trade(targetShares - currentShares);
                        }
                    }
                }

                // create plots on Sheet 1
                if (TradingDays > 0)
                {
                    _spxInitial = _spxInitial ?? FindInstrument(_spx).Close[0];

                    _plotter.SelectChart(Name + " performance", "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0] / _initialFunds);
                    _plotter.Plot(_spx, FindInstrument(_spx).Close[0] / _spxInitial);
                    _plotter.Plot("DD", (NetAssetValue[0] - NetAssetValueHighestHigh) / NetAssetValueHighestHigh);
                }
            }

            //----- post processing

            // create trading log on Sheet 2
            _plotter.SelectChart(Name + " trades", "date");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("type", entry.InstrumentType);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                _plotter.Plot("commission", -entry.Commission);
                _plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
        }

        public override void Report()
        {
        }
    }
}

//==============================================================================
// end of file