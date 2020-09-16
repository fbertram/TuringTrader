//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        LI_UniversalInvestmentStrategy
// Description: Strategy as described by Logical Invest.
//              https://logical-invest.com/app/strategy/uis/universal-investment-strategy
//              https://logical-invest.com/universal-investment-strategy/
// History:     2020viiii15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TuringTrader.Simulator;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region LI_UniversalInvestmentStrategy
    public abstract class LI_UniversalInvestmentStrategy_Core : AlgorithmPlusGlue
    {
        #region inputs
        [OptimizerParam(0, 100, 5)]
        public int VOL_FACT { get; set; } = 250;

        [OptimizerParam(50, 80, 2)]
        public int LOOKBACK_DAYS { get; set; } = 72;

        [OptimizerParam(0, 100, 10)]
        public int WFO_STOCK_PCNT { get; set; } = 60;

        public abstract string STOCKS { get; }
        public abstract string BONDS { get; }

        public virtual string BENCHMARK => Assets.PORTF_60_40;
        #endregion
        #region internal helpers
        private double UIS_ModifiedSharpe()
        {
            // this code is only required for optimization
            if (!IsOptimizing)
                return 0.0;

            var collecting = false;
            var dailyReturns = new List<double>();
            for (var t = NetAssetValue.BarsAvailable - 1; t >= 0; t--)
            {
                if (NetAssetValue[t] > 0.0 && NetAssetValue[t] != Globals.INITIAL_CAPITAL)
                    collecting = true;

                if (collecting)
                    dailyReturns.Add(Math.Log(NetAssetValue[t] / NetAssetValue[t + 1]));
            }

            var rd = dailyReturns.Average();
            var vd = dailyReturns
                .Select(r => Math.Pow(r - rd, 2.0))
                .Average();
            var f = VOL_FACT / 100.0;

            // modified sharpe ratio
            // f = 1.0: sharpe ratio
            // f = 0.0: only consider returns, not volatility
            // f > 1.0: increased relevance of volatility
            return rd / Math.Pow(vd, 0.5 * f);
        }
        #endregion

        #region OptimizeSettings - walk-forward-optimization
        private void OptimizeSettings()
        {
            // we only optimize settings on the top instance,
            // not those used for walk-forward optimization
            if (!IsOptimizing)
            {
                // enable optimizer parameters
                foreach (var s in OptimizerParams)
                    if (s.Value.Name.StartsWith("WFO_"))
                        s.Value.IsEnabled = true;

                // run optimization
                var optimizer = new OptimizerGrid(this, false);
                var end = SimTime[0];
                var start = SimTime[LOOKBACK_DAYS];
                optimizer.Run(start, end);

                // apply parameters from best result
                var best = optimizer.Results
                    .OrderByDescending(r => r.Fitness)
                    .FirstOrDefault();
                optimizer.SetParametersFromResult(best);
            }
        }
        #endregion
        #region Run - algorithm core
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

            StartTime = startTime != null ? (DateTime)startTime : Globals.START_TIME;
            EndTime = endTime != null ? (DateTime)endTime : Globals.END_TIME - TimeSpan.FromDays(5);
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

                // re-tune parameters on a monthly schedule
                if (firstOptimization || NextSimTime.Month != SimTime[0].Month)
                    OptimizeSettings();
                firstOptimization = false;

                // rebalance on a monthly schedule
                if (NextSimTime.Month != SimTime[0].Month)
                {
                    Alloc.LastUpdate = SimTime[0];

                    var stockPcnt = WFO_STOCK_PCNT / 100.0;
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
                }
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
            FitnessValue = UIS_ModifiedSharpe();

            yield break;
        }
        #endregion
    }
    #endregion

    #region original SPY/ TLT
    public class LI_UniversalInvestmentStrategy : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (SPY/ TLT)";

        public override string STOCKS => "SPY";

        // LogicalInvest uses HEDGE here
        public override string BONDS => "TLT";
    }
    #endregion
    #region 'Hell on Fire' version (3x leveraged)
    public class LI_UniversalInvestmentStrategy_3x : LI_UniversalInvestmentStrategy_Core
    {
        public override string Name => "Logical Invest's Universal Investment Strategy (3x Leveraged 'Hell on Fire')";

        // LogicalInvest shorts the 3x inverse ETFs instead
        public override string STOCKS => Assets.STOCKS_US_LG_CAP_3X;

        public override string BONDS => Assets.BONDS_US_TREAS_30Y_3X;
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