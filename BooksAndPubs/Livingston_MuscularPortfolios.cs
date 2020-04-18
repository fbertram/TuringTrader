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
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Livingston_MuscularPortfolios : SubclassableAlgorithm
    {
        #region inputs
        protected abstract HashSet<string> ETF_MENU { get; }
        protected abstract double MOMENTUM(Instrument i);
        protected virtual int NUM_PICKS { get => 3; }

        protected virtual double REBAL_TRIGGER => 0.20;
        #endregion
        #region internal data
        private readonly string BENCHMARK = Assets.PORTF_60_40;
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Livingston_MuscularPortfolios()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // the book does not deduct commissions

            var menu = AddDataSources(ETF_MENU).ToList();
            var bench = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // calculate momentum w/ algorithm-specific helper function
                var evaluation = Instruments
                    .ToDictionary(
                        i => i,
                        i => MOMENTUM(i));

                // skip, if there are any missing instruments
                if (!HasInstruments(menu) || !HasInstrument(bench))
                    continue;

                // rank, and select top-3 instruments
                var top3 = menu
                    .Select(ds => ds.Instrument)
                    .OrderByDescending(i => evaluation[i])
                    .Take(NUM_PICKS);

                // calculate target percentage and how far we are off
                double targetPercentage = 1.0 / NUM_PICKS;
                double maxOff = menu
                    .Select(ds => ds.Instrument)
                    .Max(i => (top3.Count() > 0 && top3.Contains(i) ? 1.0 : 0.0)
                        * Math.Abs(i.Position * i.Close[0] / NetAssetValue[0] - targetPercentage) / targetPercentage);

                // rebalance once per month, and only if we need adjustments exceeding 20%
                if (SimTime[0].Month != NextSimTime.Month
                    && maxOff > REBAL_TRIGGER)
                {
                    _alloc.LastUpdate = SimTime[0];

                    foreach (var i in menu.Select(ds => ds.Instrument))
                    {
                        _alloc.Allocation[i] = top3.Contains(i) ? targetPercentage : 0.0;

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

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, ETF_MENU.Select(nick => FindInstrument(nick)));

                    if (IsSubclassed) AddSubclassedBar();
                }
            }
            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
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
    // https://muscularportfolios.com/mama-bear/
    public class Livingston_MuscularPortfolios_MamaBear : Livingston_MuscularPortfolios
    {
        public override string Name => "Livingston's Mama Bear";
        protected override HashSet<string> ETF_MENU => new HashSet<string>()
        {
#if true
            // note that some instruments have not been around
            // until 2014, making this hard to simulate

            //--- equities
            "splice:VONE,$RUITR", // Vanguard Russell 1000 ETF (US large-cap stocks)
            "splice:VIOO,$SMLTR", // Vanguard Small-Cap 600 ETF (US small-cap stocks)
            "VEA",  // Vanguard FTSE Developed Markets ETF (developed-market large-cap stocks)
            "VWO",  // Vanguard FTSE Emerging Markets ETF (emerging-market stocks)
            //--- hard assets
            "VNQ",  // Vanguard Real Estate ETF (REITs)
            "splice:PDBC,DBC", // Invesco Optimum Yield Diversified Commodity Strategy ETF (Commodities)
            "IAU",  // iShares Gold Trust (Gold)
            //--- fixed-income
            "splice:VGLT,TLT", // Vanguard Long-Term Govt. Bond ETF (US Treasury bonds, long-term)
            "SHV",  // iShares Short-Term Treasury ETF (US Treasury bills, 1 to 12 months)
#else
            // the book mentions that CXO is using different ETFs
            // we use these, to simulate back to 2007
            // see page 104
            
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
        protected override double MOMENTUM(Instrument i)
        {
            // simple 5-month momentum

            // Note: Livingston calculates momentum based on daily bars,
            // see footnote here: https://muscularportfolios.com/mama-bear/
            // as of 04/17/2020, our momentum values match his website exactly
            return i.Close[0] / i.Close[105] - 1.0;
        }
    }
    #endregion
    #region Papa Bear
    // see https://muscularportfolios.com/papa-bear/
    public class Livingston_MuscularPortfolios_PapaBear : Livingston_MuscularPortfolios
    {
        public override string Name => "Livingston's Papa Bear Strategy";
        protected override HashSet<string> ETF_MENU => new HashSet<string>()
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

        protected override double MOMENTUM(Instrument i)
        {
            // average momentum over 3, 6, and 12 months

            // NOTE: Livingston is calculating momentum on daily bars,
            // see footnote here: https://muscularportfolios.com/papa-bear/
            // as of 04/17/2020, our calculation matches Livingston's exactly
            return ((i.Close[0] / i.Close[63] - 1.0)
                + (i.Close[0] / i.Close[126] - 1.0)
                + (i.Close[0] / i.Close[252] - 1.0)) / 3.0;
        }
    }
    #endregion
}

//==============================================================================
// end of file