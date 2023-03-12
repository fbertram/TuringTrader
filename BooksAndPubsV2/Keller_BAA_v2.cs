//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_BAA_v2
// Description: Bold Asset Allocation (BAA) strategy, as published in 
//              Wouter J. Keller's paper 
//              'Relative and Absolute Momentum in Times of Rising/Low Yields:
//              Bold Asset Allocation (BAA)'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=4166845
// History:     2022xi21, FUB, created
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

// OPTIONAL_CHARTS
// if defined, render optional charts showing momentum
// in the offensive, defensive, and canary universes.
//#define OPTIONAL_CHARTS

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region BAA Core
    public abstract class Keller_BAA_Core : Algorithm
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA)";

        #region configuration
        //--- offensive configuration

        /// <summary>
        /// Offensive universe.
        /// </summary>
        public abstract List<string> SEL_O { get; set; }

        /// <summary>
        /// Number of offensive assets to hold.
        /// </summary>
        public virtual int TO { get; set; }

        //--- defensive configuration

        /// <summary>
        /// Defensive universe.
        /// </summary>
        public virtual List<string> SEL_D { get; set; } = new List<string>
        {
            ETF.TIP,
            ETF.DBC,
            ETF.BIL,
            ETF.IEF,
            ETF.TLT,
            ETF.LQD,
            ETF.BND,
        };

        /// <summary>
        /// Cash asset (defaults to BIL).
        /// </summary>
        public virtual string CASH { get; set; } = ETF.BIL;

        /// <summary>
        /// Number of defensive assets to hold.
        /// </summary>
        public virtual int TD { get; set; } = 3;

        //--- protective configuration

        /// <summary>
        /// Protective (canary) universe.
        /// </summary>
        public virtual List<string> SEL_P { get; set; } = new List<string>
        {
            ETF.SPY,
            ETF.VWO,
            ETF.VEA,
            ETF.BNDX,
        };

        /// <summary>
        /// Breadth parameter for protective universe.
        /// </summary>
        public virtual int B { get; set; } = 1;

        //--- misc configuration

        /// <summary>
        /// Transaction cost (default = 0.1%).
        /// </summary>
        //public virtual double TC { get; set; } = 0.1;

        public virtual OrderType ORDER_TYPE { get; set; } = OrderType.closeThisBar;

        public virtual string BENCH { get; set; } = Benchmark.PORTFOLIO_60_40;

        public virtual double RANK_MOM(string asset) =>
            Asset(asset).Close[0] / Asset(asset).Monthly().Close.SMA(13)[0] - 1.0;
        public virtual double PROT_MOM(string asset) =>
            new List<int> { 1, 3, 6, 12 }.Sum(m =>
                (12.0 / m) * (Asset(asset).Close[0] / Asset(asset).Monthly().Close[m] - 1.0));
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

            SimLoop(() =>
            {
                // Keller's strategy only trades once per month
                if (SimDate.Month != NextSimDate.Month || IsFirstBar)
                {
                    //----- qualify and rank assets
                    // rank offensive universe based on SMA(12) = SMA13
                    // NOTE: see paper, page 3, footnote 5:
                    //       Keller's SMA(12) is the average of the last 13 prices
                    var offensiveMom = SEL_O
                        .ToDictionary(
                            a => a,
                            a => RANK_MOM(a));

                    var offensiveAssets = offensiveMom
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => kv.Key)
                        .Take(TO)
                        .ToList();

                    // rank defensive assets based on SMA(12). In addition,
                    // disqualify any assets with a return lower than BIL.
                    // Note that we only use this absolute momentum filter
                    // for the defensive (but not the offensive) universe.
                    var defensiveMom = SEL_D
                        .ToDictionary(
                            a => a,
                            a => RANK_MOM(a));

                    var defensiveAsssets = defensiveMom
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => kv.Value >= defensiveMom[CASH] ? kv.Key : CASH)
                        .Take(TD)
                        .ToList();

                    //----- money management
                    // evaluate canary universe based on 13612W momentum
                    // see paper, page 4, footnote 7 for calculation
                    var canaryMom = SEL_P
                        .ToDictionary(
                            a => a,
                            a => PROT_MOM(a));

                    var numBadCanaryAssets = canaryMom
                        .Where(kv => kv.Value < 0.0)
                        .Count();

                    // calculate offensive and defensive holdings based
                    // on the breadth of the canary universe
                    var pcntDefensive = Math.Min(1.0, (double)numBadCanaryAssets / B);
                    var pcntOffensive = 1.0 - pcntDefensive;

                    // initially, assume to close all open positions
                    var weights = Positions
                        .ToDictionary(
                            kv => kv.Key,
                            kv => 0.0);

                    // assign weights for offensive and defensive assets.
                    // note how we add to previously existing allocations, as
                    // the offensive and defensive assets may overlap.
                    foreach (var a in offensiveAssets)
                        weights[a] = (weights.ContainsKey(a) ? weights[a] : 0.0) + pcntOffensive / TO;

                    foreach (var a in defensiveAsssets)
                        weights[a] = (weights.ContainsKey(a) ? weights[a] : 0.0) + pcntDefensive / TD;

                    //----- order management
                    // we assume that Keller's simulation trades on the close of the month,
                    // but the order type can be configured to trade on the first open of the month
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, ORDER_TYPE);

