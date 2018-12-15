//==============================================================================
// Project:     Trading Simulator
// Name:        Antonacci_DualMomentumInvesting
// Description: Strategy, as published in Gary Antonacci's book
//              'Dual Momentum Investing'.
//              http://www.optimalmomentum.com/
// History:     2018xi22, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================
//
// Strategy
// * Equal weight for all 4 categories
// * Hold one of 2 instruments, if 1-year performance greater than other instrument, or cash
// * ... unless average of 3-month, 6-month, and 12-month performance is worse than holding cash
// * Trade once a month
//
// Instruments
// * Equity
// - VTI, US Equities
// - VEU, US Equities
// - SHY, Cash
// * Credit Risk
// - HYG, High Yield Bond
// - CIU, Interim Credit Bond
// - SHY, Cash
// * Real-Estate Risk
// - VNQ, Equity REIT
// - REM, Mortgage REIT
// - SHY, Cash
// * Economic Stress
// - GLD, Gold
// - TLT, Long-term Treasuries
// - Cash
//
// Also: SPY/EFA, TLT/GLD, VNQ/REM, HYG/CIU
//
//------------------------------------------------------------------------------
//
// Criticism:
//
//------------------------------------------------------------------------------
//
// further references:
// https://www.scottsinvestments.com/2016/05/25/updated-dual-momentum-test/
// https://www.scottsinvestments.com/wp-content/uploads/2016/05/dual2.png
// https://www.scottsinvestments.com/wp-content/uploads/2018/11/Dual-November-2018.png
//
// https://docs.google.com/spreadsheets/d/1S5YVvjIXexBOjonrpgSM0ngr3O-82NGalGnfbj5hOxU/edit#gid=1298415711
//
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
    class Antonacci_DualMomentumInvesting : Algorithm
    {
        #region inputs
        [OptimizerParam(1, 10, 1)] public int STOP_LEN = 126;
        [OptimizerParam(1, 10, 1)] public int STOP_TRAIL = 875;
        [OptimizerParam(1, 10, 1)] public int MOM_WGHT = 60;
        #endregion
        #region internal data
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.Index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        #endregion
        #region instruments
        private struct AssetClass
        {
            public double weight;
            public HashSet<string> assets;
        }

        private static readonly string _safeInstrument = "SHY.etf"; // available since 02/25/2005
        private static readonly HashSet<AssetClass> _assetClasses = new HashSet<AssetClass>
        {
            //--- equity
            new AssetClass { weight = 0.25, assets = new HashSet<string> {
                "VTI.ETF",   // available since 02/25/2005
                "VEU.ETF",   // available since 03/08/2007
                _safeInstrument
            } },
            //--- credit
            new AssetClass { weight = 0.25, assets = new HashSet<string> {
                "HYG.ETF",   // available since 04/11/2007
                //"CIU.ETF" => changed to IGIB in 06/2018
                "IGIB.ETF",  // available since 01/11/2007
                _safeInstrument
            } },
            //--- real estate
            new AssetClass { weight = 0.25, assets = new HashSet<string> {
                "VNQ.ETF",   // available since 02/25/2005
                "REM.ETF",   // available since 05/04/2007
                _safeInstrument
            } },
            //--- economic stress
            new AssetClass { weight = 0.25, assets = new HashSet<string> {
                "GLD.ETF",   // available since 02/25/2005
                "TLT.ETF",   // available since 02/25/2005
                _safeInstrument
            } },
        };
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            WarmupStartTime = DateTime.Parse("05/04/2007");
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
                        i => 1.0 / 3.0
                            * (i.Close[0] / i.Close[63]
                            + i.Close[0] / i.Close[126]
                            + i.Close[0] / i.Close[252]));

                // create empty structure for instrument weights
                Dictionary<Instrument, double> instrumentWeights = instruments
                    .ToDictionary(i => i, i => 0.0);

                // loop through all asset classes
                foreach (AssetClass assetClass in _assetClasses)
                {
                    List<Instrument> assetClassInstruments = assetClass.assets
                        .Select(n => FindInstrument(n))
                        .ToList();

                    var bestInstrument = assetClassInstruments
                        .OrderByDescending(i => instrumentMomentum[i])
                        .Take(1)
                        .First();

                    instrumentWeights[bestInstrument] += assetClass.weight;
                }

                // execute trades once per month
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    double totalWeight = _assetClasses
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    foreach (var instrumentWeight in instrumentWeights)
                    {
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