//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Antonacci_DualMomentumInvesting
// Description: Strategy, as published in Gary Antonacci's book
//              'Dual Momentum Investing'.
//              http://www.optimalmomentum.com/
// History:     2018xi22, FUB, created
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
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Antonacci_DualMomentumInvesting_Core : SubclassableAlgorithm
    {
        public override string Name => "Antonacci's Dual Momentum";

        #region inputs
        protected class AssetClass
        {
            public double weight = 1.0;
            public bool setSafeInstrument = false;
            public HashSet<string> assets;
        }
        /// <summary>
        /// hash set of asset classes
        /// </summary>
        protected abstract HashSet<AssetClass> ASSET_CLASSES { get; }
        /// <summary>
        /// benchmark to measure absolute momentum
        /// </summary>
        protected virtual string ABS_MOMENTUM => "BIL"; // SPDR Bloomberg Barclays 1-3 Month T-Bill ETF
        /// <summary>
        /// safe instrument
        /// </summary>
        protected virtual string SAFE_INSTR => "AGG"; // iShares Core U.S. Aggregate Bond ETF
        /// <summary>
        /// charting benchmark
        /// </summary>
        protected virtual string BENCHMARK => Globals.BALANCED_PORTFOLIO;
        /// <summary>
        /// momentum calculation
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected virtual double MOMENTUM(Instrument i) => i.Close[0] / i.Close[252] - 1.0;
        #endregion
        #region internal data
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Antonacci_DualMomentumInvesting_Core()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // it is unclear, if Antonacci considers commissions

            // assets we can trade
            List<string> assets = ASSET_CLASSES
                .SelectMany(c => c.assets)
                .Distinct()
                .Where(nick => nick != ABS_MOMENTUM)
                .ToList();
            assets.Add(SAFE_INSTR);

            AddDataSources(assets);
            AddDataSource(ABS_MOMENTUM);
            AddDataSource(BENCHMARK);

            double totalWeights = ASSET_CLASSES.Sum(a => a.weight);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate momentum for all known instruments
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i, i => MOMENTUM(i));

                // skip if there are any missing instruments
                if (!HasInstruments(assets) || !HasInstrument(BENCHMARK) || !HasInstrument(ABS_MOMENTUM))
                    continue;

                // execute trades once per month
                // CAUTION: do not calculate indicators within this block!
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                    // loop through all asset classes, and find the top-ranked one
                    Instrument safeInstrument = FindInstrument(SAFE_INSTR);
                    foreach (AssetClass assetClass in ASSET_CLASSES)
                    {
                        // find the instrument with the highest momentum
                        // in each asset class
                        var bestInstrument = assetClass.assets
                            .Select(nick =>FindInstrument(nick))
                            .OrderByDescending(i => instrumentMomentum[i])
                            .Take(1)
                            .First();

                        // sum up the weights (because safe instrument is duplicated)
                        instrumentWeights[bestInstrument] += assetClass.weight / totalWeights;

                        if (assetClass.setSafeInstrument)
                            safeInstrument = bestInstrument;
                    }

                    // if momentum of any instrument drops below that of a T-Bill,
                    // we use the safe instrument instead
                    // therefore, we swap T-Bills for the safe instrument:
                    double pcntTbill = instrumentWeights[FindInstrument(ABS_MOMENTUM)];
                    instrumentWeights[FindInstrument(ABS_MOMENTUM)] = 0.0;
                    instrumentWeights[safeInstrument] += pcntTbill;

                    // submit orders
                    _alloc.LastUpdate = SimTime[0];
                    foreach (var nick in assets)
                    {
                        var asset = FindInstrument(nick);
                        _alloc.Allocation[asset] = instrumentWeights[asset];

                        int targetShares = (int)Math.Floor(instrumentWeights[asset] * NetAssetValue[0] / asset.Close[0]);
                        int currentShares = asset.Position;
                        Order newOrder = asset.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, assets.Select(nick => FindInstrument(nick)));

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
                //_plotter.AddParameters(this);
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

    #region U.S. Stocks w/ Absolute Momentum
    public class Antonacci_USStocksWithAbsoluteMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's U.S. Stocks w/ Absolute Momentum";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass
            {
                weight = 1.0,
                assets = new HashSet<string> {
                    "SPY", // S&P 500
                    ABS_MOMENTUM,
                }
            },
        };
        protected override string BENCHMARK => Globals.STOCK_MARKET;
    }
    #endregion
    #region Global Equities Momentum
    public class Antonacci_GlobalEquitiesMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Global Equities Momentum";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass
            {
                weight = 1.0,
                assets = new HashSet<string> {
                    "SPY",  // S&P 500
                    "ACWX", // ACWI ex U.S.
                    ABS_MOMENTUM,
                },
            },
        };
        protected override string BENCHMARK => Globals.STOCK_MARKET;
    }
    #endregion
    #region Global Balanced Momentum
    public class Antonacci_GlobalBalancedMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Global Balanced Momentum";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass
            {
                weight = 0.7,
                assets = new HashSet<string> {
                    "SPY",  // S&P 500
                    "ACWX", // ACWI ex U.S.
                    ABS_MOMENTUM,
                },
            },
            new AssetClass
            {
                weight = 0.3,
                setSafeInstrument = true,
                assets = new HashSet<string> {
                    "TLT", // U.S. Long Treasury
                    "BWX", // Global Government Bonds
                    "HYG", // High Yield Bonds
                    ABS_MOMENTUM,
                },
            }
        };
    }
    #endregion
    #region 5 asset class parity portfolio w/ absolute momentum
    public class Antonacci_ParityWithAbsoluteMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Parity Portfolio w/ Absolute Momentum";
        protected override string BENCHMARK => Globals.BALANCED_PORTFOLIO;

        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- equity
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    "VTI",   // Vanguard Total Stock Market Index ETF
                    ABS_MOMENTUM,
                },
            },
            //--- treasury bond
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    "TLT",   // iShares 20+ Year Treasury Bond ETF
                    ABS_MOMENTUM,
                },
            },
            //--- reit
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    "VNQ",   // Vanguard Real Estate Index ETF
                    ABS_MOMENTUM,
                },
            },
            //--- credit bonds
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    //"HYG",   // iShares iBoxx High Yield Corporate Bond ETF
                    //"CIU" => changed to IGIB in 06/2018
                    "IGIB",  // iShares Intermediate-Term Corporate Bond ETF
                    ABS_MOMENTUM,
                },
            },
            //--- gold
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    "GLD",   // SPDR Gold Shares ETF
                    ABS_MOMENTUM,
                },
            },
        };
    }
    #endregion
    #region Dual Momentum Sector Rotation
    public class Antonacci_DualMomentumSectorRotation : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Dual Momentum Sector Rotation";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            // note that we removed those ETFs with short history
            new AssetClass { assets = new HashSet<string> { "XLB",  ABS_MOMENTUM } }, // Materials
            //new AssetClass { assets = new HashSet<string> { "XLC",  ABS_MOMENTUM } }, // Communication Services
            new AssetClass { assets = new HashSet<string> { "XLE",  ABS_MOMENTUM } }, // Energy
            new AssetClass { assets = new HashSet<string> { "XLF",  ABS_MOMENTUM } }, // Financial
            new AssetClass { assets = new HashSet<string> { "XLI",  ABS_MOMENTUM } }, // Industrial
            new AssetClass { assets = new HashSet<string> { "XLK",  ABS_MOMENTUM } }, // Technology
            new AssetClass { assets = new HashSet<string> { "XLP",  ABS_MOMENTUM } }, // Consumer Staples
            //new AssetClass { assets = new HashSet<string> { "XLRE", ABS_MOMENTUM } }, // Real Estate
            new AssetClass { assets = new HashSet<string> { "XLU",  ABS_MOMENTUM } }, // Utilities
            new AssetClass { assets = new HashSet<string> { "XLV",  ABS_MOMENTUM } }, // Health Care
            new AssetClass { assets = new HashSet<string> { "XLY",  ABS_MOMENTUM } }, // Consumer Discretionary
        };
        protected override string BENCHMARK => Globals.STOCK_MARKET;
    }
    #endregion
    #region Dual Momentum w/ 4 asset pairs - as seen on Scott's Investments
    public class Antonacci_4PairsDualMomentum: Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Dual Momentum w/ 4 Pairs";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- equity
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    "VTI",   // Vanguard Total Stock Market Index ETF
                    "VEU",   // Vanguard FTSE All World ex US ETF
                    // could use SPY/ EFA here
                    ABS_MOMENTUM,
                },
            },
            //--- credit
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    "HYG",   // iShares iBoxx High Yield Corporate Bond ETF
                    //"CIU" => changed to IGIB in 06/2018
                    "IGIB",  // iShares Intermediate-Term Corporate Bond ETF
                    ABS_MOMENTUM,
                },
            },
            //--- real estate
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    "VNQ",   // Vanguard Real Estate Index ETF
                    "REM",   // iShares Mortgage Real Estate ETF
                    ABS_MOMENTUM,
                },
            },
            //--- economic stress
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    "GLD",   // SPDR Gold Shares ETF
                    "TLT",   // iShares 20+ Year Treasury Bond ETF
                    ABS_MOMENTUM,
                },
            },
        };
    }
    #endregion
}

//==============================================================================
// end of file