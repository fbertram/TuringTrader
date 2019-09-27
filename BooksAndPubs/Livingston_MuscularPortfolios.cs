//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Livingston_MuscularPortfolios
// Description: 'Mama Bear' and 'Papa Bear' strategies, as published in Brian Livingston's book
//              'Muscular Portfolios'.
//               https://muscularportfolios.com/
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Livingston_MuscularPortfolios : Algorithm
    {
        #region internal data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string BENCHMARK = "@60_40";
        private Instrument _benchmark = null;
        private Plotter _plotter = new Plotter();

        protected HashSet<string> _etfMenu = null;
        protected abstract double _momentum(Instrument i);
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //----- algorithm setup
            StartTime = DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture);
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(BENCHMARK);
            foreach (string nick in _etfMenu)
                AddDataSource(nick);

            Deposit(INITIAL_FUNDS);
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
                if (!HasInstruments(_etfMenu))
                    continue;

                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);

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

                if (SimTime[0] >= StartTime)
                {
                    // create plots on Sheet 1
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(Name, NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Name, _benchmark.Close[0]);

                    // create holdings on Sheet 2
                    _plotter.SelectChart("Strategy Holdings", "date");
                    _plotter.SetX(SimTime[0]);
                    foreach (var i in Positions.Keys)
                        _plotter.Plot(i.Symbol, i.Position * i.Close[0] / NetAssetValue[0]);
                }
            }

            //----- post processing

            // create trading log on Sheet 3
            _plotter.SelectChart("Strategy Trades", "date");
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

    #region Mama Bear
    public class Livingston_MuscularPortfolios_MamaBear : Livingston_MuscularPortfolios
    {
        public override string Name { get { return "Mama Bear Strategy"; } }
        public Livingston_MuscularPortfolios_MamaBear()
        {
            _etfMenu = new HashSet<string>()
            {
    #if false
                // note that some instruments have not been around
                // until 2014, making this hard to simulate

                //--- equities
                "VONE", // Vanguard Russell 1000 ETF
                "VIOO", // Vanguard Small-Cap 600 ETF
                "VEA",  // Vanguard FTSE Developed Markets ETF
                "VWO",  // Vanguard FTSE Emerging Markets ETF
                //--- hard assets
                "VNQ",  // Vanguard Real Estate ETF
                "PDBC", // Invesco Optimum Yield Diversified Commodity Strategy ETF
                "IAU",  // iShares Gold Trust
                //--- fixed-income
                "VGLT", // Vanguard Long-Term Govt. Bond ETF
                "SHV",  // iShares Short-Term Treasury ETF
    #else
                // the book mentions that CXO is using different ETFs
                // we use these, to simulate back to 2007
            
                //--- equities
                "SPY", // SPDR S&P 500 Trust ETF
                "IWM", // iShares Russell 2000 ETF
                "EFA", // iShares MSCI EAFE ETF
                "EEM", // iShares MSCI Emerging Markets ETF
                //--- hard assets
                "VNQ", // Vanguard Real Estate ETF
                "DBC", // Invesco DB Commodity Index Tracking ETF
                "GLD", // SPDR Gold Shares ETF
                //--- fixed income
                "TLT", // iShares 20+ Year Treasury Bond ETF
                // Cash... substituted by T-Bill, to make strategy work
                "BIL"  // SPDR Bloomberg Barclays 1-3 Month T-Bill ETF
    #endif
            };
        }

        protected override double _momentum(Instrument i)
        {
            // simple 5-month momentum
            return i.Close[0] / i.Close[5 * 21] - 1.0;
        }
    }
    #endregion
    #region Papa Bear - incomplete, instruments need to be extended for longer simulation
    public class Livingston_MuscularPortfolios_PapaBear : Livingston_MuscularPortfolios
    {
        public override string Name { get { return "Papa Bear Strategy"; } }
        public Livingston_MuscularPortfolios_PapaBear()
        {
            _etfMenu = new HashSet<string>()
            {
                // note that some instruments have not been around for the whole
                // simulation period, leading to skewed results

                //--- equities
                "splice:VTV,yahoo:VVIAX",       // Vanguard Value Index ETF
                "splice:VUG,yahoo:VIGAX",       // Vanguard Growth Index ETF
                "splice:VIOV,VBR,yahoo:VSIAX",  // Vanguard S&P Small-Cap 600 Value Index ETF
                "splice:VIOG,VBK,yahoo:VSGAX",  // Vanguard S&P Small-Cap 600 Growth Index ETF
                "splice:VEA,yahoo:VTMGX",       // Vanguard Developed Markets Index ETF
                "splice:VWO,yahoo:VEMAX",       // Vanguard Emerging Market Stock Index ETF
                //--- hard assets
                "splice:VNQ,yahoo:VGSLX",       // Vanguard Real Estate Index ETF
                "splice:PDBC,DBC",              // Invesco Optimum Yield Diversified Commodity Strategy ETF
                "IAU",                          // iShares Gold ETF
                //--- fixed-income
                "splice:EDV,TLT",               // Vanguard Extended Duration ETF
                "splice:VGIT,IEF",              // Vanguard Intermediate-Term Treasury Index ETF
                "splice:VCLT,IGLB,USIG",        // Vanguard Long-Term Corporate Bond Index ETF
                "splice:BNDX,IBND,BWX",         // Vanguard Total International Bond Index ETF
            };
        }

        protected override double _momentum(Instrument i)
        {
            // average momentum over 3, 6, and 12 months
            return (4.0 * (i.Close[0] / i.Close[63] - 1.0)
                + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                + 1.0 * (i.Close[0] / i.Close[252] - 1.0)) / 3.0;
        }
    }
    #endregion
}

//==============================================================================
// end of file