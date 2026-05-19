//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        LazyPortfolios_v2
// Description: Simple benchmarking portfolios.
// History:     2019xii04, EFB, created
//              2023ii09, EFB, refactored for v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2025, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
using System.Linq;
using TuringTrader.GlueV2;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region LazyPortfolio core
    public abstract class LazyPortfolio : Algorithm
    {
        #region inputs
        public virtual HashSet<Tuple<object, double>> ALLOCATION { get; set; }
        public virtual object BENCH { get; set; } = Benchmark.PORTFOLIO_60_40;
        public virtual List<object> BENCHES { get; set; } = null;
        public virtual bool IS_TRADING_DAY => IsFirstBar || SimDate.Month != NextSimDate.Month; // end of month
        public virtual double MGMT_FEE { get; set; } = 0.0;
        public virtual double CAP_GAINS_TAX { get; set; } = 0.0;
        #endregion
        #region strategy logic
        public override void Run()
        {
            //========== initialization ==========

            StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(0);
            ((Account_Default)Account).Friction = 0.0; // lazy portfolios typically w/o commission

            // FIXME: ALLOCATION is not a property. As a consequence, it is evaluated
            //        every time we use it, which is problematic for strategy blends.
            //        To fix this, we introduce a copy here.
            var _ALLOCATION = ALLOCATION;
            var autoAlloc = _ALLOCATION.Sum(a => a.Item2) == 0.0;

            //========== simulation loop ==========

            double accruedFeeDollars = 0.0;
            double navAtEndOfYear = 1000.00;
            SimLoop(() =>
            {
                // rebalance allocation
                if (IS_TRADING_DAY)
                {
                    foreach (var asset in _ALLOCATION)
                        Asset(asset.Item1).Allocate(
                            autoAlloc ? 1.0 / _ALLOCATION.Count : asset.Item2,
                            OrderType.openNextBar);
                }

                // deduct management fees
                accruedFeeDollars += NetAssetValue * MGMT_FEE / 252.0;
                if (MGMT_FEE > 0.0 && SimDate.Month != NextSimDate.Month)
                {
                    ((Account_Default)Account).Deposit(-accruedFeeDollars / NetAssetValue);
                    accruedFeeDollars = 0.0;
                }

                // deduct capital gains tax
                if (CAP_GAINS_TAX > 0.0 && SimDate.Year != NextSimDate.Year)
                {
                    // this is very crude. We do not distinguish between
                    // realized and unrealized gains, we do not know about
                    // long-term vs. short-term gains, and we can't treat
                    // interest payments and dividends differently.
                    // Nonetheless, this is a helpful tool.
                    var gains = NetAssetValue - navAtEndOfYear;
                    var tax = Math.Max(0.0, gains * CAP_GAINS_TAX);
                    navAtEndOfYear = NetAssetValue - tax;
                    ((Account_Default)Account).Deposit(-tax / NetAssetValue);
                }

                // plotter output
                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    if (BENCHES == null)
                        Plotter.Plot(Asset(BENCH).Description, Asset(BENCH).Close[0]);
                    else
                        foreach (var bench in BENCHES)
                            Plotter.Plot(Asset(bench).Description, Asset(bench).Close[0]);

                    Plotter.SelectChart("Asset 12-Months Rolling Returns", "Date");
                    Plotter.SetX(SimDate);
                    foreach (var alloc in _ALLOCATION)
                    {
                        var asset = Asset(alloc.Item1);
                        Plotter.Plot(asset.Description, 100.0 * (asset.Close[0] / asset.Close[252] - 1.0));
                    }
                }
            });

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
        #endregion
    }
    #endregion

    #region 60/40 benchmark
    public class Benchmark_60_40 : LazyPortfolio
    {
        public override string Name => "Vanilla 60/40";
        public override HashSet<Tuple<object, double>> ALLOCATION { get; set; } = new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(ETF.SPY, 0.60),
            new Tuple<object, double>(ETF.AGG, 0.40),
        };
        public override string BENCH => MarketIndex.SPXTR;
    }
    #endregion
    #region Tony Robbins' All-Seasons Portfolio
    public class Robbins_AllSeasonsPortfolio : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio";
        public override HashSet<Tuple<object, double>> ALLOCATION { get; set; } = new HashSet<Tuple<object, double>>
        {
            // See Tony Robbins "Money, Master the Game", Chapter 5
            new Tuple<object, double>(ETF.SPY, 0.30),  // 30% S&P 500
            new Tuple<object, double>(ETF.IEF, 0.15),  // 15% 7-10yr Treasuries
            new Tuple<object, double>(ETF.TLT, 0.40),  // 40% 20-25yr Treasuries
            new Tuple<object, double>(ETF.GLD, 0.075), // 7.5% Gold
            new Tuple<object, double>(ETF.DBC, 0.075), // 7.5% Commodities
        };
        public override string BENCH => Benchmark.PORTFOLIO_60_40;
    }

