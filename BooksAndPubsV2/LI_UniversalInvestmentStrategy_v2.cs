//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        LI_UniversalInvestmentStrategy_v2
// Description: Universal Investment Strategy as described by Logical Invest.
//              https://logical-invest.com/app/strategy/uis/universal-investment-strategy
//              https://logical-invest.com/universal-investment-strategy/
// History:     2020viiii15, FUB, created
//              2022x29, FUB, ported to v2 engine
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.GlueV2;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region LI_UniversalInvestmentStrategy_Core
    public abstract class LI_UniversalInvestmentStrategy_Core : Algorithm
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (Core)";

        #region inputs
        public virtual IEnumerable<object> ASSETS { get; set; }
        public virtual int NUM_ASSETS { get => ASSETS.Count(); set { } }

        [OptimizerParam(21, 252, 21)]
        public virtual int LOOKBACK { get; set; } = 72;
        [OptimizerParam(0, 1000, 50)]
        public virtual int VOL_WEIGHT { get; set; } = 250;
        public virtual int MIN_ALLOC { get; set; } = 0;
        public virtual int MAX_ALLOC { get; set; } = 100;
        public virtual int ALLOC_STEP { get; set; } = 10;
        //public virtual int MAX_VOL { get; set; } = 999; // maximum portfolio volatility (not implemented)
        //public virtual int CASH_SR { get; set; } = -999; // Sharpe Ratio of cash (not implemented)
        public virtual string BENCH { get; set; } = Benchmark.PORTFOLIO_60_40;

        public enum OptMode
        {
            EqualWeight,
            EqualVolatility,
            MaxFitness,
        };

        public virtual OptMode OPT_MODE { get; set; } = OptMode.MaxFitness;

        public virtual bool IsTradingDay => IsFirstBar || SimDate.Month != NextSimDate.Month;
        #endregion
        #region asset allocation optimizer
        (double, double) CalcReturnAndVolatility(Dictionary<object, double> assetWeights)
        {
            var series = Enumerable.Range(0, LOOKBACK)
                .Select(t => assetWeights.Sum(kv => kv.Value * Asset(kv.Key).Close.LogReturn()[t]))
                .ToList();

            var mean = series.Average(r => r);
            var vol = Math.Sqrt(series.Sum(r => Math.Pow(r - mean, 2.0)));

            return (252.0 * mean, Math.Sqrt(252.0) * vol); // annualize values
        }
        double CalcModifiedSharpe(Dictionary<object, double> assetWeights)
        {
            (var ret, var vol) = CalcReturnAndVolatility(assetWeights);

            // Logical Invest 'normalizes' return and volatility before
            // calculating the modified Sharpe Ratio. That way, the
            // ratio can also be calculated for negative returns
            var retN = Math.Max(0.0, 1.0 + ret);
            var volN = 1.0 + vol;

            return retN / Math.Pow(volN, VOL_WEIGHT / 100.0);
        }
        protected Dictionary<object, double> OptimizeWeights(List<object> topAssets)
        {
            var currentWeights = topAssets
                .ToDictionary(a => a, a => 0);
            var bestWeights = (Dictionary<object, double>)null;
            var bestFitness = -1e99;

            void recursiveOptimizer(int level)
            {
                var weightAvailable = 100 - Enumerable.Range(0, level)
                    .Sum(l => currentWeights[topAssets[l]]);

                if (level == topAssets.Count - 1)
                {
                    // no more free weights, calculate fitness

                    // allocation out of range - skip
                    if (weightAvailable < MIN_ALLOC
                        || weightAvailable > MAX_ALLOC)
                        return;

                    currentWeights[topAssets[level]] = weightAvailable;

                    var currentWeights2 = currentWeights
                        .ToDictionary(kv => kv.Key, kv => kv.Value / 100.0);

                    var fitness = CalcModifiedSharpe(currentWeights2);

                    if (fitness > bestFitness)
                    {
                        // memorize best allocation
                        bestFitness = fitness;
                        bestWeights = currentWeights2;
                    }
                }
                else
                {
                    // free weights, iterate
                    for (int w = MIN_ALLOC;
                        w <= Math.Min(MAX_ALLOC, weightAvailable);
                        w += ALLOC_STEP)
                    {
                        currentWeights[topAssets[level]] = w;
                        recursiveOptimizer(level + 1);
                    }
                }
            }

            recursiveOptimizer(0);

            return bestWeights;
        }
        #endregion
        #region main logic
        public override void Run()
        {
            //========== initialization ==========

            StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(0.5 * 365);
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            //========== simulation loop ==========

            SimLoop(() =>
            {
                if (IsTradingDay)
                {
                    //----- pick top-ranking assets
                    var topAssets = ASSETS
                        .OrderByDescending(a => CalcModifiedSharpe(new Dictionary<object, double> { { a, 1.0 } }))
                        .Take(NUM_ASSETS)
                        .Select(a => (object)Asset(a).Name) // convert algorithm objects to their string name
                        .ToList();

                    //----- money management

                    // assume to close all open positions
                    var weights = Positions.Keys
                        .ToDictionary(a => (object)a, a => 0.0);

                    switch (OPT_MODE)
                    {
                        case OptMode.EqualWeight:
                            foreach (var a in topAssets)
                                weights[a] = 1.0 / NUM_ASSETS;
                            break;

                        case OptMode.EqualVolatility:
                            {
                                var vol = topAssets
                                    .ToDictionary(
                                        a => a,
                                        a =>
                                        {
                                            (var ret, var vol) = CalcReturnAndVolatility(new Dictionary<object, double> { { a, 1.0 } });
                                            return vol;
                                        });
                                var totalVol = vol.Sum(kv => 1.0 / kv.Value);

                                foreach (var a in topAssets)
                                    weights[a] = 1.0 / (totalVol * vol[a]);
                            }
                            break;

                        case OptMode.MaxFitness:
                            {
                                var optWeights = OptimizeWeights(topAssets);

                                if (optWeights == null)
                                    OptimizeWeights(topAssets);

                                foreach (var kv in optWeights)
                                    weights[kv.Key] = kv.Value;
                            }
                            break;
                    }

                    // TODO: implement volatility overlay here

                    //----- order management
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, OrderType.openNextBar);
                }

                //----- main chart
                if (!IsOptimizing)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot(Asset(BENCH).Description, Asset(BENCH).Close[0]);
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
    #endregion

    #region original SPY/ TLT
    public class LI_UniversalInvestmentStrategy_SPY_TLT : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (SPY/ TLT)";

        public override IEnumerable<object> ASSETS { get; set; } = new List<string>
        {
            ETF.SPY,
            ETF.TLT,
        };
        public override int LOOKBACK { get; set; } = 84;
        public override int VOL_WEIGHT { get; set; } = 200;
    }
    #endregion
    #region 3x Leveraged 'Hell on Fire'
    public class LI_UniversalInvestmentStrategy_3x : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (3x Leveraged 'Hell on Fire')";

        // LogicalInvest shorts the 3x inverse ETFs instead
        public override IEnumerable<object> ASSETS { get; set; } = new List<string>
        {
            ETF.SPXL,
            ETF.TMF,
        };
        public override int LOOKBACK { get; set; } = 63;
        public override int VOL_WEIGHT { get; set; } = 400;
    }
    #endregion
}

//==============================================================================
// end of file