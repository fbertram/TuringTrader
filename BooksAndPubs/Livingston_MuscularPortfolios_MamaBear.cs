//==============================================================================
// Project:     Trading Simulator
// Name:        Livingston_MuscularPortfolios_MamaBear
// Description: 'Mama Bear' strategy, as published in Brian Livingston's book
//              'Muscular Portfolios'.
//               https://muscularportfolios.com/
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================
//
// Strategy:
// * Menu of 9 ETFs, covering all major asset classes
// * Rank by average return 3 month, 6 month, 12 month
// * Hold top 3
// * Trading/ rebalancing once per month
//
//------------------------------------------------------------------------------
//
// Criticism:
// * the book was published in 2018, and shows performance
//   data between 01/01/1973 and 12/31/2015
// * why was no performance shown later than that?
// * many of the ETFs used in the portfolio have not been around for
//   that timeframe. how were those data backfilled?
// * maximum drawdown in 2008 (31%) does not match book (21%)
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
    class Livingston_MuscularPortfolios_MamaBear : Algorithm
    {
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        private HashSet<string> _etfMenu = new HashSet<string>()
        {
#if true
            //--- equities
            "VONE.etf", // Vanguard Russell 1000 ETF, available since 11/16/2010
            "VIOO.etf", // Vanguard Small-Cap 600 ETF, available since 09/14/2010
            "VEA.etf",  // Developed-market stocks, available since 7/26/2007
            "VWO.etf",  // Emerging-market stocks, available since 3/10/2005
            //--- hard assets
            "VNQ.etf",  // US real-estate investment trusts, available since 2/25/2005
            "PDBC.etf", // Commodities, available since 11/11/2014
            "IAU.etf",  // Gold, available since 2/25/2005
            //--- fixed-income
            "VGLT.etf", // Vanguard Long-Term Govt. Bond ETF, available since 11/24/2009
            "SHV.etf",  // iShares Short-Term Treasury ETF, available since 01/11/2007
#else
            // the book mentions that CXO is using different ETFs:
            SPY
            IWM
            EFA
            EEM
            VNQ
            DBC
            GLD
            TLT
            Cash
#endif
        };

        override public void Run()
        {
            //----- algorithm setup
            WarmupStartTime = DateTime.Parse("01/01/2006");
            StartTime = DateTime.Parse("01/01/2008");
            //EndTime = DateTime.Parse("12/31/2015, 4pm"); // as published in book
            EndTime = DateTime.Parse("11/30/2018, 4pm");

            AddDataSource(_spx);
            foreach (string nick in _etfMenu)
                AddDataSource(nick);

            Deposit(100000);
            //CommissionPerShare = 0.015; // it is unclear, if the book considers commissions

            //----- simulation loop
            foreach (DateTime simTime in SimTimes)
            {
                // find active instruments. note that many instruments
                // have not been around for the whole simulation period
                var instruments = Instruments
                    .Where(i => _etfMenu.Contains(i.Nickname));

                // evaluate instruments. note that we store this in a
                // dictionary, to make sure indicators are only evaluated
                // once
                var evaluation = instruments
                    .ToDictionary(
                        i => i,
                        i => i.Close[0] / i.Close[5 * 21]);

                // select top-3 instruments
                const int numHold = 3;
                var top3 = evaluation
                    .OrderByDescending(e => e.Value)
                    .Select(e => e.Key)
                    .Take(numHold);

                // calculate target percentage and how far we are off
                double targetPercentage = 1.0 / numHold;
                double maxOff = instruments
                    .Max(i => (top3.Count() > 0 && top3.Contains(i) ? 1.0 : 0.0)
                        * Math.Abs(i.Position * i.Close[0] / NetAssetValue[0] - targetPercentage) / targetPercentage);

                // rebalance only once per month, and only if we need more than 20% change
                if (SimTime[0].Month != SimTime[1].Month
                    && maxOff > 0.2)
                {
                    foreach (Instrument i in instruments)
                    {
                        // determine current and target shares per instrument...
                        double targetEquity = (top3.Contains(i) ? targetPercentage : 0.0) * NetAssetValue[0];
                        int targetShares = (int)Math.Floor(targetEquity / i.Close[0]);
                        int currentShares = i.Position;

                        // ... and trade the delta
                        Order newOrder = i.Trade(targetShares - currentShares);

                        // add a comment, to make the trading log easier to read
                        if (newOrder != null)
                        {
                            if (currentShares == 0)
                                newOrder.Comment = "Open";
                            else if (targetShares == 0)
                                newOrder.Comment = "Close";
                            else
                                newOrder.Comment = "Rebalance";
                        }
                    }
                }

                // create plots on Sheet 1
                if (SimTime[0] >= StartTime)
                {
                    _spxInitial = _spxInitial ?? FindInstrument(_spx).Close[0];

                    _plotter.SelectChart(Name + " performance", "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(_spx, FindInstrument(_spx).Close[0] / _spxInitial);
                    _plotter.Plot("NAV", NetAssetValue[0] / _initialFunds);
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
            _plotter.OpenWith("SimpleChart");
        }
    }
}

//==============================================================================
// end of file