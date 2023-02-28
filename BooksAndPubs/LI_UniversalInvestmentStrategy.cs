//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        LI_UniversalInvestmentStrategy
// Description: Universal Investment Strategy as described by Logical Invest.
//              https://logical-invest.com/app/strategy/uis/universal-investment-strategy
//              https://logical-invest.com/universal-investment-strategy/
// History:     2020viiii15, FUB, created
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
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
#if true
    #region helper functions
    static class Helpers
    {
        public static double StandardDeviation(this IEnumerable<double> rets)
        {
            var mean = rets.Average();
            var var = rets
                .Select(r => Math.Pow(r - mean, 2.0))
                .Average();

            return Math.Sqrt(var);
        }
    }
    #endregion
    #region LI_UniversalInvestmentStrategy_Core (faster optimization)
    public abstract class LI_UniversalInvestmentStrategy_Core : AlgorithmPlusGlue
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (Core)";

        #region inputs
        public virtual int WFO_LOOKBACK { get; set; } = 72;
        public virtual int VOL_WEIGHT { get; set; } = 250;
        public abstract IEnumerable<object> ASSETS { get; }
        #endregion
        #region optional customization
        public virtual int NUM_ASSETS { get; set; } = 3;

        public virtual string TBILL { get; set; } = null;
        public virtual string BENCH { get; set; } = Indices.PORTF_60_40;

        protected virtual bool IsOptimizationDay => SimTime[0].Month != NextSimTime.Month;
        protected virtual bool IsTradingDay => IsOptimizationDay;

        protected virtual int IsAllocValid(Dictionary<Instrument, double> alloc, double ret, double vol) => 1;
        protected virtual double VolatilityOverlay(ITimeSeries<double> rawPortfolioReturns) => 1.0;
        protected virtual Dictionary<Instrument, double> Tranching(Dictionary<Instrument, double> weights) => weights;
        #endregion
        #region asset allocation optimizer
        protected virtual (double, double) CalcReturnAndRisk(IEnumerable<double> returnSeries)
        {
            var annualizedReturn = 252.0 * returnSeries.Average();
            var annualizedVolatility = 100.0 * Math.Sqrt(252.0) * returnSeries.StandardDeviation();

            return (annualizedReturn, annualizedVolatility);
        }
        protected Dictionary<Instrument, double> Optimize(IEnumerable<Instrument> assets, Instrument tbill)
        {
            // NOTE: this function is called not called on very bar, but
            //       a (kind of) irregular schedule. Therefore, we should
            //       not call any indicators here, unless we know for sure
            //       that these are indepent from prior results.

            var currentWeights = assets.ToDictionary(a => a, a => 0);
            var bestWeights = assets.ToDictionary(a => a, a => 0.0);
            var bestFitness = -1e10;
            var bestValidity = 1;

            //var riskFreeRate = Math.Log(tbill.Close[0] / tbill.Close[252]);
            var riskFreeRate = tbill != null
                ? 252.0 * tbill.Close.Momentum(63)[0] // Momentum is safe to call!
                : 0.0;

            void recursiveOptimize(int level, int parentNumAssets)
            {
                var currentInstrument = currentWeights.Skip(level).First().Key;
                var weightAvailable = 100 - currentWeights.Take(level).Sum(kv => kv.Value);

                for (int currentWeight = 0; currentWeight <= 100; currentWeight += 10)
                {
                    currentWeights[currentInstrument] = currentWeight;
                    var currentNumAssets = parentNumAssets
                        + (currentWeight > 0 ? 1 : 0);

                    // exceeded max number of assets
                    if (currentNumAssets > NUM_ASSETS)
                        return; // no more valid combinations on this level

                    // total weight more than 100%
                    if (currentWeight > weightAvailable)
                        return; // no more valid combinations on this level

                    // total weight less than 100%
                    if (level == assets.Count() - 1 && currentWeight < weightAvailable)
                        continue;

                    if (level < assets.Count() - 1)
                    {
                        // free weights: recurse
                        recursiveOptimize(level + 1, currentNumAssets);
                    }
                    else
                    {
                        // all weights set: calculate fitness
                        var currentWeightsFloat = currentWeights
                            .ToDictionary(kv => kv.Key, kv => kv.Value / 100.0);

                        var currentReturnSeries = Enumerable.Range(0, WFO_LOOKBACK)
                            .Select(t => currentWeightsFloat
                                .Sum(kv => kv.Value * Math.Log(kv.Key.Close[t] / kv.Key.Close[t + 1])))
                            .ToList();

                        // typically, this is annualized return and annualized volatility
                        (var currentReturn, var currentRisk) = CalcReturnAndRisk(currentReturnSeries);

                        var currentFitness = (currentReturn - riskFreeRate)
                            / Math.Pow(Math.Max(1e-10, currentRisk), VOL_WEIGHT / 100.0);

                        var currentValidity = IsAllocValid(currentWeightsFloat, currentReturn, currentRisk);

                        // we aim to improve our overall fitness score
                        // a new allocation can only replace the current allocation
                        // if its validity score is greater or equal to the current.
                        if (currentFitness > bestFitness && currentValidity >= bestValidity)
                        {
                            bestFitness = currentFitness;
                            bestWeights = currentWeightsFloat;
                            bestValidity = currentValidity;
                        }
                    }
                }
            }

            recursiveOptimize(0, 0);

            return bestWeights;
        }
        #endregion

        #region main logic
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
            WarmupStartTime = StartTime - TimeSpan.FromDays(90);

            CommissionPerShare = Globals.COMMISSION;
            Deposit(Globals.INITIAL_CAPITAL);

            var assets = ASSETS
                .Select(o => AddDataSource(o))
                .ToList();
            var tbill = TBILL != null ? AddDataSource(TBILL) : null;
            var bench = AddDataSource(BENCH);

            //========== simulation loop ==========

            var weights = new Dictionary<Instrument, double>();

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(assets) || !HasInstrument(bench))
                    continue;

                if (weights.Count == 0 || IsOptimizationDay)
                {
                    var weightsNew = Optimize(assets.Select(a => a.Instrument), tbill?.Instrument);
                    weights = Tranching(weightsNew);
                }

                var assetReturns = Instruments
                    .ToDictionary(
                        i => i,
                        i => i.Close.LogReturn());

