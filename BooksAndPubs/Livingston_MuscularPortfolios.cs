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
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
        private Plotter _plotter = new Plotter();
        #endregion
        #region ETF menu & momentum calculation
#if MAMA_BEAR
        #region Mama Bear
        private string _name = "Mama Bear";

        private HashSet<string> _etfMenu = new HashSet<string>()
        {

#if false
            // note that some instruments have not been around
            // until 2014, making this hard to simulate

            //--- equities
            "VONE.etf", // Vanguard Russell 1000 ETF
            "VIOO.etf", // Vanguard Small-Cap 600 ETF
            "VEA.etf",  // Vanguard FTSE Developed Markets ETF
            "VWO.etf",  // Vanguard FTSE Emerging Markets ETF
            //--- hard assets
            "VNQ.etf",  // Vanguard Real Estate ETF
            "PDBC.etf", // Invesco Optimum Yield Diversified Commodity Strategy ETF
            "IAU.etf",  // iShares Gold Trust
            //--- fixed-income
            "VGLT.etf", // Vanguard Long-Term Govt. Bond ETF
            "SHV.etf",  // iShares Short-Term Treasury ETF
#else
            // the book mentions that CXO is using different ETFs
            // we use these, to simulate back to 2007
            
            //--- equities
            "SPY.etf", // SPDR S&P 500 Trust ETF
            "IWM.etf", // iShares Russell 2000 ETF
            "EFA.etf", // iShares MSCI EAFE ETF
            "EEM.etf", // iShares MSCI Emerging Markets ETF
            //--- hard assets
            "VNQ.etf", // Vanguard Real Estate ETF
            "DBC.etf", // Invesco DB Commodity Index Tracking ETF
            "GLD.etf", // SPDR Gold Shares ETF
            //--- fixed income
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
            // Cash... substituted by T-Bill, to make strategy work
            "BIL.etf"  // SPDR Bloomberg Barclays 1-3 Month T-Bill ETF
#endif
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
            "VTV.etf",  // Vanguard Value Index ETF
            "VUG.etf",  // Vanguard Growth Index ETF
            "VIOV.etf", // Vanguard S&P Small-Cap 600 Value Index ETF
            "VIOG.etf", // Vanguard S&P Small-Cap 600 Growth Index ETF
            "VEA.etf",  // Vanguard Developed Markets Index ETF
            "VWO.etf",  // Vanguard Emerging Market Stock Index ETF
            //--- hard assets
            "VNQ.etf",  // Vanguard Real Estate Index ETF
            "PDBC.etf", // Invesco Optimum Yield Diversified Commodity Strategy ETF
            "IAU.etf",  // iShares Gold ETF
            //--- fixed-income
            "EDV.etf",  // Vanguard Extended Duration ETF
            "VGIT.etf", // Vanguard Intermediate-Term Treasury Index ETF
            "VCLT.etf", // Vanguard Long-Term Corporate Bond Index ETF
            "BNDX.etf", // Vanguard Total International Bond Index ETF
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
            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now - TimeSpan.FromDays(3);

            AddDataSource(_spx);
            foreach (string nick in _etfMenu)
                AddDataSource(nick);

            Deposit(100000);
            //CommissionPerShare = 0.015; // the book does not deduct commissions

            //----- simulation loop
            foreach (DateTime simTime in SimTimes)
            {
                // calculate momentum w/ algorithm-specific helper function
                var evaluation = Instruments
                    .ToDictionary(
                        i => i,
                        i => _momentum(i));

                // skip, if there are any missing instruments
                // we want to make sure our strategy has all instruments available
                bool instrumentsMissing = _etfMenu
                    .Where(n => Instruments.Where(i => i.Nickname == n).Count() == 0)
                    .Count() > 0;

                if (instrumentsMissing)
                    continue;

                // find our trading instruments
                var instruments = Instruments
                    .Where(i => _etfMenu.Contains(i.Nickname));

                // rank, and select top-3 instruments
                const int numHold = 3;
                var top3 = instruments
                    .OrderByDescending(i => evaluation[i])
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
                    _plotter.SelectChart(_name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("NAV", NetAssetValue[0]);
                    _plotter.Plot(_spx, FindInstrument(_spx).Close[0]);
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
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
}

//==============================================================================
// end of file