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

/*
Comments from Brian Livingston, 04/18/2020

When backtesting the Muscular Portfolios — or, in fact, any investing 
methodology — I believe it's important to follow a few standards to make the 
results of different strategies comparable:

1. The backtest should begin on Dec. 31 and include complete calendar years 
   thereafter (up through last month'e end), to avoid accusations that we are 
   cherry-picking the dates.
2. As the book states, a backtest must include at least one complete bear-market 
   and bull-market cycle, beginning with the first day of a bear market. Testing 
   any investing strategy only during a bull market doesn't tell us anything, 
   because most things go up during a bull market. The long-term return for 
   investors is not determined by what they make during a bull market but by how 
   much they keep during a bear market.
3. Asset-class reallocation should take place at the close of the last trading 
   day of the month. We only have monthly closes — not daily closes — for asset 
   classes back to Dec. 31, 1972. This data is in the Quant simulator, which was 
   developed by Mebane Faber using information from Global Financial Data (and used 
   in the book). Reallocating on the the last trading day of each month makes a 
   backtest comparable with different strategies that were also backtested using 
   monthly data.
4. Muscular Portfolios — or any asset-rotation formula that uses low-cost 
   ETFs — can only be tested with actual ETFs as far back as Dec. 29, 2006. No ETFs 
   existed prior to mid-2006 for (A) commodities, i.e., DBC, and (B) non-US bonds, 
   i.e., BWX. These two asset classes were very important for Muscular Portfolios 
   to rotate into in the 2008–2009 financial crisis (not to mention the 2020 
   crash). Mutual funds that offered commodities and non-US bonds cannot be used 
   as substitutes, because they had high expense ratios in the 2000s and were 
   actively managed funds that did not track an index.
5. I've extensively researched which ETFs track different indexes over the years. 
   The substitute ETFs shown below closely match the performance of the actual ETFs 
   in the book. This can be verified using the free PerfChart feature of 
   StockCharts.com.
6. The first non-US bond ETF, BWX, did not actually open until Oct. 5, 2007. This 
   was followed in 2013 by Vanguard's superior, USD-hedged BNDX. However, BWX can 
   be omitted from the backtest until October 2007 without harm. The Papa Bear 
   Portfolio would not have held a position in non-US bonds during the last nine 
   months of the roaring 2002–2007 bull market.

The following three snippets are pseudo-code to select the correct tracking ETFs, 
beginning on Dec. 29, 2006, for the Mama Bear Portfolio, Papa Bear Portfolio, and 
Baby Bear Portfolio. The code tests each date during the run and selects the top 
ETFs from whatever list would have been available on that date. This gives us a 
backtest that is based on actual ETFs that investors could have purchased and held 
during the past 13¼ years. (The "greater than or equal to" dates are one day later 
than each new ETF's inception date, since a new ETF may not have had rate-of-change 
data on its opening date.)

When a substitute ETF is swapped out for one of the book's ETFs, the symbol is in 
boldface below to help you see what is going on. I don't know C#, but I'm sure you 
could easily transform this pseudo-code into whatever language you like:

--------------------

PSEUDO-CODE FOR THE MAMA BEAR PORTFOLIO

if      DATE is .ge. 2006-12-29 then LIST == "IAU, VNQ, VWO, SHY, EFA, TLT,  IJR,  IWB,  DBC"
else if DATE is .ge. 2007-01-12 then LIST == "IAU, VNQ, VWO, SHV, EFA, TLT,  IJR,  IWB,  DBC"
else if DATE is .ge. 2007-07-27 then LIST == "IAU, VNQ, VWO, SHV, VEA, TLT,  IJR,  IWB,  DBC"
else if DATE is .ge. 2007-11-25 then LIST == "IAU, VNQ, VWO, SHV, VEA, VGLT, IJR,  IWB,  DBC"
else if DATE is .ge. 2010-09-10 then LIST == "IAU, VNQ, VWO, SHV, VEA, VGLT, VIOO, IWB,  DBC"
else if DATE is .ge. 2010-09-23 then LIST == "IAU, VNQ, VWO, SHV, VEA, VGLT, VIOO, VONE, DBC"
else if DATE is .ge. 2014-11-08 then LIST == "IAU, VNQ, VWO, SHV, VEA, VGLT, VIOO, VONE, PDBC"
end if

 

PSEUDO-CODE FOR THE PAPA BEAR PORTFOLIO

if      DATE is .ge. 2006-12-29 then LIST == "IAU, VNQ, VTV, VUG, VWO, EFA, TLT, IEF,  LQD,  IJS,  IJT,  -,    DBC"
else if DATE is .ge. 2007-07-27 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, TLT, IEF,  LQD,  IJS,  IJT,  -,    DBC"
else if DATE is .ge. 2007-10-06 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, TLT, IEF,  LQD,  IJS,  IJT,  BWX,  DBC"
else if DATE is .ge. 2007-12-07 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, EDV, IEF,  LQD,  IJS,  IJT,  BWX,  DBC"
else if DATE is .ge. 2009-11-24 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, EDV, VGIT, VCLT, IJS,  IJT,  BWX,  DBC"
else if DATE is .ge. 2010-09-10 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, EDV, VGIT, VCLT, VIOV, VIOG, BWX,  DBC"
else if DATE is .ge. 2013-06-05 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, EDV, VGIT, VCLT, VIOV, VIOG, BNDX, DBC"
else if DATE is .ge. 2014-11-08 then LIST == "IAU, VNQ, VTV, VUG, VWO, VEA, EDV, VGIT, VCLT, IJS,  IJT,  BWX,  PDBC"
end if


PSEUDO-CODE FOR THE BABY BEAR PORTFOLIO

if DATE is .ge. 2006-12-29 then LIST == "AGG,VTI"
if DATE is .ge. 2007-04-11 then LIST == "BND,VTI"
end if


--------------------
*/

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Livingston_MuscularPortfolios : AlgorithmPlusGlue
    {
        #region inputs
        protected abstract HashSet<string> ETF_MENU { get; }
        protected abstract double MOMENTUM(Instrument i);
        protected virtual int NUM_PICKS { get => 3; }
        protected virtual OrderType ORDER_TYPE => OrderType.closeThisBar;
        protected virtual string BENCHMARK => Assets.PORTF_60_40;
        protected virtual double REBAL_TRIGGER => 0.20;
        protected virtual bool REBAL_TODAY(double maxOff)
        {
            return SimTime[0].Month != NextSimTime.Month && maxOff > REBAL_TRIGGER;
        }
        #endregion

        #region override public void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
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
                if (REBAL_TODAY(maxOff))
                {
                    foreach (var i in menu.Select(ds => ds.Instrument))
                    {
                        Alloc.Allocation[i] = top3.Contains(i) ? targetPercentage : 0.0;

                        // determine current and target shares per instrument...
                        double targetEquity = (top3.Contains(i) ? targetPercentage : 0.0) * NetAssetValue[0];
                        int targetShares = (int)Math.Floor(targetEquity / i.Close[0]);
                        int currentShares = i.Position;

                        // ... and trade the delta
                        Order newOrder = i.Trade(targetShares - currentShares, ORDER_TYPE);

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
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    if (IsDataSource)
                    {
                        var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                        yield return Bar.NewOHLC(
                            this.GetType().Name, SimTime[0],
                            v, v, v, v, 0);
                    }
                }
            }
            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
    }

    #region Baby Bear
    public class Livingston_MuscularPortfolios_BabyBear : LazyPortfolio
    {
        public override string Name => "Livingston's Baby Bear";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>("VT",   0.50),
            new Tuple<object, double>("splice:AGG,BND", 0.50),
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion
    #region Mama Bear
    // https://muscularportfolios.com/mama-bear/
    public class Livingston_MuscularPortfolios_MamaBear : Livingston_MuscularPortfolios
    {
        public override string Name => "Livingston's Mama Bear";
        protected override HashSet<string> ETF_MENU => new HashSet<string>()
        {
#if true
            // proxies as suggest by Brian Livingston
            // reaching back to December 2006
            // see email snippet above

            //--- equities
            "splice:VONE,IWB", // Vanguard Russell 1000 ETF (US large-cap stocks)
            "splice:VIOO,IJR", // Vanguard Small-Cap 600 ETF (US small-cap stocks)
            "splice:VEA,EFA",  // Vanguard FTSE Developed Markets ETF (developed-market large-cap stocks)
            "VWO",             // Vanguard FTSE Emerging Markets ETF (emerging-market stocks)
            //--- hard assets
            "VNQ",             // Vanguard Real Estate ETF (REITs)
            "splice:PDBC,DBC", // Invesco Optimum Yield Diversified Commodity Strategy ETF (Commodities)
            "IAU",             // iShares Gold Trust (Gold)
            //--- fixed-income
            "splice:VGLT,TLT", // Vanguard Long-Term Govt. Bond ETF (US Treasury bonds, long-term)
            "splice:SHV,SHY",  // iShares Short-Term Treasury ETF (US Treasury bills, 1 to 12 months)
#else
            // the book mentions that CXO is using different ETFs
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
        public override string Name => "Livingston's Papa Bear";
        protected override HashSet<string> ETF_MENU => new HashSet<string>()
        {
            // proxies as suggest by Brian Livingston
            // reaching back to December 2006
            // see email snippet above

            // NOTE: Brian Livingston suggests to ignore BWX prior to its inception.
            //       As our implementation enforces that all instruments exist before
            //       emitting trades, we fill it up with SHY

            //--- equities
            "VTV",                 // Vanguard Value Index ETF
            "VUG",                 // Vanguard Growth Index ETF
            "splice:VIOV,IJS",     // Vanguard S&P Small-Cap 600 Value Index ETF
            "splice:VIOG,IJT",     // Vanguard S&P Small-Cap 600 Growth Index ETF
            "splice:VEA,EFA",      // Vanguard Developed Markets Index ETF
            "VWO",                 // Vanguard Emerging Market Stock Index ETF
            //--- hard assets
            "VNQ",                 // Vanguard Real Estate Index ETF
            "splice:PDBC,DBC",     // Invesco Optimum Yield Diversified Commodity Strategy ETF
            "IAU",                 // iShares Gold ETF
            //--- fixed-income
            "splice:EDV,TLT",      // Vanguard Extended Duration ETF
            "splice:VGIT,IEF",     // Vanguard Intermediate-Term Treasury Index ETF
            "splice:VCLT,LQD",     // Vanguard Long-Term Corporate Bond Index ETF
            "splice:BNDX,BWX,SHY", // Vanguard Total International Bond Index ETF
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