#if true
                foreach (var w in weights)
                {
                    if (!assetReturns.ContainsKey(w.Key))
                        Output.WriteWarning("Missing key '{$0}'", w.Key);
                }
#endif

                var rawPortfolioReturns = IndicatorsBasic.BufferedLambda(
                    prev => weights.Sum(kv => kv.Value * assetReturns[kv.Key][0]),
                    0.0);

                var volAdj = IndicatorsBasic.BufferedLambda(
                    prev => VolatilityOverlay(rawPortfolioReturns),
                    0.0);

                if (Positions.Count == 0 || IsTradingDay)
                {
                    foreach (var kv in weights)
                    {
                        var i = kv.Key;
                        var w = volAdj[0] * kv.Value;
                        Alloc.Allocation[i] = w;

                        var shares = (int)Math.Floor(w * NetAssetValue[0] / i.Close[0]);
                        i.Trade(shares - i.Position);
                    }
                }

                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    _plotter.AddStrategyHoldings(this, assets.Select(a => a.Instrument));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Portfolio Volatility", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Volatility NAV", 100.0 * Math.Sqrt(252.0) * NetAssetValue.Volatility(21)[0]);
                    _plotter.Plot("Volatility Raw", 100.0 * Math.Sqrt(252.0) * rawPortfolioReturns.StandardDeviation(21)[0]);
                    _plotter.Plot("Total Exposure", 100.0 * volAdj[0]);
                }

                var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    Name, SimTime[0],
                    v, v, v, v, 0);
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

            yield break;
        }
        #endregion
    }
    #endregion
