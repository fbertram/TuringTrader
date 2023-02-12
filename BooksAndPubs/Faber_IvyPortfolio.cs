//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Faber_IvyPortfolio
// Description: Variuous strategies as published in Mebane Faber's book
//              'The Ivy Portfolio' and
//              'A Quantitative Approach to Tactical Asset Allocation'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=962461
//              'Relative Strength Strategies for Investing'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1585517
// History:     2018xii14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
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

// NOTE
// it is not exactly clear, how Faber imagined this strategy implementation.
// we especially have the following open questions:
// - how are 1, 3, 6-month momentum calculated? daily or monthly bars?
// - how is the 10-month SMA calculated? on daily or monthly bars?
// - how is the momentum score calculated? simple average of 1, 3, 6-month, 
//   or are they annualized first?

// further implementation details taken from
// http://www.scottsinvestments.com/2012/01/27/new-ivy-portfolio-tool/
// https://docs.google.com/spreadsheets/d/1DbLtigPIUy8-dgA_v9FN7rVM2_5BD_H2SMgHoYCW8Sg/edit#gid=971644476

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region Ivy Portfolio Core
    public abstract class Faber_IvyPortfolio_Core : AlgorithmPlusGlue
    {
        #region inputs
        protected struct AssetClass
        {
            public double weight;
            public int numpicks;
            public List<string> assets;
        }
        protected abstract HashSet<AssetClass> ASSET_CLASSES { get; }

        protected abstract double SCORING_FUNC(Instrument i);
        #endregion
        #region internal data
        private readonly string BENCHMARK = Indices.PORTF_60_40;
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Faber does not consider commissions

            var ASSETS = ASSET_CLASSES
                .SelectMany(c => c.assets)
                .Distinct()
                .ToList();

            var benchmark = AddDataSource(BENCHMARK);
            var assets = AddDataSources(ASSETS);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate instrument momentum for all known instruments,
                // we need to make sure to evaluate every instrument only once!
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i,
                        i => SCORING_FUNC(i));

                // skip if there are any missing instruments,
                // we want to make sure our strategy has all instruments available
                if (!HasInstruments(assets) || !HasInstrument(benchmark))
                    continue;

                // execute trades once per month
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = assets
                        .ToDictionary(ds => ds.Instrument, ds => 0.0);

                    // loop through all asset classes and accumulate asset weights
                    foreach (AssetClass assetClass in ASSET_CLASSES)
                    {
                        List<Instrument> assetClassInstruments = assetClass.assets
                            .Select(n => FindInstrument(n))
                            .ToList();

                        var bestInstruments = assetClassInstruments
                            .OrderByDescending(i => instrumentMomentum[i])
                            .Take(assetClass.numpicks);

                        foreach (Instrument bestInstrument in bestInstruments)
                            instrumentWeights[bestInstrument] += assetClass.weight / assetClass.numpicks;
                    }

                    double totalWeight = ASSET_CLASSES
                        .Sum(a => a.weight);
                    double equityUnit = NetAssetValue[0] / totalWeight;

                    // create orders
                    string message = string.Format("{0:MM/dd/yyyy}: ", SimTime[0]);
                    foreach (var i in instrumentWeights.Keys)
                    {
                        Alloc.Allocation[i] = instrumentWeights[i] / totalWeight;
                        message += string.Format("{0} = {1:P2}, ", i.Symbol, instrumentWeights[i]);

                        int targetShares = (int)Math.Floor(instrumentWeights[i] * equityUnit / i.Close[0]);
                        int currentShares = i.Position;
                        Order newOrder = i.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }

                    if (TradingDays > 0 && !IsOptimizing && (EndTime - SimTime[0]).TotalDays < 63)
                        Output.WriteLine(message);
                }

                // plotter output
                if (TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, ASSETS.Select(nick => FindInstrument(nick)));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);
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
    #endregion
    #region Ivy Timing Portfolio Core
    public abstract class Faber_IvyTiming_Core : Faber_IvyPortfolio_Core
    {
        private Dictionary<Instrument, TimeSeries<double>> _monthlyBars = new Dictionary<Instrument, TimeSeries<double>>();

        protected abstract string SAFE_INSTRUMENT { get; }
        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == SAFE_INSTRUMENT)
                return 0.0;

