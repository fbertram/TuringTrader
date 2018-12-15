//==============================================================================
// Project:     Trading Simulator
// Name:        RND_Ivy
// Description: Strategy as published in Mebane Faber's book
//              'The Ivy Portfolio'.
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
//==============================================================================
//
// see 'Ivy Portfolio'on Scott's Investments
// https://www.scottsinvestments.com/ivy-commission-free-portfolios/
// https://docs.google.com/spreadsheets/d/1DbLtigPIUy8-dgA_v9FN7rVM2_5BD_H2SMgHoYCW8Sg/edit#gid=956060578
//
// https://seekingalpha.com/article/4134753-ivy-portfolio-2018-update
//
// There are 2 options here, each with 5, 10, or 20 assets
// 
// __Timing Strategy__
// * Determine, if asset trading above or below 10-month moving average
//   - above: invested
//   - below: cash
//
// __Rotation Strategy___
// * Rank by average return 3 month, 6 month, 12 month
// * Hold top 5 (Ivy 10) or top 3 (Ivy 5)
// * When a security is trading below its 10-month simple moving average, the 
//   position is listed as "Cash." When the security is trading above its 10-month 
//   simple moving average the position is listed as "Invested."
// * Trading/ rebalancing once per month
//
// Instruments, see page 192:
// --- domestic stocks ---
// Domestic Large Cap:          20, 10, 5, VTI, Vanguard Total Stock Market ETF
// Domestic Mid Cap:            20,        VO,  
// Domestic Small Cap:          20, 10,    VB,  Vanguard Small Cap ETF
// Domestic Micro Cap:          20,        IWC
// --- foreign stocks ---
// Foreign Developed Stocks:    20, 10, 5, VEU, Vanguard FTSE All-World ex-US ETF
// Foreign Emerging Stocks:     20, 10,    VWO, Vanguard Emerging Markets Stock ETF
// Foreign Developed Small Cap: 20,        GWX
// Foreign Emerging Small Cap:  20,        EWX
// --- bonds ---
// Domestic Bonds:              20, 10, 5, BND, Vanguard Total Bond Market ETF
// TIPS:                        20, 10,    TIP, iShares Barclays TIPS Bond
// Foreign Bonds:               20,        BWX
// Emerging Bonds:              20,        ESD
// --- real estate ---
// Real Estate:                 20, 10, 5, VNQ, Vanguard REIT Index ETF
// Foreign Real Estate:         20, 10,    RWX, SPDR DJ International Real Estate ETF
// Infrastructure:              20,        IGF, iShares Global Infrastructure ETF
// Timber:                      20,        TREE.L, CAMBIUM GLOBAL TIMBERLAND LIMIT
// --- commodities ---
// Commodities:                 20,        DBA, Invesco DB Agriculture Fund
// Commodities:                 20,        DBE, Invesco DB Energy Fund
// Commodities:                 20,        DBB, Invesco DB Base Metals Fund
// Commodities:                 20,        DBP, Invesco DB Precious Metals Fund
// Commodities:                     10, 5, DBC, PowerShares DB Commodity Index Tracking
// Commodities:                     10,    GSG, S&P GSCI(R) Commodity-Indexed Trust
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
    class Faber_IvyPortfolio : Algorithm
    {
        #region private data
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.Index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        #endregion
        #region instruments & setup
        private struct AssetClass
        {
            public double weight;
            public int numpicks;
            public List<string> assets;
        }

        #if false
        #region  Ivy-5 portfolio: timing system
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF, available since 5/30/2007
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new HashSet<string> {
                "VTI.ETF", // 10, 5  Vanguard Total Stock Market ETF, data since 02/25/2005
                _safeInstrument
            } },
            //--- world equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new HashSet<string> {
                "VEU.ETF", // 10, 5, Vanguard FTSE All-World ex-US ETF, data since 03/08/2007
                _safeInstrument
            } },
            //--- credit
            new AssetClass { weight = 0.20, numpicks = 1, assets = new HashSet<string> {
                 "BND.ETF", // 10, 5, Vanguard Total Bond Market ETF, data since 04/10/2007
                _safeInstrument
            } },
            //--- real estate
            new AssetClass { weight = 0.20, numpicks = 1, assets = new HashSet<string> {
                "VNQ.ETF", // 10, 5, Vanguard REIT Index ETF, data since 02/25/2005
                _safeInstrument
            } },
            //--- economic stress
            new AssetClass { weight = 0.20, numpicks = 1, assets = new HashSet<string> {
                "DBC.ETF", // 10, 5, PowerShares DB Commodity Index Tracking, data since 02/03/2006
                _safeInstrument
            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(200)[0]
                ? 1.0
                : -1.0;
        };
        #endregion
        #endif
        #if false
        #region Ivy-5 portfolio: rotation system
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF, available since 5/30/2007
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                //--- domestic equity
                "VTI.ETF", // 10, 5  Vanguard Total Stock Market ETF, data since 02/25/2005
                //--- world equity
                "VEU.ETF", // 10, 5, Vanguard FTSE All-World ex-US ETF, data since 03/08/2007
                //--- credit
                 "BND.ETF", // 10, 5, Vanguard Total Bond Market ETF, data since 04/10/2007
                //--- real estate
                "VNQ.ETF", // 10, 5, Vanguard REIT Index ETF, data since 02/25/2005
                //--- economic stress
                "DBC.ETF", // 10, 5, PowerShares DB Commodity Index Tracking, data since 02/03/2006
                _safeInstrument,
                _safeInstrument,
                _safeInstrument

            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        };
        #endregion
        #endif
        #if false
        #region Ivy-10 portfolio: timing system
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF, available since 5/30/2007
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VB.ETF",  // 10,    Vanguard Small Cap ETF, data since 02/25/2005
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VTI.ETF", // 10, 5  Vanguard Total Stock Market ETF, data since 02/25/2005
                _safeInstrument
            } },
            //--- world equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VWO.ETF", // 10,    Vanguard Emerging Markets Stock ETF, data since 02/10/2005
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VEU.ETF", // 10, 5, Vanguard FTSE All-World ex-US ETF, data since 03/08/2007
                _safeInstrument
            } },
            //--- credit
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "BND.ETF", // 10, 5, Vanguard Total Bond Market ETF, data since 04/10/2007
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "TIP.ETF", // 10,    iShares Barclays TIPS Bond, data since 02/25/2005
                _safeInstrument
            } },
            //--- real estate
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "RWX.ETF", // 10,    SPDR DJ International Real Estate ETF, data since 12/15/2006
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VNQ.ETF", // 10, 5, Vanguard REIT Index ETF, data since 02/25/2005
                _safeInstrument
            } },
            //--- economic stress
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "DBC.ETF", // 10, 5, PowerShares DB Commodity Index Tracking, data since 02/03/2006
                _safeInstrument
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "GSG.ETF", // 10,    S&P GSCI(R) Commodity-Indexed Trust, data since 07/21/2006
                _safeInstrument,
            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] / i.Close.EMA(210)[0] - 1.0;
        };
        #endregion
        #endif
        #if true
        #region Ivy-10 portfolio: rotation system
        private static readonly string _safeInstrument = "BIL.etf"; // SPDR Barclays 1-3 Month T-Bill ETF, available since 5/30/2007
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 5, assets = new List<string> {
                //--- domestic equity
                "VB.ETF",  // 10,    Vanguard Small Cap ETF, data since 02/25/2005
                "VTI.ETF", // 10, 5  Vanguard Total Stock Market ETF, data since 02/25/2005
                //--- world equity
                "VWO.ETF", // 10,    Vanguard Emerging Markets Stock ETF, data since 02/10/2005
                "VEU.ETF", // 10, 5, Vanguard FTSE All-World ex-US ETF, data since 03/08/2007
                //--- credit
                "BND.ETF", // 10, 5, Vanguard Total Bond Market ETF, data since 04/10/2007
                "TIP.ETF", // 10,    iShares Barclays TIPS Bond, data since 02/25/2005
                //--- real estate
                "RWX.ETF", // 10,    SPDR DJ International Real Estate ETF, data since 12/15/2006
                "VNQ.ETF", // 10, 5, Vanguard REIT Index ETF, data since 02/25/2005
                //--- economic stress
                "DBC.ETF", // 10, 5, PowerShares DB Commodity Index Tracking, data since 02/03/2006
                "GSG.ETF", // 10,    S&P GSCI(R) Commodity-Indexed Trust, data since 07/21/2006
                //---
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,
                _safeInstrument,

            } },
        };
        private static readonly Func<Instrument, double> _scoringFunc = (i) =>
        {
            if (i.Nickname == _safeInstrument)
                return 0.0;

            return i.Close[0] > i.Close.EMA(210)[0]
                ? (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
        };
        #endregion
        #endif
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            WarmupStartTime = DateTime.Parse("05/30/2007");
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Parse("11/30/2018, 4pm");

            AddDataSource(_spx);
            foreach (AssetClass assetClass in _assetClasses)
                foreach (string nick in assetClass.assets)
                    AddDataSource(nick);

            Deposit(_initialFunds);

            _plotter.Clear();

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // collect all of our trading instruments
                List<Instrument> instruments = _assetClasses
                    .SelectMany(s => s.assets)
                    .Distinct()
                    .Select(n => FindInstrument(n))
                    .ToList();

                // evaluate instrument momentum: make sure instrument indicators are only evaluated once!
                Dictionary<Instrument, double> instrumentMomentum = instruments
                    .ToDictionary(i => i,
                        i => _scoringFunc(i));

                // create empty structure for instrument weights
                Dictionary<Instrument, double> instrumentWeights = instruments
                    .ToDictionary(i => i, i => 0.0);

                // loop through all asset classes
                foreach (AssetClass assetClass in _assetClasses)
                {
                    List<Instrument> assetClassInstruments = assetClass.assets
                        .Select(n => FindInstrument(n))
                        .ToList();

                    var bestInstruments = assetClassInstruments
                        .OrderByDescending(i => instrumentMomentum[i])
                        .Take(assetClass.numpicks);

                    foreach (Instrument bestInstrument in bestInstruments)
                        instrumentWeights[bestInstrument] += assetClass.weight / assetClass.numpicks;
                }

                // execute trades once per month
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    double totalWeight = _assetClasses
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    string message = string.Format("{0:MM/dd/yyyy}: ", SimTime[0]);
                    foreach (var instrumentWeight in instrumentWeights)
                    {
                        message += string.Format("{0} = {1:P2}, ", instrumentWeight.Key.Symbol, instrumentWeight.Value);
                        //message += string.Format("{0:P2}, ", instrumentMomentum[instrumentWeight.Key]);
                        int targetShares = (int)Math.Floor(instrumentWeight.Value * equityUnit / instrumentWeight.Key.Close[0]);
                        int currentShares = instrumentWeight.Key.Position;

                        Order newOrder = instrumentWeight.Key.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }

                    if (TradingDays > 0)
                        Output.WriteLine(message);
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
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleChart");
        }
        #endregion
    }
}

//==============================================================================
// end of file