#else
    #region LI_UniversalInvestmentStrategy_Core (walk-forward optimization)
    // Implementation Note:
    // For this implementation, we use TuringTrader's walk-forward-optimization.
    // Some readers might feel that this is overkill, given the simplicity of the
    // strategy. We agree that with a few lines of code a simple custom solution
    // can be crafted, which is likely less CPU intense.
    // However, we'd like to remind readers of the purpose of these showcase
    // implementations. With these strategies, we want to show-off TuringTrader's
    // features, and provide implementations which can serve as a robust starting
    // point for your own experimentation. We have no doubt that our solution,
    // based on walk-forward-optimization, scales better in these regards.
    public abstract class LI_UniversalInvestmentStrategy_Core : AlgorithmPlusGlue
    {
    #region inputs
        [OptimizerParam(0, 100, 5)]
        public virtual int VOL_WEIGHT { get; set; } = 250;

        [OptimizerParam(50, 80, 5)]
        public virtual int WFO_LOOKBACK { get; set; } = 72;

        [OptimizerParam(0, 100, 10)]
        public int STOCK_PCNT { get; set; } = 60;
        protected abstract IEnumerable<string> ASSETS { get; }
        private string STOCKS => ASSETS.First();
        private string BONDS => ASSETS.Last();

        public virtual string BENCHMARK => Assets.PORTF_60_40;
    #endregion
    #region internal helpers
        private double ModifiedSharpeRatio(double f)
        {
            // this code is only required for optimization
            if (!IsOptimizing)
                return 0.0;

            var dailyReturns = Enumerable.Range(0, TradingDays)
                .Select(t => Math.Log(NetAssetValue[t] / NetAssetValue[t + 1]))
                .ToList();
            var rd = dailyReturns.Average();
            var vd = dailyReturns
                .Select(r => Math.Pow(r - rd, 2.0))
                .Average();

            // modified sharpe ratio
            // f = 1.0: sharpe ratio
            // f = 0.0: only consider returns, not volatility
            // f > 1.0: increased relevance of volatility
            return rd / Math.Pow(vd, 0.5 * f);
        }
        private Dictionary<string, bool> SaveAndDisableOptimizerParams()
        {
            var isEnabled = new Dictionary<string, bool>();
            foreach (var s in OptimizerParams)
            {
                isEnabled[s.Key] = s.Value.IsEnabled;
                s.Value.IsEnabled = false;
            }
            return isEnabled;
        }

        private void RestoreOptimizerParams(Dictionary<string, bool> isEnabled)
        {
            foreach (var s in OptimizerParams)
                s.Value.IsEnabled = isEnabled[s.Key];
        }
    #endregion

    #region OptimizeParameter - walk-forward-optimization
        private void OptimizeParameter(string parameter)
        {
            if (parameter == "STOCK_PCNT"
                && !OptimizerParams["STOCK_PCNT"].IsEnabled)
            {
                var save = SaveAndDisableOptimizerParams();

                // run optimization
                var optimizer = new OptimizerGrid(this, false);
                var end = SimTime[0];
                var start = SimTime[WFO_LOOKBACK];
                OptimizerParams["STOCK_PCNT"].IsEnabled = true;
                optimizer.Run(start, end);

                // apply parameters from best result
                var best = optimizer.Results
                    .OrderByDescending(r => r.Fitness)
                    .FirstOrDefault();
                optimizer.SetParametersFromResult(best);

                RestoreOptimizerParams(save);
            }

            // NOTE: Frank Grossmann does not mention optimization of the lookback period.
            // this code fragment is only meant to demonstrate
            // how we could expand optimzation to other parameters
            if (parameter == "LOOKBACK_DAYS"
                && !OptimizerParams["STOCK_PCNT"].IsEnabled
                && !OptimizerParams["LOOKBACK_DAYS"].IsEnabled)
            {
                // var save = SaveAndDisableOptimizerParams();
                // TODO: put optimizer code here
                // RestoreOptimizerParams(save);
            }
        }
    #endregion
    #region Run - algorithm core
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

            StartTime = startTime != null ? (DateTime)startTime : Globals.START_TIME;
            EndTime = endTime != null ? (DateTime)endTime : Globals.END_TIME;
            WarmupStartTime = StartTime - TimeSpan.FromDays(90);

            CommissionPerShare = Globals.COMMISSION;
            Deposit(Globals.INITIAL_CAPITAL);

            var stocks = AddDataSource(STOCKS);
            var bonds = AddDataSource(BONDS);
            var bench = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            bool firstOptimization = true;
            foreach (var s in SimTimes)
            {
                if (!HasInstruments(new List<DataSource> { stocks, bonds, bench }))
                    continue;

                if (SimTime[0] < StartTime)
                    continue;

#if false
                // NOTE: the Universal Investment Strategy does not
                // use walk-forward optimization for the lookback days.
                // this code is only meant to demonstrate how optimization
                // could be expanded to include more parameters.
                if (firstOptimization 
                || (NextSimTime.Month != SimTime[0].Month && new List<int> { 1, 7 }.Contains(NextSimTime.Month)))
                    OptimizeParameter("LOOKBACK_DAYS");
#endif

                // re-tune asset allocation on monthly schedule
                if (firstOptimization
                || NextSimTime.Month != SimTime[0].Month)
                    OptimizeParameter("STOCK_PCNT");

                firstOptimization = false;

                // open positions on first execution, rebalance monthly
                if (NextSimTime.Month != SimTime[0].Month || Positions.Count == 0)
                {
                    Alloc.LastUpdate = SimTime[0];

                    var stockPcnt = STOCK_PCNT / 100.0;
                    var stockShares = (int)Math.Floor(NetAssetValue[0] * stockPcnt / stocks.Instrument.Close[0]);
                    stocks.Instrument.Trade(stockShares - stocks.Instrument.Position);
                    Alloc.Allocation[stocks.Instrument] = stockPcnt;

                    var bondPcnt = 1.0 - stockPcnt;
                    var bondShares = (int)Math.Floor(NetAssetValue[0] * bondPcnt / bonds.Instrument.Close[0]);
                    bonds.Instrument.Trade(bondShares - bonds.Instrument.Position);
                    Alloc.Allocation[bonds.Instrument] = bondPcnt;
                }
                else
                {
                    Alloc.AdjustForPriceChanges(this);
                }

                // strategy output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    _plotter.AddStrategyHoldings(this, new List<Instrument> { stocks.Instrument, bonds.Instrument });

                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    //_plotter.SelectChart("Walk-Forward Optimization", "Date");
                    //_plotter.SetX(SimTime[0]);
                    //_plotter.Plot("STOCK_PCNT", STOCK_PCNT);
                    //_plotter.Plot("LOOKBACK_DAYS", LOOKBACK_DAYS);
                }

                var v = NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
                    v, v, v, v, 0);
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddAverageHoldings(this);
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            // fitness value used for walk-forward-optimization
            FitnessValue = ModifiedSharpeRatio(VOL_WEIGHT / 100.0);
        }
    #endregion
    }
    #endregion