#if true
            if (!_monthlyBars.ContainsKey(i))
                _monthlyBars[i] = new TimeSeries<double>();

            if (SimTime[0].Month != NextSimTime.Month)
                _monthlyBars[i].Value = i.Close[0];

            return (_monthlyBars[i].BarsAvailable > 12 && _monthlyBars[i][0] > _monthlyBars[i].SMA(10)[0])
                ? 1.0
                : -1.0;
#else
            return i.Close[0] > i.Close.SMA(210)[0]
                ? 1.0
                : -1.0;
#endif
        }
    }
    #endregion
    #region Ivy Rotation Portfolio Core
    public abstract class Faber_IvyRotation_Core : Faber_IvyPortfolio_Core
    {
        private Dictionary<Instrument, TimeSeries<double>> _monthlyBars = new Dictionary<Instrument, TimeSeries<double>>();

        protected abstract string SAFE_INSTRUMENT { get; }
        protected override double SCORING_FUNC(Instrument i)
        {
            if (i.Nickname == SAFE_INSTRUMENT)
                return 0.0;

#if true
            if (!_monthlyBars.ContainsKey(i))
                _monthlyBars[i] = new TimeSeries<double>();

            if (SimTime[0].Month != NextSimTime.Month)
                _monthlyBars[i].Value = i.Close[0];

            return (_monthlyBars[i].BarsAvailable > 12 && _monthlyBars[i][0] > _monthlyBars[i].SMA(10)[0])
                ? (1.0 * (_monthlyBars[i][0] / _monthlyBars[i][3] - 1.0)
                        + 1.0 * (_monthlyBars[i][0] / _monthlyBars[i][6] - 1.0)
                        + 1.0 * (_monthlyBars[i][0] / _monthlyBars[i][12] - 1.0))
                    / 3.0
                : -1.0;
#else
            return i.Close[0] > i.Close.SMA(210)[0]
                ? (1.0 * (i.Close[0] / i.Close[63] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[126] - 1.0)
                        + 1.0 * (i.Close[0] / i.Close[252] - 1.0))
                    / 3.0
                : -1.0;
#endif
        }
    }
    #endregion

    #region Ivy 5 Timing
    public class Faber_IvyPortfolio_5_Timing : Faber_IvyTiming_Core
    {
        public override string Name => "Faber's Ivy 5 Timing";

        protected override string SAFE_INSTRUMENT => Assets.BIL; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                Assets.VTI, // Vanguard Total Stock Market ETF
                SAFE_INSTRUMENT
            } },
            //--- world equity
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                Assets.VEU, // Vanguard FTSE All-World ex-US ETF
                SAFE_INSTRUMENT
            } },
            //--- credit
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                Assets.BND, // Vanguard Total Bond Market ETF
                SAFE_INSTRUMENT
            } },
            //--- real estate
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                Assets.VNQ, // Vanguard REIT Index ETF
                SAFE_INSTRUMENT
            } },
            //--- economic stress
            new AssetClass { weight = 0.20, numpicks = 1, assets = new List<string> {
                Assets.DBC, // PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT
            } },
        };
    }
    #endregion
    #region Ivy 5 Rotation
    public class Faber_IvyPortfolio_5_Rotation : Faber_IvyRotation_Core
    {
        public override string Name => "Faber's Ivy 5 Rotation";

        protected override string SAFE_INSTRUMENT => Assets.BIL; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                //--- domestic equity
                Assets.VTI, // Vanguard Total Stock Market ETF
                //--- world equity
                Assets.VEU, // Vanguard FTSE All-World ex-US ETF
                //--- credit
                Assets.BND, // Vanguard Total Bond Market ETF
                //--- real estate
                Assets.VNQ, // Vanguard REIT Index ETF
                //--- economic stress
                Assets.DBC, // PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT
            } },
        };
    }
    #endregion
    #region Ivy 10 Timing
    public class Faber_IvyPortfolio_10_Timing : Faber_IvyTiming_Core
    {
        public override string Name => "Faber's Ivy 10 Timing";

        protected override string SAFE_INSTRUMENT => "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- domestic equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VB",  // Vanguard Small Cap ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VTI", // Vanguard Total Stock Market ETF
                SAFE_INSTRUMENT
            } },
            //--- world equity
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VWO", // Vanguard Emerging Markets Stock ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VEU", // Vanguard FTSE All-World ex-US ETF
                SAFE_INSTRUMENT
            } },
            //--- credit
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "BND", // Vanguard Total Bond Market ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "TIP", // iShares Barclays TIPS Bond
                SAFE_INSTRUMENT
            } },
            //--- real estate
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "RWX", // SPDR DJ International Real Estate ETF
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "VNQ", // Vanguard REIT Index ETF
                SAFE_INSTRUMENT
            } },
            //--- economic stress
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "DBC", // 1PowerShares DB Commodity Index Tracking
                SAFE_INSTRUMENT
            } },
            new AssetClass { weight = 0.10, numpicks = 1, assets = new List<string> {
                "GSG", // S&P GSCI(R) Commodity-Indexed Trust
                SAFE_INSTRUMENT,
            } },
        };
    }
    #endregion
    #region Ivy 10 Rotation
    public class Faber_IvyPortfolio_10_Rotation : Faber_IvyRotation_Core
    {
        public override string Name => "Faber's Ivy 10 Rotation";

        protected override string SAFE_INSTRUMENT => "BIL"; // SPDR Barclays 1-3 Month T-Bill ETF

        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 5, assets = new List<string> {
                //--- domestic equity
                "VB",  // Vanguard Small Cap ETF
                "VTI", // Vanguard Total Stock Market ETF
                //--- world equity
                "VWO", // Vanguard Emerging Markets Stock ETF
                "VEU", // Vanguard FTSE All-World ex-US ETF
                //--- credit
                "BND", // Vanguard Total Bond Market ETF
                "TIP", // iShares Barclays TIPS Bond
                //--- real estate
                "RWX", // SPDR DJ International Real Estate ETF
                "VNQ", // Vanguard REIT Index ETF
                //--- economic stress
                "DBC", // PowerShares DB Commodity Index Tracking
                "GSG", // S&P GSCI(R) Commodity-Indexed Trust
                //---
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
            } },
        };
    }
    #endregion

    #region Ned Davis Research: Three-Way Model
    // from Greg Kuhn:
    // Ned Davis three way model.  It is somewhat similar to the permeant
    // portfolio, at least in asset choice.  The rules are invest in Gold,
    // Stocks or bonds (GLD, TLT, VTI) if the assets'
    // 3 month moving average > 10 month moving average.
    // If only one asset satisfies the condition, invest all funds in that
    // asset (if 2 satisfy the condition then funds are allocated evenly
    // between these assets).
    //
    // see https://mebfaber.com/2015/06/16/three-way-model/
    // This study was completed by Ned Davis Research (http://www.ndr.com/),
    // and could not be more simple. Three asset classes: Stocks, bonds, gold.
    // Invest equally in whatever is going up(defined as 3 month SMA > 10 month SMA).
    // That’s it.Thumps the stock market with less risk.  
   
    public class Davis_ThreeWayModel : AlgorithmPlusGlue
    {
        public override string Name => "Davis' Three-Way System";

        private readonly string BENCHMARK = Indices.PORTF_60_40;

        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME - TimeSpan.FromDays(5);

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Faber does not consider commissions

            var assets = new List<object>
                {
                    Assets.GLD,
                    Assets.TLT,
                    Assets.SPY, // should be VTI?
                }
                .Select(ticker => AddDataSource(ticker))
                .ToList();

            var bench = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                var instruments = assets
                    .Select(ds => ds.Instrument)
                    .ToList();

                if (!HasInstruments(assets))
                    continue;

                var indicators = assets
                    .ToDictionary(
                        ds => ds,
                        ds => new
                        {
                            mom3mo = ds.Instrument.Close.SMA(63),
                            mom10mo = ds.Instrument.Close.SMA(210),
                        });

                if (SimTime[0].Month != NextSimTime.Month)
                {
                    var goingUp = assets
                        .Where(ds => indicators[ds].mom3mo[0] > indicators[ds].mom10mo[0])
                        .ToList();
                    var weight = 1.0 / Math.Max(1.0, goingUp.Count());

                    var weights = Instruments
                        .ToDictionary(
                            i => i,
                            i => 0.0);
                    foreach (var ds in goingUp)
                        weights[ds.Instrument] = weight;

                    foreach (var i in Instruments)
                    { 
                        var shares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(shares - i.Position);
                    }
                }

                // plotter output
                if (TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, assets.Select(ds => ds.Instrument));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    foreach (var ds in assets)
                    {
                        _plotter.SelectChart("Asset " + ds.Instrument.Symbol, "Date");
                        _plotter.SetX(SimTime[0]);
                        _plotter.Plot(ds.Instrument.Symbol, ds.Instrument.Close[0]);
                        _plotter.Plot("3-months SMA", indicators[ds].mom3mo[0]);
                        _plotter.Plot("10-months SMA", indicators[ds].mom10mo[0]);
                    }
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                //_plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
    }
    #endregion

    #region sector rotation strategy
    // From Meb Faber - Relative Strength Strategies for Investing
    // see https://papers.ssrn.com/sol3/papers.cfm?abstract_id=1585517
    // 
    // Also discussed here: https://school.stockcharts.com/doku.php?id=trading_strategies:sector_rotation_roc
    // Quote:
    // The strategy described here is based on the findings in Faber's white
    // paper. First, the strategy is based on monthly data and the portfolio
    // is rebalanced once per month. Chartists can use the last day of the
    // month, the first day of the month or a set date every month. The
    // strategy is long when the S&P 500 is above its 10-month simple moving
    // average and out of the market when the S&P 500 is below its 10-month
    // SMA. This basic timing technique ensures that investors are out of
    // the market during extended downtrends and in the market during extended
    // uptrends. Such a strategy would have avoided the 2001-2002 bear
    // market and the gut-wrenching decline in 2008.
    //
    // In his backtest, Faber used the 10 sector/industry groups from the
    // French-Fama CRSP Data Library. These include Consumer Non-Durables,
    // Consumer Durables, Manufacturing, Energy, Technology, Telecommunications,
    // Shops, Health, Utilities and Other. “Other” includes Mines, Construction,
    // Transportation, Hotels, Business Services, Entertainment, and Finance.
    // Instead of searching for individual ETFs to match these groups, this
    // strategy will simply use the nine sector SPDRs.
    //
    // The next step is to choose the performance interval. Chartists can
    // choose anything from one to twelve months. One month may be a little
    // short and cause excessive rebalancing. Twelve months may be a bit long
    // and miss too much of the move. As a compromise, this example will use
    // three months and define performance with the three-month Rate-of-Change,
    // which is the percentage gain over a three-month period.
    //
    // Chartists must then decide how much capital to allocate to each sector and
    // to the strategy as a whole.Chartists could buy the top three sectors and
    // allocate equal amounts to all three (33%). Alternatively, investors could
    // implement a weighted strategy by investing the most in the top sector and
    // lower amounts in the subsequent sectors.
    // * Buy Signal: When the S&P 500 is above its 10-month simple moving average,
    //   buy the sectors with the biggest gains over a three-month timeframe.
    // * Sell Signal: Exit all positions when the S&P 500 moves below its 10-month
    //   simple moving average on a monthly closing basis.
    // * Rebalance: Once per month, sell sectors that fall out of the top tier (three)
    //   and buy the sectors that move into the top tier (three).

    public class Faber_Sector_Rotation : Faber_IvyRotation_Core
    {
        // FIXME: Faber's paper describes an additional market-regime filter,
        // which we didn't implement here. Instead, we have introduced the
        // SAFE_INSTRUMENT, which the strategy falls back to when all
        // sectors indicate negative momentum. While the regime filter is
        // probably better, it will not make a huge difference to the
        // overall behavior of the strategy.

        public override string Name => "Faber's Sector Rotation";

        protected override string SAFE_INSTRUMENT => Assets.BIL; // SPDR Barclays 1-3 Month T-Bill ETF
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass { weight = 1.00, numpicks = 3, assets = new List<string> {
                Assets.XLB,
                //Assets.XLC,
                Assets.XLE,
                Assets.XLF,
                Assets.XLI,
                Assets.XLK,
                Assets.XLP,
                //Assets.XLRE,
                //Assets.XLU,
                Assets.XLV,
                Assets.XLY,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT,
                SAFE_INSTRUMENT
            } },
        };
    }
    #endregion
}

//==============================================================================
// end of file