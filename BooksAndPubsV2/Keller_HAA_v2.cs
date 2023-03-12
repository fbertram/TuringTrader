//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_HAA_v2
// Description: Bold Asset Allocation (BAA) strategy, as published in 
//              Wouter J. Keller's  and Jan Willem Keyning's paper 
//              'Dual and Canary Momentum with Rising Yields/Inflation:
//              Hybrid Asset Allocation (HAA)'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=4346906
// History:     2023iii06, FUB, created
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
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region HAA core
    public abstract class Keller_HAA_Core : Algorithm
    {
        #region configuration
        //--- offensive configuration

        /// <summary>
        /// Offensive universe.
        /// </summary>
        public abstract List<string> SEL_O { get; set; }

        /// <summary>
        /// Number of offensive assets.
        /// </summary>
        public virtual int NO => SEL_O.Count;

        /// <summary>
        /// Number of offensive assets to hold.
        /// </summary>
        public virtual int TO { get => Math.Max(1, SEL_O.Count / 2); set { } }

        //--- defensive configuration

        /// <summary>
        /// Defensive universe.
        /// </summary>
        public virtual List<string> SEL_D { get; set; } = new List<string>
        {
            ETF.IEF,
            ETF.BIL,
        };

        /// <summary>
        /// Number of defensive assets
        /// </summary>
        public virtual int ND => SEL_D.Count;

        /// <summary>
        /// Number of defensive assets to hold.
        /// </summary>
        //public virtual int TD { get; set; } = 1;

        //--- protective (canary configuration

        /// <summary>
        /// Protective (canary) universe.
        /// </summary>
        public virtual List<string> SEL_P { get; set; } = new List<string>
        {
            ETF.TIP,
        };

        /// <summary>
        /// Number of protective assets (canary universe).
        /// </summary>
        public virtual int NP => SEL_P.Count;

        /// <summary>
        /// 13612U filter (denoted L=1) is the (unweighted)
        /// average total returns over the last 1, 3, 6 and 12 months.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public double MOMENTUM(string asset)
            => new List<int> { 1, 3, 6, 12 }.Sum(m =>
                Asset(asset).Close[0] / Asset(asset).Monthly().Close[m] - 1.0);

        /// <summary>
        /// Transaction cost (default = 0.1%).
        /// </summary>
        //public virtual double TC { get; set; } = 0.1;

        public virtual OrderType ORDER_TYPE { get; set; } = OrderType.closeThisBar;

        public virtual string BENCH { get; set; } = Benchmark.PORTFOLIO_60_40;
        #endregion
        #region strategy logic
        public override void Run()
        {


            //========== initialization ==========

            StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(365);

            // Keller assumes 0.1% transaction cost
            //((Account_Default)Account).Friction = TC / 100.0;
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            //========== simulation loop ==========

            /* see paper, page 13
            The recipe for this new HAA strategy is simple: on the close of the 
            last trading day of each month:
            1. Calculate the momentum of each asset in the(risky) offensive, 
               defensive(BIL / IEF) and canary(TIP) universe, where momentum is 
               the average total return over the past 1, 3, 6, and 12 months.
            2. Select only the best defensive ‘cash’ asset(BIL or IEF) when 
               TIP is bad, or else allocate 1 / TopX of the portfolio to each 
               of the best TopX half of the risky assets(equally weighted), while 
               replacing each of those TopX assets by the best ‘cash’ asset when 
               ‘bad’ (ie.has non - positive momentum).
            3. Hold all positions until the final trading day of the following 
               month.Rebalance the entire portfolio monthly, regardless of 
               whether there is a change in position.
            */

            SimLoop(() =>
            {
                // Keller's strategy only trades once per month
                if (SimDate.Month != NextSimDate.Month || IsFirstBar)
                {
                    //--- asset ranking

                    // rank the offensive assets by momentum
                    // and pick the top half.
                    var offensiveMom = SEL_O
                        .ToDictionary(
                            a => a,
                            a => MOMENTUM(a));

                    var offensiveAssets = SEL_O
                        .OrderByDescending(a => offensiveMom[a])
                        .Take(TO)
                        .ToList();

                    // rank the defensive assets by momentum
                    // and pick the best one
                    var defensiveAsset = SEL_D
                        .OrderByDescending(a => MOMENTUM(a))
                        .First();

                    //--- money management

                    // consider markets crashed when more than
                    // one asset in the protective universe
                    // has negative momentum

                    var crash = SEL_P
                        .Select(a => MOMENTUM(a))
                        .Where(a => a < 0.0)
                        .Count() > 0;

                    // start with 100% offensive assets.
                    // if the markets have crashed, replace
                    // all with the defensive asset. Otherwise,
                    // replace those with negative momentum

                    var assetsToHold = offensiveAssets
                        .Select(a => crash
                            ? defensiveAsset
                            : (offensiveMom[a] < 0 ? defensiveAsset : a))
                        .ToList();

                    //--- order management

                    // initially, assume to close all open positions
                    var weights = Positions
                        .ToDictionary(
                            kv => kv.Key,
                            kv => 0.0);

                    // assign weights to assets we want to hold
                    foreach (var a in assetsToHold)
                        weights[a] = (weights.ContainsKey(a) ? weights[a] : 0.0) + 1.0 / TO;

                    // place the rebalancing orders. we assume that
                    // Keller fills the orders on the monthly close,
                    // but we can configure the code to fill on the
                    // next open
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, ORDER_TYPE);
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

    //--- main strategy
    #region HAA-Balanced
    public class Keller_HAA_Balanced : Keller_HAA_Core
    {
        public override string Name => "Keller's Hybrid Asset Allocation (HAA-Balanced)";

        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY, ETF.IWM,
            ETF.VWO, ETF.VEA,
            ETF.VNQ, ETF.DBC,
            ETF.IEF, ETF.TLT,
        };
    }
    #endregion

    //--- strategy variants
    #region HAA-16
    /*public class Keller_HAA_16 : Keller_HAA_Core
    {
        public override string Name => "Keller's Hybrid Asset Allocation (HAA-16)";

        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY, ETF.QQQ, ETF.IWM, ETF.IWD,
            ETF.VGK, ETF.EWJ, ETF.VWO, ETF.SCZ, 
            ETF.VNQ, ETF.REM, ETF.DBC, ETF.GLD, 
            ETF.IEF, ETF.TLT, ETF.HYG, ETF.LQD,
        };
    }*/
    #endregion
    #region HAA-12
    public class Keller_HAA_12 : Keller_HAA_Core
    {
        public override string Name => "Keller's Hybrid Asset Allocation (HAA-12)";

        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY, ETF.QQQ, ETF.IWM,
            ETF.VGK, ETF.EWJ, ETF.VWO,
            ETF.VNQ, ETF.DBC, ETF.GLD,
            ETF.IEF, ETF.TLT, ETF.LQD,
        };
    }
    #endregion
    #region HAA-4
    public class Keller_HAA_4 : Keller_HAA_Core
    {
        public override string Name => "Keller's Hybrid Asset Allocation (HAA-4)";

        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY,
            ETF.VEA,
            ETF.VNQ,
            ETF.IEF
        };
    }
    #endregion
    #region HAA-Simple
    public class Keller_HAA_Simple : Keller_HAA_Core
    {
        public override string Name => "Keller's Hybrid Asset Allocation (HAA-Simple)";

        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY,
        };
    }
    #endregion
}

//==============================================================================
// end of file
