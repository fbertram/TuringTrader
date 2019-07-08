//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Clenow_StocksOnTheMove
// Description: Strategy, as published in Andreas F. Clenow's book
//              'Stocks on the Move'.
//              http://www.followingthetrend.com/
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
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
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Clenov_StocksOnTheMove_Core : Algorithm
    {
        #region inputs
        [OptimizerParam(63, 252, 21)]
        public int MOM_PERIOD = 90;

        [OptimizerParam(110, 125, 5)]
        public int MAX_MOVE = 115;

        [OptimizerParam(63, 252, 21)]
        public int INSTR_FLT = 100;

        [OptimizerParam(5, 25, 5)]
        public int ATR_PERIOD = 20;

        [OptimizerParam(63, 252, 21)]
        public int INDEX_FLT = 200;

        // Clenow uses 20 here. We need to use a higher number,
        // as we are using a much smaller universe.
        [OptimizerParam(5, 50, 5)]
        public int TOP_PCNT = 25;
        #endregion
        #region private data
        private readonly string BENCHMARK = "$SPX";
        protected abstract List<string> UNIVERSE { get; }
        private readonly double INITIAL_FUNDS = 100000;
        private Plotter _plotter = new Plotter();
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //---------- initialization

            // set simulation time frame
            WarmupStartTime = DateTime.Parse("01/01/2005");
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            // set account value
            Deposit(INITIAL_FUNDS);
            CommissionPerShare = 0.015;

            // add instruments
            AddDataSource(BENCHMARK);
            foreach (string nickname in UNIVERSE)
                AddDataSource(nickname);

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                // determine active instruments
                var activeInstruments = Instruments
                    .Where(i => UNIVERSE.Contains(i.Nickname)
                            && i.Time[0] == simTime)
                    .ToList();

                // calculate indicators
                // store to list, to make sure indicators are evaluated
                // exactly once per bar
                var instrumentEvaluation = activeInstruments
                    .Select(i => new
                    {
                        instrument = i,
                        regression = i.Close.LogRegression(MOM_PERIOD),
                        maxMove = i.Close.LogReturn().AbsValue().Highest(MOM_PERIOD),
                        avg100 = i.Close.EMA(INSTR_FLT),
                        atr20 = i.AverageTrueRange(ATR_PERIOD),
                    })
                    .ToList();

                // 1) rank instruments by volatility-adjusted momentum
                // 2) determine position size with risk equal to 10-basis points
                //    of NAV per instrument, based on 20-day ATR
                // 3) disqualify instruments, when
                //    - trading below 100-day moving average
                //    - negative momentum
                //    - maximum move > 15%
                var instrumentRanking = instrumentEvaluation
                    .OrderByDescending(e => e.regression.Slope[0] * e.regression.R2[0])
                    .Select(e => new
                    {
                        instrument = e.instrument,
                        positionSize = (e.maxMove[0] < Math.Log(MAX_MOVE / 100.0)
                                && e.instrument.Close[0] > e.avg100[0]
                                && e.regression.Slope[0] > 0.0)
                            ? 0.001 / e.atr20[0] * e.instrument.Close[0]
                            : 0.0,
                    })
                    .ToList();

                // assign equity, until we run out of cash,
                // or we are no longer in the top-ranking 20%
                var instrumentEquity = Enumerable.Range(0, instrumentRanking.Count)
                    .ToDictionary(
                        i => instrumentRanking[i].instrument,
                        i => i <= TOP_PCNT / 100.0 * instrumentRanking.Count
                            ? Math.Min(
                                instrumentRanking[i].positionSize,
                                Math.Max(0.0, 1.0 - instrumentRanking.Take(i).Sum(r => r.positionSize)))
                            : 0.0);

                // index filter: only buy any shares, while S&P-500 is trading above its 200-day moving average
                var indexFilter = FindInstrument(BENCHMARK).Close
                    .Divide(FindInstrument(BENCHMARK).Close.EMA(INDEX_FLT));

                // trade once per week
                // this is a slight simplification from Clenow's suggestion to change positions
                // every week, and adjust position sizes only every other week
                if (SimTime[0].DayOfWeek >= DayOfWeek.Wednesday && SimTime[1].DayOfWeek < DayOfWeek.Wednesday)
                {
                    foreach (Instrument instrument in instrumentEquity.Keys)
                    {
                        int currentShares = instrument.Position;
                        int targetShares = (int)Math.Round(NetAssetValue[0] * instrumentEquity[instrument] / instrument.Close[0]);

                        if (indexFilter[0] < 0.0)
                            targetShares = Math.Min(targetShares, currentShares);

                        instrument.Trade(targetShares - currentShares);
                    }

                    string message = instrumentEquity
                        .Where(i => i.Value != 0.0)
                        .Aggregate(string.Format("{0:MM/dd/yyyy}: ", SimTime[0]),
                            (prev, next) => prev + string.Format("{0}={1:P2} ", next.Key.Symbol, next.Value));
                    Output.WriteLine(message);
                }

                // create plots on Sheet 1
                if (TradingDays > 0)
                {
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot(BENCHMARK, FindInstrument(BENCHMARK).Close[0]);
                }
            }

            //----- post processing

            // print position log, grouped as LIFO
            var tradeLog = LogAnalysis
                .GroupPositions(Log, true)
                .OrderBy(i => i.Entry.BarOfExecution.Time);

            _plotter.SelectChart(Name + " positions", "entry date");
            foreach (var trade in tradeLog)
            {
                _plotter.SetX(trade.Entry.BarOfExecution.Time.Date);
                _plotter.Plot("exit date", trade.Exit.BarOfExecution.Time.Date);
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

    #region 200 of the most liquid US stocks
    public class Clenov_StocksOnTheMove : Clenov_StocksOnTheMove_Core
    {
        protected override List<string> UNIVERSE
        {
            get
            {
                return new List<string>()
                {
                    "AAL",
                    "AAPL",
                    "ABBV",
                    "ABT",
                    //"ABX",
                    "ACN",
                    "ADBE",
                    "ADI",
                    "ADP",
                    "ADSK",
                    "AIG",
                    "AKAM",
                    "ALL",
                    "ALXN",
                    "AMAT",
                    "AMGN",
                    "AMZN",
                    "APA",
                    "APC",
                    "ATVI",
                    "AVGO",
                    "AXP",
                    "BA",
                    "BAC",
                    "BBBY",
                    "BHP",
                    "BIDU",
                    "BIIB",
                    "BK",
                    "BMY",
                    "BP",
                    "BRK.A",
                    "BRK.B",
                    "C",
                    //"CA.x",
                    "CAT",
                    "CELG",
                    "CERN",
                    "CF",
                    "CHK",
                    "CHKP",
                    "CHRW",
                    "CHTR",
                    "CL",
                    "CLF",
                    "CMCSA",
                    "CMI",
                    "COF",
                    "COP",
                    "COST",
                    "CRM",
                    "CSCO",
                    "CTSH",
                    "CTXS",
                    "CVS",
                    "CVX",
                    "DE",
                    "DIS",
                    "DISCA",
                    "DISCK",
                    "DISH",
                    "DLTR",
                    "DTV",
                    "DVN",
                    "EA",
                    "EBAY",
                    "EMR",
                    "EOG",
                    //"ESRX.X",
                    "EXC",
                    "EXPD",
                    "F",
                    "FAST",
                    "FB",
                    "FCX",
                    "FDX",
                    "FISV",
                    //"TFCF.X",
                    //"TFCFA.X",
                    "GD",
                    "GE",
                    "GILD",
                    "GLW",
                    "GM",
                    "GOOG",
                    "GOOGL",
                    "GRMN",
                    "GS",
                    "HAL",
                    "HD",
                    "HES",
                    "HON",
                    "HPQ",
                    "HSIC",
                    "IBM",
                    "ILMN",
                    "INTC",
                    "INTU",
                    "ISRG",
                    "ITUB",
                    "JCP",
                    "JNJ",
                    "JPM",
                    "KLAC",
                    "KMI",
                    "KO",
                    "LBTYA",
                    "LBTYK",
                    "LLY",
                    "LMT",
                    "LOW",
                    "LRCX",
                    "LVS",
                    "M",
                    "MA",
                    "MAR",
                    "MAT",
                    "MCD",
                    "MDLZ",
                    "MDT",
                    "MET",
                    "MMM",
                    "MNST",
                    "MO",
                    //"MON.X",
                    "MOS",
                    "MRK",
                    "MS",
                    "MSFT",
                    "MU",
                    "MYL",
                    "NEM",
                    "NFLX",
                    "NKE",
                    "NLY",
                    "NOV",
                    "NSC",
                    "NTAP",
                    "NVDA",
                    "NWSA",
                    "NXPI",
                    "ORCL",
                    "ORLY",
                    "OXY",
                    "PAYX",
                    "PBR",
                    "PCAR",
                    "PEP",
                    "PFE",
                    "PG",
                    "PM",
                    "PRU",
                    "QCOM",
                    "REGN",
                    "RIG",
                    "ROST",
                    "RTN",
                    "SBAC",
                    "SBUX",
                    "SINA",
                    "SIRI",
                    "SLB",
                    "SO",
                    "SPG",
                    "SRCL",
                    "STX",
                    "SYMC",
                    "T",
                    "TGT",
                    "TRIP",
                    "TSCO",
                    "TSLA",
                    //"TWX.X",
                    "TXN",
                    "UNH",
                    "UNP",
                    "UPS",
                    "USB",
                    "UTX",
                    "V",
                    "VALE",
                    "VIAB",
                    "VLO",
                    "VOD",
                    "VRSK",
                    "VRTX",
                    "VZ",
                    "WBA",
                    "WDC",
                    "WFC",
                    "WMT",
                    "WYNN",
                    "X",
                    "XLNX",
                    "XOM",
                    //"ALTR.X",
                    //"ANR.X",
                    //"BHI.X",
                    //"BRCM.X",
                    //"CAM.X",
                    //"CMCSK.X",
                    //"COV.X",
                    //"DELL.X",
                    //"DD.X",
                    //"DOW.X",
                    //"EMC.X",
                    //"EP.X",
                    //"GMCR.X",
                    //"LLTC.X",
                    //"LMCA.X",
                    //"LMCK.X",
                    //"LVNTA.X",
                    //"MHS.X",
                    //"MJN.X",
                    //"PCLN.X",
                    //"POT.X",
                    //"PSE.X",
                    //"QVCA.X",
                    //"SNDK.X",
                    //"SPLS.X",
                    //"STJ.X",
                    //"TWC.X",
                    //"VIP.X",
                    //"WAG.X",
                    //"WFM.X",
                    //"WLP.X",
                    //"WLT.X",
                    //"YHOO.X",
                };
            }
        }
    }
    #endregion
}

//==============================================================================
// end of file