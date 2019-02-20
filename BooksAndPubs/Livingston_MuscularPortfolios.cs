//==============================================================================
// Project:     TuringTrader Demos
// Name:        Livingston_MuscularPortfolios
// Description: 'Mama Bear' and 'Papa Bear' strategies, as published in Brian Livingston's book
//              'Muscular Portfolios'.
//               https://muscularportfolios.com/
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#define MAMA_BEAR
//#define PAPA_BEAR

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
    public class Livingston_MuscularPortfolios : Algorithm
    {
        #region internal data
        private const double _initialFunds = 100000;
        private string _spx = "^SPX.index";
        private double? _spxInitial = null;
        private Plotter _plotter = new Plotter();
        #endregion
        #region ETF menu & momentum calculation
#if MAMA_BEAR
        #region Mama Bear
        private string _name = "Mama Bear";

        private HashSet<string> _etfMenu = new HashSet<string>()
        {

            // note that some instruments have not been around for the whole
            // simulation period, leading to skewed results

            //--- equities
            "VONE.etf", // Vanguard Russell 1000 ETF, available since 11/16/2010
            "VIOO.etf", // Vanguard Small-Cap 600 ETF, available since 09/14/2010
            "VEA.etf",  // Vanguard FTSE Developed Markets ETF, available since 7/26/2007
            "VWO.etf",  // Vanguard FTSE Emerging Markets ETF, available since 3/10/2005
            //--- hard assets
            "VNQ.etf",  // Vanguard Real Estate ETF, available since 2/25/2005
            "PDBC.etf", // Invesco Optimum Yield Diversified Commodity Strategy ETF, available since 11/11/2014
            "IAU.etf",  // iShares Gold Trust, available since 2/25/2005
            //--- fixed-income
            "VGLT.etf", // Vanguard Long-Term Govt. Bond ETF, available since 11/24/2009
            "SHV.etf",  // iShares Short-Term Treasury ETF, available since 01/11/2007

            // the book mentions that CXO is using different ETFs:
            // SPY
            // IWM
            // EFA
            // EEM
            // VNQ
            // DBC
            // GLD
            // TLT
            // Cash
        };

        // simple 5-month momentum
        private Func<Instrument, double> _momentum = (i) => i.Close[0] / i.Close[5 * 21] - 1.0;
        #endregion
#endif
#if PAPAA_BEAR
        #region Papa Bear
        private string _name = "Papa Bear";

        private HashSet<string> _etfMenu = new HashSet<string>()
        {
            // note that some instruments have not been around for the whole
            // simulation period, leading to skewed results

            //--- equities
            "VTV.etf",  // US large-cap value stocks, available since 2/25/2005
            "VUG.etf",  // US large-cap growth stocks, available since 2/25/2005
            "VIOV.etf", // US small-cap value stocks, available since 9/14/2010
            "VIOG.etf", // US small-cap growth stocks, available since 9/14/2010
            "VEA.etf",  // Developed-market stocks, available since 7/26/2007
            "VWO.etf",  // Emerging-market stocks, available since 3/10/2005
            //--- hard assets
            "VNQ.etf",  // US real-estate investment trusts, available since 2/25/2005
            "PDBC.etf", // Commodities, available since 11/11/2014
            "IAU.etf",  // Gold, available since 2/25/2005
            //--- fixed-income
            "EDV.etf",  // US Treasury bonds, 30-year, available since 12/13/2007
            "VGIT.etf", // US Treasury notes, 10-year, available since 11/23/2009
            "VCLT.etf", // US investment-grade corporate bonds, available since 11/23/2009
            "BNDX.etf", // Non-US govt. & corporate bonds, available since 6/7/2013
        };

        // average momentum over 3, 6, and 12 months
        private Func<Instrument, double> _momentum = (i) =>
            (4.0 * (i.Close[0] / i.Close[63] - 1.0)
            + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
            + 1.0 * (i.Close[0] / i.Close[252] - 1.0)) / 3.0;
        #endregion
#endif
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //----- algorithm setup
            WarmupStartTime = DateTime.Parse("01/01/2006");
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Parse("12/31/2018, 4pm");

            AddDataSource(_spx);
            foreach (string nick in _etfMenu)
                AddDataSource(nick);

            Deposit(100000);
            //CommissionPerShare = 0.015; // the book does not deduct commissions

            //----- simulation loop
            foreach (DateTime simTime in SimTimes)
            {
                // find active instruments
                var instruments = Instruments
                    .Where(i => _etfMenu.Contains(i.Nickname));

                // calculate momentum w/ algorithm-specific helper function
                var evaluation = instruments
                    .ToDictionary(
                        i => i,
                        i => _momentum(i));

                // rank, and select top-3 instruments
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

                // rebalance once per month, and only if we need adjustments exceeding 20%
                if (SimTime[0].Month != SimTime[1].Month
                    && maxOff > 0.20)
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

                    _plotter.SelectChart(_name + " performance", "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(_spx, FindInstrument(_spx).Close[0] / _spxInitial);
                    _plotter.Plot("NAV", NetAssetValue[0] / _initialFunds);
                    _plotter.Plot("DD", (NetAssetValue[0] - NetAssetValueHighestHigh) / NetAssetValueHighestHigh);
                }
            }

            //----- post processing

            // create trading log on Sheet 2
            _plotter.SelectChart(_name + " trades", "date");
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