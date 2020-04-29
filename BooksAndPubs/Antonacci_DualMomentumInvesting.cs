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
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    // the book is not always conclusive how to really implement these strategies
    // additional clarifications can be found here:
    // https://www.optimalmomentum.com/faq/

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
        protected virtual string ABS_MOMENTUM => Assets.BONDS_US_TREAS_3M; // BIL - 1 to 3-Months U.S. Treasury Bills
        /// <summary>
        /// safe instrument
        /// </summary>
        protected virtual string SAFE_INSTR => Assets.BONDS_US_TOTAL; // AGG - Aggregate Bond Market
        /// <summary>
        /// charting benchmark
        /// </summary>
        protected virtual string BENCHMARK => Assets.PORTF_60_40; // 60/40 Portfolio
        /// <summary>
        /// momentum calculation
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected virtual double MOMENTUM(Instrument i) => i.Close[0] / i.Close[252] - 1.0;
        /// <summary>
        /// simulation start time
        /// </summary>
        protected virtual DateTime START_TIME => Globals.START_TIME;
        /// <summary>
        /// simulation end time
        /// </summary>
        protected virtual DateTime END_TIME => Globals.END_TIME;
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

            //WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = START_TIME;
            EndTime = END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // it is unclear, if Antonacci considers commissions

            // assets we can trade
            List<string> ASSETS = ASSET_CLASSES
                .SelectMany(c => c.assets)
                .Distinct()
                .Where(nick => nick != ABS_MOMENTUM)
                .ToList();
            ASSETS.Add(SAFE_INSTR);

            var assets = AddDataSources(ASSETS);
            var safe = AddDataSource(SAFE_INSTR); // we just need the data source
            var absMom = AddDataSource(ABS_MOMENTUM);
            var benchmark = AddDataSource(BENCHMARK);

            double totalWeights = ASSET_CLASSES.Sum(a => a.weight);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // skip if there are any missing instruments
                if (!HasInstruments(assets) || !HasInstrument(benchmark) || !HasInstrument(absMom))
                    continue;

                // evaluate momentum for all known instruments
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i, i => MOMENTUM(i));

                // execute trades once per month
                // CAUTION: do not calculate indicators within this block!
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                    // loop through all asset classes, and find the top-ranked one
                    Instrument safeInstrument = safe.Instrument;
                    foreach (AssetClass assetClass in ASSET_CLASSES)
                    {
                        // find the instrument with the highest momentum
                        // in each asset class
                        var bestInstrument = assetClass.assets
                            .Select(nick => FindInstrument(nick))
                            .OrderByDescending(i => instrumentMomentum[i])
                            .Take(1)
                            .First();

                        // sum up the weights (because instrument is duplicated)
                        instrumentWeights[bestInstrument] += assetClass.weight / totalWeights;

                        if (assetClass.setSafeInstrument)
                            safeInstrument = bestInstrument;
                    }

                    // if momentum of any instrument drops below that of a T-Bill,
                    // we use the safe instrument instead
                    // therefore, we swap T-Bills for the safe instrument:
                    double pcntTbill = instrumentWeights[absMom.Instrument];
                    instrumentWeights[absMom.Instrument] = 0.0;
                    instrumentWeights[safeInstrument] += pcntTbill;

                    // submit orders
                    _alloc.LastUpdate = SimTime[0];
                    foreach (var ds in assets)
                    {
                        _alloc.Allocation[ds.Instrument] = instrumentWeights[ds.Instrument];

                        int targetShares = (int)Math.Floor(instrumentWeights[ds.Instrument] * NetAssetValue[0] / ds.Instrument.Close[0]);
                        int currentShares = ds.Instrument.Position;
                        Order newOrder = ds.Instrument.Trade(targetShares - currentShares);

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
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    _plotter.AddStrategyHoldings(this, assets.Select(ds => ds.Instrument));

                    if (IsSubclassed) 
                        AddSubclassedBar(10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL);
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
                    Assets.STOCKS_US_LG_CAP,
                    ABS_MOMENTUM,
                }
            },
        };
        protected override string BENCHMARK => Assets.STOCKS_US_LG_CAP;
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
                    Assets.STOCKS_US_LG_CAP,
                    Assets.STOCKS_WXUS_LG_MID_CAP,
                    ABS_MOMENTUM,
                },
            },
        };
        protected override string BENCHMARK => Assets.PORTF_60_40;
    }

    public class Antonacci_GlobalEquitiesMomentum_p98 : Antonacci_GlobalEquitiesMomentum
    {
        // on page 98 of his book, Antonacci describes that he tests
        // for S&P 500's absolute momentum first, because S&P 500
        // is leading world markets.
        // this contradicts the flowchart and description on page 101
        // this code is the 'absolute momentum first' implementation
        public override string Name => "Antonacci's Global Equities Momentum (p98 variant)";
        protected override double MOMENTUM(Instrument i)
        {
            var m = i.Close[0] / i.Close[252] - 1.0;

            var spx = FindInstrument(Assets.STOCKS_US_LG_CAP);
            var abs = FindInstrument(ABS_MOMENTUM);

            if (i.Nickname == Assets.STOCKS_WXUS_LG_MID_CAP
            && MOMENTUM(spx) < MOMENTUM(abs))
            {
                // if S&P 500 returns are below T-Bill,
                // ACWX is forced negative, so that we end
                // up in bonds
                return -999.9;
            }

            return m;
        }
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
                    Assets.STOCKS_US_LG_CAP,
                    Assets.STOCKS_WXUS_LG_MID_CAP,
                    ABS_MOMENTUM,
                },
            },
            new AssetClass
            {
                weight = 0.3,
                setSafeInstrument = true, // use this group as safe instrument
                assets = new HashSet<string> {
                    Assets.BONDS_US_TREAS_30Y,
                    Assets.BONDS_WRLD_TREAS,
                    Assets.BONDS_US_CORP_JUNK,
                    ABS_MOMENTUM,
                },
            }
        };
    }
    #endregion
    #region Parity Portfolio w/ Absolute Momentum
    public class Antonacci_ParityPortfolioWithAbsoluteMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Parity Portfolio w/ Absolute Momentum";
        protected override string BENCHMARK => Assets.PORTF_60_40;

        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- equity
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    Assets.STOCKS_US_LG_CAP,
                    ABS_MOMENTUM,
                },
            },
            //--- treasury bond
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    Assets.BONDS_US_TREAS_30Y,
                    ABS_MOMENTUM,
                },
            },
            //--- reit
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    Assets.REIT_US,
                    ABS_MOMENTUM,
                },
            },
            //--- credit bonds
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    Assets.BONDS_US_CORP_10Y,
                    ABS_MOMENTUM,
                },
            },
            //--- gold
            new AssetClass {
                weight = 0.2,
                assets = new HashSet<string> {
                    Assets.GOLD,
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
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_MATERIALS,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_COMMUNICATION,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_ENERGY,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_FINANCIAL,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_INDUSTRIAL,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_TECHNOLOGY,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_STAPLES,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_REAL_ESTATE, ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_UTILITIES,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_HEALTH_CARE,  ABS_MOMENTUM } },
            new AssetClass { assets = new HashSet<string> { Assets.STOCKS_US_SECT_DISCRETIONARY,  ABS_MOMENTUM } },
        };
        protected override string BENCHMARK => Assets.STOCKS_US_LG_CAP;
    }
    #endregion
    #region Dual Momentum w/ 4 asset pairs - as seen on Scott's Investments
    // see also:
    // https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2042750
    // https://allocatesmartly.com/antonaccis-composite-dual-momentum/
    // Equities: SPY/ EFA
    // Credit risk: LQD/ HYG
    // Real estate: VNQ/ REM
    // Economic stress: GLD/ TLT
    // safe instrument: BIL
    public class Antonacci_4PairsDualMomentum: Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Antonacci's Dual Momentum w/ 4 Pairs";
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            //--- equity
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    Assets.STOCKS_US_LG_CAP,
                    Assets.STOCKS_WXUS_LG_MID_CAP,
                    ABS_MOMENTUM,
                },
            },
            //--- credit
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    Assets.BONDS_US_CORP_JUNK,
                    Assets.BONDS_US_CORP_10Y,
                    ABS_MOMENTUM,
                },
            },
            //--- real estate
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    Assets.REIT_US,
                    Assets.MREIT_US,
                    ABS_MOMENTUM,
                },
            },
            //--- economic stress
            new AssetClass {
                weight = 0.25,
                assets = new HashSet<string> {
                    Assets.GOLD,
                    Assets.BONDS_US_TREAS_30Y,
                    ABS_MOMENTUM,
                },
            },
        };
    }
    #endregion
    #region Accelerating Dual Momentum - as seen on www.EngineeredPortfolio.com
    // post: https://engineeredportfolio.com/2018/05/02/accelerating-dual-momentum-investing/
    // code by author here: https://www.quantopian.com/posts/accelerating-dual-momentum-150-year-backtest
    // simulation: https://www.portfoliovisualizer.com/test-market-timing-model?s=y&coreSatellite=false&timingModel=6&startYear=1985&endYear=2018&initialAmount=10000&symbols=VFINX+VINEX&singleAbsoluteMomentum=false&volatilityTarget=9.0&downsideVolatility=false&outOfMarketAssetType=2&outOfMarketAsset=VUSTX&movingAverageSignal=1&movingAverageType=1&multipleTimingPeriods=true&periodWeighting=2&windowSize=1&windowSizeInDays=105&movingAverageType2=1&windowSize2=10&windowSizeInDays2=105&volatilityWindowSize=0&volatilityWindowSizeInDays=0&assetsToHold=1&allocationWeights=1&riskControl=false&riskWindowSize=10&riskWindowSizeInDays=0&rebalancePeriod=1&separateSignalAsset=false&tradeExecution=0&benchmark=VFINX&timingPeriods[0]=1&timingUnits[0]=2&timingWeights[0]=33&timingPeriods[1]=3&timingUnits[1]=2&timingWeights[1]=33&timingPeriods[2]=6&timingUnits[2]=2&timingWeights[2]=34&timingUnits[3]=2&timingWeights[3]=0&timingUnits[4]=2&timingWeights[4]=0&volatilityPeriodUnit=2&volatilityPeriodWeight=0
    public class EngineeredPortfolio_AcceleratingDualMomentum : Antonacci_DualMomentumInvesting_Core
    {
        public override string Name => "Engineered Portfolio's Accelerating Dual Momentum";

        //protected override DateTime START_TIME => DateTime.Parse("01/01/1990");
        protected override HashSet<AssetClass> ASSET_CLASSES => new HashSet<AssetClass>
        {
            new AssetClass
            {
                weight = 1.0,
                assets = new HashSet<string> {
                    "splice:SPY,yahoo:VFINX",
                    "splice:VSS,yahoo:VINEX",
                    ABS_MOMENTUM,
                },
            },
        };

        // the instrument for absolute momentum is just a dummy,
        // MOMENTUM() will always return zero for this
        protected override string ABS_MOMENTUM => "splice:BIL,yahoo:VFISX";
        protected override string SAFE_INSTR => "splice:VGLT,yahoo:VUSTX";
        //protected override string BENCHMARK => "yahoo:VFINX";
        //protected override string BENCHMARK => "$SPXTR";
        //protected override string BENCHMARK => "$SPX";

        protected override double MOMENTUM(Instrument i)
        {
            if (i.Nickname == ABS_MOMENTUM)
                return 0.0;

            var m1 = i.Close[0] / i.Close[21] - 1.0;
            var m3 = i.Close[0] / i.Close[63] - 1.0;
            var m6 = i.Close[0] / i.Close[126] - 1.0;

            return m1 + m3 + m6;
        }
    }
    #endregion
}

//==============================================================================
// end of file