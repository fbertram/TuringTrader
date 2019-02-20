//==============================================================================
// Project:     TuringTrader Demos
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
    public class Antonacci_DualMomentumInvesting : Algorithm
    {
        #region internal data
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.Index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        #endregion
        #region instruments
        private static readonly HashSet<HashSet<string>> _assetClasses = new HashSet<HashSet<string>>
        {
            //--- equity
            new HashSet<string> {
                "VTI.ETF",   // available since 02/25/2005
                "VEU.ETF",   // available since 03/08/2007
                // could use SPY/ EFA here
                "SHY.etf",   // available since 02/25/2005
            },
            //--- credit
            new HashSet<string> {
                "HYG.ETF",   // available since 04/11/2007
                //"CIU.ETF" => changed to IGIB in 06/2018
                "IGIB.ETF",  // available since 01/11/2007
                "SHY.etf",   // available since 02/25/2005
            },
            //--- real estate
            new HashSet<string> {
                "VNQ.ETF",   // available since 02/25/2005
                "REM.ETF",   // available since 05/04/2007
                "SHY.etf",   // available since 02/25/2005
            },
            //--- economic stress
            new HashSet<string> {
                "GLD.ETF",   // available since 02/25/2005
                "TLT.ETF",   // available since 02/25/2005
                "SHY.etf",   // available since 02/25/2005
            },
        };
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            WarmupStartTime = DateTime.Parse("05/04/2007");
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Parse("12/31/2018, 4pm");

            AddDataSource(_spx);
            foreach (HashSet<string> assetClass in _assetClasses)
                foreach (string nick in assetClass)
                    AddDataSource(nick);

            Deposit(_initialFunds);
            CommissionPerShare = 0.015; // it is unclear, if the book considers commissions

            _plotter.Clear();

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // collect all of our trading instruments
                // note that the safe instrument is duplicated
                // in each asset class
                List<Instrument> instruments = _assetClasses
                    .SelectMany(s => s)
                    .Distinct()
                    .Select(n => FindInstrument(n))
                    .ToList();

                // evaluate instrument momentum
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
                foreach (HashSet<string> assetClass in _assetClasses)
                {
                    List<Instrument> assetClassInstruments = assetClass
                        .Select(n => FindInstrument(n))
                        .ToList();

                    // find the instrument with the highest momentum
                    // in each asset class
                    var bestInstrument = assetClassInstruments
                        .OrderByDescending(i => instrumentMomentum[i])
                        .Take(1)
                        .First();

                    // sum up the weights (because the safe instrument is duplicated)
                    instrumentWeights[bestInstrument] += 1.0 / _assetClasses.Count;
                }

                // execute trades once per month
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    foreach (var instrumentWeight in instrumentWeights)
                    {
                        int targetShares = (int)Math.Floor(instrumentWeight.Value * NetAssetValue[0] / instrumentWeight.Key.Close[0]);
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