#endif

    #region original SPY/ TLT
    public class LI_UniversalInvestmentStrategy : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (SPY/ TLT)";

        public override IEnumerable<object> ASSETS => new List<string>
        {
            "SPY", // VFINX
            "TLT", // VUSTX. LogicalInvest uses HEDGE strategy here
        };
    }
    #endregion
    #region 3x Leveraged 'Hell on Fire'
    public class LI_UniversalInvestmentStrategy_3x : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (3x Leveraged 'Hell on Fire')";

        // LogicalInvest shorts the 3x inverse ETFs instead
        public override IEnumerable<object> ASSETS => new List<string>
        {
            Assets.SPXL,
            Assets.TMF,
        };
    }
    #endregion

    #region more
#if false
    // NOTE: it is unclear if these strategies are built upon the
    // same UIS core. It is likely they are not, and use a simple
    // momentum-based switching method instead.

    // HEDGE: Hedge Strategy
    // https://logical-invest.com/app/strategy/hedge/hedge-strategy
    // GLD-UUP, TRESHEDGE

    // GLD-UUP: Gold-Currency
    // https://logical-invest.com/app/strategy/gld-uup/gold-currency-strategy-ii
    // GSY, GLD

    // TRESHEDGE: Treasury Hedge
    // https://logical-invest.com/app/strategy/treshedge/treasury-hedge
    // TLT, GSY, TIP
#endif
    #endregion
}

//==============================================================================
// end of file