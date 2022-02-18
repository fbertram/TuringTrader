//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Heine_BondModel
// Description: Bond strategies as published in Mark Boucher's book
//              'The Hedge Fund Edge'.
// History:     2022i20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Solutions LLC
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

// FULL_RANGE: if defined, start simulation as early as possible (1968)
//#define FULL_RANGE

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace TuringTrader.BooksAndPubs
{
    // Heine Bond Model
    // Looks at five variables each week and compare them to their moving
    // average. Score +1 if bullish, and 0 if bearish. Next, total all five
    // variables. Result becomes bullish if total 3 or more and bearish if
    // less than 3. The variables are:
    // (1) Dow Jones 20 bond index and 24-wk MA. If above MA give
    //     +1 point, otherwise give 0 points.
    // (2) Long bond Treasury yield and 6-wk MA. If yield is below
    //     MA give +1 point, otherwise give 0 points.
    // (3) Thirteen-week T-bill yield and 6-wk MA. If yield is below
    //     MA, give +1 point, otherwise give 0 points.
    // (4) Dow Jones Utility Average and 10-wk MA. If Utility Average
    //     is above MA, give +1 point, otherwise give 0 points.
    // (5) CRB index and 20-wk MA. If CRB Index is below MA, give
    //     +1 point, otherwise give 0 points.
    #region Heine Bond Model core
    public abstract class Boucher_HeineBondModel_Core: AlgorithmPlusGlue
    {
        public override string Name => string.Format("Heine Bond Model");

        #region inputs
        // It is unclear how Heine envisions to trade various bonds,
        // we see two possibilities:
        // (1) we use BOND_IDX to time the bond market, but trade ASSET
        // (2) we use ASSET for timing and trade it as well
        // In our opinion only (2) makes sense, because we want to consider
        // the trade asset's price action. However, we tend to believe that
        // Heine envisioned option (1).
        public virtual object ASSET { get; set; } = Assets.IEF; 
        public virtual object BOND_IDX { get => ASSET; set { } } // Heine uses Dow Jones 20 Bond Price Index here?
        [OptimizerParam(30, 130, 5)]
        public virtual int BOND_PER { get; set; } = 24 * 5;

        public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS20"; // 20-year T-Bond Yield (since 01/1962)
        [OptimizerParam(20, 60, 5)]
        public virtual int BOND_LT_YIELD_PER { get; set; } = 6 * 5;

        public virtual string BOND_ST_YIELD { get; set; } = "fred:DTB3"; // 13-week T-Bill Yield (since 01/1954)
        [OptimizerParam(20, 60, 5)]
        public virtual int BOND_ST_YIELD_PER { get; set; } = 6 * 5;

        public virtual string UTILITY_INDEX { get; set; } = "$DJU"; // Dow Jones Utility Average (since 01/1929)
        [OptimizerParam(30, 90, 5)]
        public virtual int UTILITY_INDEX_PER { get; set; } = 10 * 5;

        //public virtual string COMMODITY_INDEX { get; set; } = "$CRB"; // Core Commodity CRB Index (since 01/1994)
        public virtual string COMMODITY_INDEX { get; set; } = "fred:PPIACO"; // producer price index to substitute CRB (since 01/1913)
        [OptimizerParam(50, 150, 5)]
        public virtual int COMMODITY_INDEX_PER { get; set; } = 20 * 5;

        public virtual object BENCHMARK { get => ASSET; set { } } // it is fair to benchmark agains the traded asset
        public virtual object SAFE { get; set; } = Assets.BIL; // it is unclear if Heine is rotating into a safe asset

        public virtual double COMMISSION { get; set; } = 0.0; // Heine is not mentioning commission
        protected virtual bool IsTradingDay => SimTime[0].DayOfWeek > NextSimTime.DayOfWeek;
        protected virtual bool IsTrendingUp(ITimeSeries<double> series, int period) => series.EMA(5)[0] > series.EMA(period)[0];
        protected virtual bool IsTrendingDown(ITimeSeries<double> series, int period) => !IsTrendingUp(series, period);
        #endregion
        #region strategy logic
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if FULL_RANGE
            StartTime = DateTime.Parse("01/01/1965");
            EndTime = Globals.END_TIME - TimeSpan.FromDays(5);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = COMMISSION;

            var bondAsset = AddDataSource(ASSET);
            var bondIndex = AddDataSource(BOND_IDX);
            var safeAsset = SAFE != null ? AddDataSource(SAFE) : null;
            var bondLtYield = AddDataSource(BOND_LT_YIELD);
            var bondStYield = AddDataSource(BOND_ST_YIELD);
            var utilityIndex = AddDataSource(UTILITY_INDEX);
            var commodityIndex = AddDataSource(COMMODITY_INDEX);
            var benchmark = AddDataSource(BENCHMARK);

            var allDs = new List<DataSource>
            {
                bondAsset,
                safeAsset ?? bondAsset,
                bondLtYield,
                bondStYield,
                utilityIndex,
                commodityIndex,
                benchmark,
            };

            var holdBonds = false;
            var holdBondsTimeStamp = StartTime;

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(allDs))
                    continue;

                var bondIndexRising = IsTrendingUp(bondIndex.Instrument.Close, BOND_PER) ? 1 : 0;
                var bondLtYieldFalling = IsTrendingDown(bondLtYield.Instrument.Close, BOND_LT_YIELD_PER) ? 1 : 0;
                var bondStYieldFalling = IsTrendingDown(bondStYield.Instrument.Close, BOND_ST_YIELD_PER) ? 1 : 0;
                var utilityIndexRising = IsTrendingUp(utilityIndex.Instrument.Close, UTILITY_INDEX_PER) ? 1 : 0;
                var commodityIndexFalling = IsTrendingDown(commodityIndex.Instrument.Close, COMMODITY_INDEX_PER) ? 1 : 0;

                var score = bondIndexRising + bondLtYieldFalling + bondStYieldFalling
                    + utilityIndexRising + commodityIndexFalling;
                var newHoldBonds = score >= 3;

#if true
                // this code reflects Boucher's book
                holdBonds = newHoldBonds;
#else
                // TuringTrader's modified method to reduce whipsaws
                if (holdBonds != newHoldBonds && (SimTime[0] - holdBondsTimeStamp).TotalDays > 10.0)
                {
                    holdBonds = newHoldBonds;
                    holdBondsTimeStamp = SimTime[0];
                }
#endif

                if (IsTradingDay)
                {
                    var bondWeight = holdBonds ? 1.0 : 0.0;
                    var bondShares = (int)Math.Floor(bondWeight * NetAssetValue[0] / bondAsset.Instrument.Close[0]);
                    bondAsset.Instrument.Trade(bondShares - bondAsset.Instrument.Position);
                    Alloc.Allocation[bondAsset.Instrument] = bondWeight;

                    if (safeAsset != null)
                    {
                        var safeWeight = 1.0 - bondWeight;
                        var safeShares = (int)Math.Floor(safeWeight * NetAssetValue[0] / safeAsset.Instrument.Close[0]);
                        safeAsset.Instrument.Trade(safeShares - safeAsset.Instrument.Position);
                        Alloc.Allocation[safeAsset.Instrument] = safeWeight;
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    //_plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));

                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Bond Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondAsset.Instrument.Name, bondAsset.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(bondAsset.Instrument.Name + " - MA", bondAsset.Instrument.Close.EMA(BOND_PER)[0]);

                    _plotter.SelectChart("Bond LT Yield", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondLtYield.Instrument.Name, bondLtYield.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(bondLtYield.Instrument.Name + " - MA", bondLtYield.Instrument.Close.EMA(BOND_LT_YIELD_PER)[0]);

                    _plotter.SelectChart("Bond ST Yield", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondStYield.Instrument.Name, bondStYield.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(bondStYield.Instrument.Name + " - MA", bondStYield.Instrument.Close.EMA(BOND_ST_YIELD_PER)[0]);

                    _plotter.SelectChart("Utility Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(utilityIndex.Instrument.Name, utilityIndex.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(utilityIndex.Instrument.Name + " - MA", utilityIndex.Instrument.Close.EMA(UTILITY_INDEX_PER)[0]);

                    _plotter.SelectChart("Commodity Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(commodityIndex.Instrument.Name, commodityIndex.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(commodityIndex.Instrument.Name + " - MA", commodityIndex.Instrument.Close.EMA(COMMODITY_INDEX_PER)[0]);

                    _plotter.SelectChart("Buy/ Sell Signals", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondAsset.Instrument.Name + " Rising", bondIndexRising + 14.0);
                    _plotter.Plot(bondLtYield.Instrument.Name + " Falling", bondLtYieldFalling + 12.0);
                    _plotter.Plot(bondStYield.Instrument.Name + " Falling", bondStYieldFalling + 10.0);
                    _plotter.Plot(utilityIndex.Instrument.Name + " Rising", utilityIndexRising + 8.0);
                    _plotter.Plot(commodityIndex.Instrument.Name + " Falling", commodityIndexFalling + 6.0);
                    _plotter.Plot("Score", score); // 2 - 7
                    _plotter.Plot("Hold Bonds", holdBonds ? 1 : 0);
                }

                var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
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
        }
        #endregion
    }
    #endregion
    #region Heine Bond Model instances
    public class Boucher_HeineBondModel_AGG : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (AGG)";
        public override object ASSET => Assets.AGG;
    }
    public class Boucher_HeineBondModel_SHY : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (SHY)";
        public override object ASSET => Assets.SHY;
    }
    public class Boucher_HeineBondModel_IEF: Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (IEF)";
        public override object ASSET => Assets.IEF;
    }
    public class Boucher_HeineBondModel_TLH : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (TLH)";
        public override object ASSET => Assets.TLH;
    }
    public class Boucher_HeineBondModel_TLT : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET => Assets.TLT;
    }
    public class Boucher_HeineBondModel_TIP : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (TIP)";
        public override object ASSET => Assets.TIP;
    }
    public class Boucher_HeineBondModel_LQD : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (LQD)";
        public override object ASSET => Assets.LQD;
    }
    public class Boucher_HeineBondModel_JNK : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (JNK)";
        public override object ASSET => Assets.JNK;
    }
    #endregion

    // Bond-Bill-Utility Model
    // Buy bonds when the 30-year government bond yield is below its 10-week
    // moving average and either
    // (a) the 3-month government T-bill yield is below its
    //     30-week moving average, or
    // (b) the Dow Jones Utility Average Index is above its
    //     10-week moving average.
    #region Bond-Bill-Utility Model core
    public abstract class Boucher_BondBillUtilityModel_Core : AlgorithmPlusGlue
    {
        public override string Name => string.Format("Bond-Bill-Utility Model");

        #region inputs
        public virtual object ASSET { get; set; } = Assets.IEF;

        public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS20"; // 20-year T-Bond Yield (since 01/1962)
        [OptimizerParam(50, 250, 5)]
        public virtual int BOND_LT_YIELD_PER { get; set; } = 10 * 5;

        public virtual string BOND_ST_YIELD { get; set; } = "fred:DTB3"; // 13-week T-Bill Yield (since 01/1954)
        [OptimizerParam(50, 250, 5)]
        public virtual int BOND_ST_YIELD_PER { get; set; } = 30 * 5;

        public virtual string UTILITY_INDEX { get; set; } = "$DJU"; // Dow Jones Utility Average (since 01/1929)
        [OptimizerParam(50, 250, 5)]
        public virtual int UTILITY_INDEX_PER { get; set; } = 10 * 5;

        public virtual object BENCHMARK { get => ASSET; set { } }
        public virtual object SAFE { get; set; } = Assets.BIL; // T-Bil as safe instrument

        public virtual double COMMISSION { get; set; } = 0.0; // Heine is not mentioning commission
        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
        protected virtual bool IsTrendingUp(ITimeSeries<double> series, int period) => series.EMA(5)[0] > series.EMA(period)[0];
        protected virtual bool IsTrendingDown(ITimeSeries<double> series, int period) => !IsTrendingUp(series, period);
        #endregion
        #region strategy logic
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if true
            //WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = DateTime.Parse("01/01/1965");
            EndTime = DateTime.Parse("01/16/2022");
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME - TimeSpan.FromDays(5);
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = COMMISSION;

            var bondAsset = AddDataSource(ASSET);
            var safeAsset = SAFE != null ? AddDataSource(SAFE) : null;
            var bondLtYield = AddDataSource(BOND_LT_YIELD);
            var bondStYield = AddDataSource(BOND_ST_YIELD);
            var utilityIndex = AddDataSource(UTILITY_INDEX);
            var benchmark = AddDataSource(BENCHMARK);

            var allDs = new List<DataSource>
            {
                bondAsset,
                safeAsset ?? bondAsset,
                bondLtYield,
                bondStYield,
                utilityIndex,
                benchmark,
            };

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(allDs))
                    continue;

                var bondLtYieldFalling = IsTrendingDown(bondLtYield.Instrument.Close, BOND_LT_YIELD_PER);
                var bondStYieldFalling = IsTrendingDown(bondStYield.Instrument.Close, BOND_ST_YIELD_PER);
                var utilityIndexRising = IsTrendingUp(utilityIndex.Instrument.Close, UTILITY_INDEX_PER);

                var holdBonds = bondLtYieldFalling && (bondStYieldFalling || utilityIndexRising);

                if (IsTradingDay)
                {
                    var bondWeight = holdBonds ? 1.0 : 0.0;
                    var bondShares = (int)Math.Floor(bondWeight * NetAssetValue[0] / bondAsset.Instrument.Close[0]);
                    bondAsset.Instrument.Trade(bondShares - bondAsset.Instrument.Position);

                    var safeWeight = 1.0 - bondWeight;
                    var safeShares = (int)Math.Floor(safeWeight * NetAssetValue[0] / safeAsset.Instrument.Close[0]);
                    safeAsset.Instrument.Trade(safeShares - safeAsset.Instrument.Position);
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    //_plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));
                    //if (Alloc.LastUpdate == SimTime[0])
                    //    _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Bond LT Yield", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondLtYield.Instrument.Name, bondLtYield.Instrument.Close[0]);
                    _plotter.Plot(bondLtYield.Instrument.Name + " - MA", bondLtYield.Instrument.Close.EMA(BOND_LT_YIELD_PER)[0]);

                    _plotter.SelectChart("Bond ST Yield", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondStYield.Instrument.Name, bondStYield.Instrument.Close[0]);
                    _plotter.Plot(bondStYield.Instrument.Name + " - MA", bondStYield.Instrument.Close.EMA(BOND_ST_YIELD_PER)[0]);

                    _plotter.SelectChart("Utility Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(utilityIndex.Instrument.Name, utilityIndex.Instrument.Close[0]);
                    _plotter.Plot(utilityIndex.Instrument.Name + " - MA", utilityIndex.Instrument.Close.EMA(UTILITY_INDEX_PER)[0]);

                    _plotter.SelectChart("Buy/ Sell Signals", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondLtYield.Instrument.Name + " Falling", bondLtYieldFalling ? 7.0 : 6.0);
                    _plotter.Plot(bondStYield.Instrument.Name + " Falling", bondStYieldFalling ? 5.0 : 4.0);
                    _plotter.Plot(utilityIndex.Instrument.Name + " Rising", utilityIndexRising ? 3.0 : 2.0);
                    _plotter.Plot("Hold Bonds", holdBonds ? 1.0 : 0.0);
                }

                var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
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
        }
        #endregion
    }
    #endregion
    #region Bond-Bill-Utility Model instances
    public class Boucher_BondBillUtilityModel_AGG : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (AGG)";
        public override object ASSET => Assets.AGG;
    }
    public class Boucher_BondBillUtilityModel_SHY : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (SHY)";
        public override object ASSET => Assets.SHY;
    }
    public class Boucher_BondBillUtilityModel_IEF : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (IEF)";
        public override object ASSET => Assets.IEF;
    }
    public class Boucher_BondBillUtilityModel_TLH : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (TLH)";
        public override object ASSET => Assets.TLH;
    }
    public class Boucher_BondBillUtilityModel_TLT : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET => Assets.TLT;
    }
    public class Boucher_BondBillUtilityModel_LQD : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (LQD)";
        public override object ASSET => Assets.LQD;
    }
    public class Boucher_BondBillUtilityModel_JNK : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (JNK)";
        public override object ASSET => Assets.JNK;
    }
    #endregion
}