#if false
    public class Robbins_AllSeasonsPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio (2x Leverage)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // see https://www.optimizedportfolio.com/all-weather-portfolio/
            Tuple.Create(ETF.SSO, 0.30),  // 30% 2x S&P 500 (SSO)
            Tuple.Create(ETF.UBT, 0.40),  // 40% 2x 20-25yr Treasuries (UBT)
            Tuple.Create(ETF.UST, 0.15),  // 15% 2x 7-10yr Treasuries (UST)
            Tuple.Create(ETF.UGL, 0.075), // 7.5% 2x Gold (UGL)
            Tuple.Create(ETF.DIG, 0.075), // 7.5% 2x Oil & Gas (DIG)
        };
        public override string BENCH => Assets.PORTF_60_40;
        //public override DateTime START_TIME => DateTime.Parse("01/01/1900", CultureInfo.InvariantCulture);
    }
#endif
#if false
    public class Robbins_AllSeasonsPortfolio_3x : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio (2.89x Leverage)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // replacing commodities w/ utilities
            // see https://www.optimizedportfolio.com/all-weather-portfolio/
            Tuple.Create(ETF.UPRO, 0.289),  // 30% 3x S&P 500 (UPRO)
            Tuple.Create(ETF.TMF,  0.385),  // 40% 3x 20-25yr Treasuries (TMF)
            Tuple.Create(ETF.TYD,  0.145),  // 15% 3x 7-10yr Treasuries (TYD)
            Tuple.Create(ETF.UGL,  0.073),  // 7.5% 2x Gold (UGL)
            Tuple.Create(ETF.UTSL, 0.108),  // 7.5% 3x Utilities (UTSL)
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
    #endregion
    #region Harry Browne's Permanent Portfolio
    public class Browne_PermanentPortfolio : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio";
        public override HashSet<Tuple<object, double>> ALLOCATION { get; set; } = new HashSet<Tuple<object, double>>
        {
            // See Harry Browne, Fail Safe Investing
            new Tuple<object, double>(ETF.SPY, 0.25),  // 25% S&P 500
            new Tuple<object, double>(ETF.TLT, 0.25),  // 25% 20-25yr Treasuries
            new Tuple<object, double>(ETF.SHY, 0.25),  // 25% Short-Term Treasuries
            new Tuple<object, double>(ETF.GLD, 0.25),  // 25% Gold
        };
        public override string BENCH => Benchmark.PORTFOLIO_60_40;
    }
#if false
    // NOTE: 3x Gold not available after summer 2020
    public class Browne_PermanentPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio (2x leveraged)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See https://www.optimizedportfolio.com/permanent-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_3X,   0.167),  // 25% S&P 500
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_3X, 0.167),  // 25% 20-25yr Treasuries
            Tuple.Create(Assets.GOLD_3X,               0.166),  // 25% Gold
            Tuple.Create(Assets.BONDS_US_TREAS_3Y,     0.500),  // 25% Short-Term Treasuries
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
#if false
    public class Browne_PermanentPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio (1.82x leveraged)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See https://www.optimizedportfolio.com/permanent-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_3X,   0.15),  // 25% S&P 500 (UPRO)
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_3X, 0.15),  // 25% 20-25yr Treasuries (TMF)
            Tuple.Create(Assets.GOLD_2X,               0.22),  // 25% Gold (UGL)
            Tuple.Create(Assets.BONDS_US_TREAS_3Y,     0.48),  // 25% Short-Term Treasuries (BIL)
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
    #endregion
}

//==============================================================================
// end of file