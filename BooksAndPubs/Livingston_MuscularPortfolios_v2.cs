//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Livingston_MuscularPortfolios
// Description: 'Mama Bear' and 'Papa Bear' strategies, as published in
//              Brian Livingston's book 'Muscular Portfolios'.
//               https://muscularportfolios.com/
// History:     2018xii14, FUB, created
//              2022x29, FUB, ported to v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    public abstract class Livingston_MuscularPortfolios : Algorithm
    {
        #region inputs
        protected abstract HashSet<string> ETF_MENU { get; }
        protected abstract double MOMENTUM(string ticker);
        protected virtual int NUM_PICKS { get => 3; }
        protected virtual string BENCH => Benchmark.PORTFOLIO_60_40;
        protected virtual bool IS_REBAL_DAY => SimDate.Month != NextSimDate.Month || IsFirstBar;
        protected virtual double MAX_ALLOC_DEVIATION => 0.20;
        protected virtual OrderType ORDER_TYPE => OrderType.closeThisBar;
        #endregion
        #region strategy logic
        public override void Run()
        {
            //========== initialization ==========

            StartDate = DateTime.Parse("01/01/2007");
            EndDate = DateTime.Now;
            WarmupPeriod = TimeSpan.FromDays(365);

            //========== simulation loop ==========

            SimLoop(() =>
            {
                if (IS_REBAL_DAY)
                {
                    // rank, and select top-3 instruments
                    var top3 = ETF_MENU
                        .OrderByDescending(i => MOMENTUM(i))
                        .Take(NUM_PICKS);

                    // calculate asset weights
                    var weights = ETF_MENU
                        .ToDictionary(
                            ticker => ticker,
                            ticker => top3.Contains(ticker) ? 1.0 / NUM_PICKS : 0.0);

                    // calculate max deviation from target allocation
                    var allocDeviation = ETF_MENU
                        .Select(ticker => Math.Abs(weights[ticker] - Asset(ticker).Position))
                        .Max();

                    // only rebalance, if necessary
                    if (allocDeviation > MAX_ALLOC_DEVIATION)
                    {
                        foreach (var kv in weights)
                            Asset(kv.Key).Allocate(kv.Value, ORDER_TYPE);
                    }
                }

                if (!IsOptimizing)
                {
                    // equity chart
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot(Asset(BENCH).Description, Asset(BENCH).Close[0]);

                    // asset momentum
                    Plotter.SelectChart("Asset Momentum", "Date");
                    Plotter.SetX(SimDate);
                    foreach (var ticker in ETF_MENU)
                        Plotter.Plot(Asset(ticker).Description, MOMENTUM(ticker));
                }
            });

            //========== post processing ==========

            if (!IsOptimizing)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
        #endregion
    }

    #region Baby Bear
#if false
    public class Livingston_MuscularPortfolios_BabyBear : LazyPortfolio
    {
        public override string Name => "Livingston's Baby Bear";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            new Tuple<string, double>(ETF.VT,  0.50),
            new Tuple<string, double>(ETF.AGG, 0.50),
        };
        public override string BENCH => Benchmark.PORTFOLIO_60_40;
    }
#endif
    #endregion
    #region Mama Bear
    // https://muscularportfolios.com/mama-bear/
    public class Livingston_MuscularPortfolios_MamaBear : Livingston_MuscularPortfolios
    {
        public override string Name => "Livingston's Mama Bear";
        protected override HashSet<string> ETF_MENU => new HashSet<string>()
        {
#if true
            //--- equities
            ETF.VONE, // Vanguard Russell 1000 ETF (US large-cap stocks)
            ETF.VIOO, // Vanguard Small-Cap 600 ETF (US small-cap stocks)
            ETF.VEA,  // Vanguard FTSE Developed Markets ETF (developed-market large-cap stocks)
            ETF.VWO,  // Vanguard FTSE Emerging Markets ETF (emerging-market stocks)
            //--- hard assets
            ETF.VNQ,  // Vanguard Real Estate ETF (REITs)
            ETF.PDBC, // Invesco Optimum Yield Diversified Commodity Strategy ETF (Commodities)
            ETF.IAU,  // iShares Gold Trust (Gold)
            //--- fixed-income
            ETF.VGLT, // Vanguard Long-Term Govt. Bond ETF (US Treasury bonds, long-term)
            ETF.SHV,  // Shares Short-Term Treasury ETF (US Treasury bills, 1 to 12 months)
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
        protected override double MOMENTUM(string name)
        {
            // simple 5-month momentum

            // Note: Livingston calculates momentum based on daily bars,
            // see footnote here: https://muscularportfolios.com/mama-bear/
            var closingPrices = Asset(name).Close;
            return closingPrices[0] / closingPrices[105] - 1.0;
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
            //--- equities
            ETF.VTV,  // Vanguard Value Index ETF
            ETF.VUG,  // Vanguard Growth Index ETF
            ETF.VIOV, // Vanguard S&P Small-Cap 600 Value Index ETF
            ETF.VIOG, // Vanguard S&P Small-Cap 600 Growth Index ETF
            ETF.VEA,  // Vanguard Developed Markets Index ETF
            ETF.VWO,  // Vanguard Emerging Market Stock Index ETF
            //--- hard assets
            ETF.VNQ,  // Vanguard Real Estate Index ETF
            ETF.PDBC, // Invesco Optimum Yield Diversified Commodity Strategy ETF
            ETF.IAU,  // iShares Gold ETF
            //--- fixed-income
            ETF.EDV,  // Vanguard Extended Duration ETF
            ETF.VGIT, // Vanguard Intermediate-Term Treasury Index ETF
            ETF.VCLT, // Vanguard Long-Term Corporate Bond Index ETF
            ETF.BNDX, // Vanguard Total International Bond Index ETF
        };

        protected override double MOMENTUM(string ticker)
        {
            // average momentum over 3, 6, and 12 months

            // NOTE: Livingston is calculating momentum on daily bars,
            // see footnote here: https://muscularportfolios.com/papa-bear/
            var closingPrices = Asset(ticker).Close;
            var mom3mo = closingPrices[0] / closingPrices[63] - 1.0;
            var mom6mo = closingPrices[0] / closingPrices[126] - 1.0;
            var mom12mo = closingPrices[0] / closingPrices[252] - 1.0;
            return (mom3mo + mom6mo + mom12mo) / 3.0;
        }
    }
    #endregion
}

//==============================================================================
// end of file