#if OPTIONAL_CHARTS
                    //----- optional charts (for debugging and analysis)
                    if (!IsOptimizing && Plotter.AllData.Count > 0)
                    {
                        Plotter.SelectChart("Offensive Universe", "Date");
                        Plotter.SetX(SimDate);
                        foreach (var kv in offensiveMom)
                            Plotter.Plot(Asset(kv.Key).Description, kv.Value);

                        Plotter.SelectChart("Defensive Universe", "Date");
                        Plotter.SetX(SimDate);
                        foreach (var kv in defensiveMom)
                            Plotter.Plot(Asset(kv.Key).Description, kv.Value);

                        Plotter.SelectChart("Canary Universe", "Date");
                        Plotter.SetX(SimDate);
                        foreach (var kv in canaryMom)
                            Plotter.Plot(Asset(kv.Key).Description, kv.Value);
                        Plotter.Plot("Offensive/ Defensive", numBadCanaryAssets == 0 ? SEL_P.Count : -SEL_P.Count);
                    }
#endif
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

    //--- main strategies
    #region BAA-G12 (balanced)
    public class Keller_BAA_G12 : Keller_BAA_Core
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA-G12)";
        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY,
            ETF.QQQ,
            ETF.IWM,
            ETF.VGK,
            ETF.EWJ,
            ETF.VWO,
            ETF.VNQ,
            ETF.DBC,
            ETF.GLD,
            ETF.TLT,
            ETF.HYG,
            ETF.LQD,
         };
        public override int TO => 6;
    }
    #endregion
    #region BAA-G4 (aggressive)
    public class Keller_BAA_G4 : Keller_BAA_Core
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA-G4)";
        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.QQQ,
            ETF.VWO,
            ETF.VEA,
            ETF.BND,
         };
        public override int TO => 1;
    }
    #endregion

    //--- strategy variants
    #region BAA-G12/T3 (less diversified balanced)
    public class Keller_BAA_G12T3 : Keller_BAA_G12
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA-G12/T3)";
        public override int TO => 3;
    }
    #endregion
    #region BAA-G4/T2 (less concentrated aggressive)
    public class Keller_BAA_G4T2 : Keller_BAA_G4
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA-G4/T2)";
        public override int TO => 2;
    }
    #endregion
    #region BAA-SPY (max simplicity)
    public class Keller_BAA_SPY : Keller_BAA_Core
    {
        public override string Name => "Keller's Bold Asset Allocation (BAA-SPY)";
        public override List<string> SEL_O { get; set; } = new List<string>
        {
            ETF.SPY,
        };
        public override int TO => 1;
    }
    #endregion
}

//==============================================================================
// end of file
