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
    public abstract class Antonacci_DualMomentumInvesting_Core : Algorithm
    {
        public override string Name => "Antonacci's Dual Momentum";

        #region inputs
        // this is the benchmark to measure absolute momentum:
        protected abstract string ABS_BENCHM { get; }
        // the safe instrument, in case we fall below the absolute benchmark:
        protected abstract string SAFE_INSTR { get; }
        protected abstract HashSet<HashSet<string>> ASSET_CLASSES { get; }
        #endregion
        #region internal data
        private readonly string BENCHMARK = "@60_40";
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
                .SelectMany(c => c)
                .Distinct()
                .Where(nick => nick != ABS_BENCHM)
                .ToList();
            assets.Add(SAFE_INSTR);

            AddDataSources(assets);
            AddDataSource(ABS_BENCHM);
            AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate momentum for all known instruments
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i,
                        i => i.Close[0] / i.Close[252] - 1.0);

                // skip if there are any missing instruments
                if (!HasInstruments(assets) || !HasInstrument(BENCHMARK) || !HasInstrument(ABS_BENCHM))
                    continue;

                // execute trades once per month
                // CAUTION: do not calculate indicators within this block!
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                    // loop through all asset classes, and find the top-ranked one
                    foreach (HashSet<string> assetClass in ASSET_CLASSES)
                    {
                        // find the instrument with the highest momentum
                        // in each asset class
                        var bestInstrument = assetClass
                            .Select(nick =>FindInstrument(nick))
                            .OrderByDescending(i => instrumentMomentum[i])
                            .Take(1)
                            .First();

                        // sum up the weights (because safe instrument is duplicated)
                        instrumentWeights[bestInstrument] += 1.0 / ASSET_CLASSES.Count;
                    }

                    // if momentum of any instrument drops below that of a T-Bill,
                    // we use the safe instrument instead
                    // therefore, we swap T-Bills for the safe instrument:
                    double pcntTbill = instrumentWeights[FindInstrument(ABS_BENCHM)];
                    instrumentWeights[FindInstrument(ABS_BENCHM)] = 0.0;
                    instrumentWeights[FindInstrument(SAFE_INSTR)] = pcntTbill;

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
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
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

    #region strategy w/ 4 asset classes
    public class Antonacci_DualMomentumInvesting : Antonacci_DualMomentumInvesting_Core
    {
        protected override string ABS_BENCHM => "BIL"; // SPDR Bloomberg Barclays 1-3 Month T-Bill ETF

        protected override string SAFE_INSTR => "AGG"; // iShares Core U.S. Aggregate Bond ETF

        protected override HashSet<HashSet<string>> ASSET_CLASSES => new HashSet<HashSet<string>>
        {
            //--- equity
            new HashSet<string> {
                "VTI",   // Vanguard Total Stock Market Index ETF
                "VEU",   // Vanguard FTSE All World ex US ETF
                // could use SPY/ EFA here
                ABS_BENCHM,
            },
            //--- credit
            new HashSet<string> {
                "HYG",   // iShares iBoxx High Yield Corporate Bond ETF
                //"CIU" => changed to IGIB in 06/2018
                "IGIB",  // iShares Intermediate-Term Corporate Bond ETF
                ABS_BENCHM,
            },
            //--- real estate
            new HashSet<string> {
                "VNQ",   // Vanguard Real Estate Index ETF
                "REM",   // iShares Mortgage Real Estate ETF
                ABS_BENCHM,
            },
            //--- economic stress
            new HashSet<string> {
                "GLD",   // SPDR Gold Shares ETF
                "TLT",   // iShares 20+ Year Treasury Bond ETF
                ABS_BENCHM,
            },
        };
    }
    #endregion
}

//==============================================================================
// end of file