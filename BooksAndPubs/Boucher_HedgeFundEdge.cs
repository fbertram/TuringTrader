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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace Algorithms.TTorg
{
    // Heine Bond Model
    // Looks at five variables each week and compare
    // them to their moving average. Score +1 if bullish,
    // and 0 if bearish. Next, total all five variables.
    // Result becomes bullish if total 3 or more and
    // bearish if less than 3. The variables are:
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

        public virtual object BOND { get; set; } = Assets.IEF; // since 01/1968

        public virtual object BOND_INDEX { get => BOND; set { } } // Heine uses Dow Jones 20 Bond Price Index
        public virtual int BOND_INDEX_PER { get; set; } = 24 * 5;

        //public virtual string BOND_LT_YIELD { get; set; } = "%TYX"; // 30-year T-Bond Yield (since 11/1993)
        //public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS30"; // 30-year T-Bond Yield (since 02/1977)
        public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS20"; // 20-year T-Bond Yield (since 01/1962)
        public virtual int BOND_LT_YIELD_PER { get; set; } = 6 * 5;

        public virtual string BOND_ST_YIELD { get; set; } = "fred:DTB3"; // 13-week T-Bill Yield (since 01/1954)
        public virtual int BOND_ST_YIELD_PER { get; set; } = 6 * 5;

        public virtual string UTILITY_INDEX { get; set; } = "$DJU"; // Dow Jones Utility Average (since 01/1929)
        public virtual int UTILITY_INDEX_PER { get; set; } = 10 * 5;

        //public virtual string COMMODITY_INDEX { get; set; } = "$CRB"; // Core Commodity CRB Index (since 01/1994)
        public virtual string COMMODITY_INDEX { get; set; } = "fred:PPIACO"; // producer price index to substitute CRB (since 01/1913)
        public virtual int COMMODITY_INDEX_PER { get; set; } = 20 * 5;

        public virtual object ASSET { get => BOND; set { } } // it is unclear, which asset Heine is trading
        public virtual object BENCHMARK { get => BOND; set { } } // it is fair to benchmark agains the traded asset

        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
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
            //CommissionPerShare = Globals.COMMISSION;

            var bondIndex = AddDataSource(BOND_INDEX);
            var bondLtYield = AddDataSource(BOND_LT_YIELD);
            var bondStYield = AddDataSource(BOND_ST_YIELD);
            var utilityIndex = AddDataSource(UTILITY_INDEX);
            var commodityIndex = AddDataSource(COMMODITY_INDEX);

            var asset = AddDataSource(ASSET);
            var bench = AddDataSource(BENCHMARK);

            var allDs = new List<DataSource>
            {
                bondIndex,
                bondLtYield,
                bondStYield,
                utilityIndex,
                commodityIndex,
                asset,
                bench,
            };

            var holdBonds = false;
            var holdBondsTimeStamp = StartTime;

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(allDs))
                    continue;

#if true
                // this code matches Boucher's book
                bool IsTrendingUp(ITimeSeries<double> series, int period) => series.EMA(5)[0] > series.EMA(period)[0];
                bool IsTrendingDown(ITimeSeries<double> series, int period) => series.EMA(5)[0] < series.EMA(period)[0];
#else
                // TuringTrader's modified method
                bool IsTrendingUp(ITimeSeries<double> series, int period) => series.Return().EMA(3).EMA(3).EMA(period)[0] > 0.0;
                bool IsTrendingDown(ITimeSeries<double> series, int period) => series.Return().EMA(3).EMA(3).EMA(period)[0] < 0.0;
#endif

                var bondIndexRising = IsTrendingUp(bondIndex.Instrument.Close, BOND_INDEX_PER) ? 1 : 0;
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
                    var weight = holdBonds ? 1.0 : 0.0;
                    var shares = (int)Math.Floor(weight * NetAssetValue[0] / asset.Instrument.Close[0]);
                    asset.Instrument.Trade(shares - asset.Instrument.Position);
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    //_plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));
                    //if (Alloc.LastUpdate == SimTime[0])
                    //    _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Bond Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondIndex.Instrument.Name, bondIndex.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(bondIndex.Instrument.Name + " - MA", bondIndex.Instrument.Close.EMA(BOND_INDEX_PER)[0]);

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
                    _plotter.Plot(bondIndex.Instrument.Name + " Rising", bondIndexRising + 16.0);
                    _plotter.Plot(bondLtYield.Instrument.Name + " Falling", bondLtYieldFalling + 14.0);
                    _plotter.Plot(bondStYield.Instrument.Name + " Falling", bondStYieldFalling + 12.0);
                    _plotter.Plot(utilityIndex.Instrument.Name + " Rising", utilityIndexRising + 10.0);
                    _plotter.Plot(commodityIndex.Instrument.Name + " Falling", commodityIndexFalling + 8.0);
                    _plotter.Plot("Score", score + 2); // 2 - 7
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
    }
    #endregion
    #region Heine Bond Model instances
    public class Boucher_HeineBondModel_AGG : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (AGG)";
        public override object BOND => Assets.AGG; // since 01/1968
    }
    public class Boucher_HeineBondModel_SHY : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (SHY)";
        public override object BOND => Assets.SHY; // since 01/1968
    }
    public class Boucher_HeineBondModel_IEF : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (IEF)";
        public override object BOND => Assets.IEF; // since 01/1968
    }

    public class Boucher_HeineBondModel_TLT : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object BOND => Assets.TLT; // since 01/1968
    }

    public class Boucher_HeineBondModel_LQD : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (LQD)";
        public override object BOND => Assets.LQD; // since 01/1968
    }

    public class Boucher_HeineBondModel_JNK : Boucher_HeineBondModel_Core
    {
        public override string Name => base.Name + " (JNK)";
        public override object BOND => Assets.JNK; // since 05/2007
    }
    #endregion

    // Bond-Bill-Utility Model
    // Buy bonds when the 30-year government bond yield is
    // below its 10-week moving average and either
    // (a) the 3-month government T-bill yield is below its
    //     30-week moving average, or
    // (b) the Dow Jones Utility Average Index is above its
    //     10-week moving average.
    #region Bond-Bill-Utility Model core
    public abstract class Boucher_BondBillUtilityModel_Core : AlgorithmPlusGlue
    {
        public override string Name => string.Format("Bond-Bill-Utility Model");

        public virtual object BOND { get; set; } = Assets.IEF; // since 01/1968

        public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS20"; // 20-year T-Bond Yield (since 01/1962)
        [OptimizerParam(50, 250, 5)]
        public virtual int BOND_LT_YIELD_PER { get; set; } = 10 * 5;

        public virtual string BOND_ST_YIELD { get; set; } = "fred:DTB3"; // 13-week T-Bill Yield (since 01/1954)
        [OptimizerParam(50, 250, 5)]
        public virtual int BOND_ST_YIELD_PER { get; set; } = 30 * 5;

        public virtual string UTILITY_INDEX { get; set; } = "$DJU"; // Dow Jones Utility Average (since 01/1929)
        [OptimizerParam(50, 250, 5)]
        public virtual int UTILITY_INDEX_PER { get; set; } = 10 * 5;

        public virtual object ASSET { get => BOND; set { } }
        public virtual object BENCHMARK { get => BOND; set { } }

        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
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
            //CommissionPerShare = Globals.COMMISSION;

            var bondLtYield = AddDataSource(BOND_LT_YIELD);
            var bondStYield = AddDataSource(BOND_ST_YIELD);
            var utilityIndex = AddDataSource(UTILITY_INDEX);

            var asset = AddDataSource(ASSET);
            var bench = AddDataSource(BENCHMARK);

            var allDs = new List<DataSource>
            {
                bondLtYield,
                bondStYield,
                utilityIndex,
                asset,
                bench,
            };

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(allDs))
                    continue;

#if true
                // this is the tend-detection method used int the book
                bool IsTrendingDown(ITimeSeries<double> series, int period) => series[0] < series.EMA(period)[0];
                bool IsTrendingUp(ITimeSeries<double> series, int period) => series[0] > series.EMA(period)[0];
#else
                // TuringTrader's modified code
                //bool IsTrendingDown(ITimeSeries<double> series, int period) => series.Return().EMA(period / 4).EMA(period / 2).EMA(period)[0] < 0.0;
                //bool IsTrendingUp(ITimeSeries<double> series, int period) => series.Return().EMA(period / 4).EMA(period / 2).EMA(period)[0] > 0.0;
#endif

                var bondLtYieldFalling = IsTrendingDown(bondLtYield.Instrument.Close, BOND_LT_YIELD_PER);
                var bondStYieldFalling = IsTrendingDown(bondStYield.Instrument.Close, BOND_ST_YIELD_PER);
                var utilityIndexRising = IsTrendingUp(utilityIndex.Instrument.Close, UTILITY_INDEX_PER);

                var holdBonds = bondLtYieldFalling && (bondStYieldFalling || utilityIndexRising);

                if (IsTradingDay)
                {
                    var weight = holdBonds ? 1.0 : 0.0;
                    var shares = (int)Math.Floor(weight * NetAssetValue[0] / asset.Instrument.Close[0]);
                    asset.Instrument.Trade(shares - asset.Instrument.Position);
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
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
    }
    #endregion
    #region Bond-Bill-Utility Model instances
    public class Boucher_BondBillUtilityModel_AGG : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (AGG)";
        public override object BOND => Assets.AGG; // since 01/1968
    }
    public class Boucher_BondBillUtilityModel_SHY : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (SHY)";
        public override object BOND => Assets.SHY; // since 01/1968
    }
    public class Boucher_BondBillUtilityModel_IEF : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (IEF)";
        public override object BOND => Assets.IEF; // since 01/1968
    }
    public class Boucher_BondBillUtilityModel_TLT : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object BOND => Assets.TLT; // since 01/1968
    }
    public class Boucher_BondBillUtilityModel_LQD : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (LQD)";
        public override object BOND => Assets.LQD; // since 01/1968
    }
    public class Boucher_BondBillUtilityModel_JNK : Boucher_BondBillUtilityModel_Core
    {
        public override string Name => base.Name + " (JNK)";
        public override object BOND => Assets.JNK; // since 05/2007
    }
    #endregion


    #region experimental code

    public class TTcom_HeineBondModel_Core : AlgorithmPlusGlue
    {
        public override string Name => string.Format("Heine Bond Model");

        public virtual object BOND { get; set; } = Assets.IEF; // since 01/1968

        public virtual object BOND_INDEX { get => BOND; set { } } // Heine uses Dow Jones 20 Bond Price Index
        public virtual int BOND_INDEX_PER { get; set; } = 24 * 5;
        [OptimizerParam(25, 200, 5)]
        public virtual int BOND_INDEX_WGT { get; set; } = 100;

        //public virtual string BOND_LT_YIELD { get; set; } = "%TYX"; // 30-year T-Bond Yield (since 11/1993)
        //public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS30"; // 30-year T-Bond Yield (since 02/1977)
        public virtual string BOND_LT_YIELD { get; set; } = "fred:DGS20"; // 20-year T-Bond Yield (since 01/1962)
        public virtual int BOND_LT_YIELD_PER { get; set; } = 6 * 5;
        [OptimizerParam(25, 200, 5)]
        public virtual int BOND_LT_YIELD_WGT { get; set; } = 100;

        public virtual string BOND_ST_YIELD { get; set; } = "fred:DTB3"; // 13-week T-Bill Yield (since 01/1954)
        public virtual int BOND_ST_YIELD_PER { get; set; } = 6 * 5;
        [OptimizerParam(25, 200, 5)]
        public virtual int BOND_ST_YIELD_WGT { get; set; } = 100;

        public virtual string UTILITY_INDEX { get; set; } = "$DJU"; // Dow Jones Utility Average (since 01/1929)
        public virtual int UTILITY_INDEX_PER { get; set; } = 10 * 5;
        [OptimizerParam(25, 200, 5)]
        public virtual int UTILITY_INDEX_WGT { get; set; } = 100;

        //public virtual string COMMODITY_INDEX { get; set; } = "$CRB"; // Core Commodity CRB Index (since 01/1994)
        public virtual string COMMODITY_INDEX { get; set; } = "fred:PPIACO"; // producer price index to substitute CRB (since 01/1913)
        public virtual int COMMODITY_INDEX_PER { get; set; } = 20 * 5;
        [OptimizerParam(25, 200, 5)]
        public virtual int COMMODITY_INDEX_WGT { get; set; } = 100;

        public virtual object ASSET { get => BOND; set { } } // it is unclear, which asset Heine is trading
        public virtual object BENCHMARK { get => BOND; set { } } // it is fair to benchmark agains the traded asset

        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
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
            //CommissionPerShare = Globals.COMMISSION;

            var bondIndex = AddDataSource(BOND_INDEX);
            var bondLtYield = AddDataSource(BOND_LT_YIELD);
            var bondStYield = AddDataSource(BOND_ST_YIELD);
            var utilityIndex = AddDataSource(UTILITY_INDEX);
            var commodityIndex = AddDataSource(COMMODITY_INDEX);

            var asset = AddDataSource(ASSET);
            var bench = AddDataSource(BENCHMARK);

            var allDs = new List<DataSource>
            {
                bondIndex,
                bondLtYield,
                bondStYield,
                utilityIndex,
                commodityIndex,
                asset,
                bench,
            };

            var holdBonds = false;
            var holdBondsTimeStamp = StartTime;

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(allDs))
                    continue;

                double Trend(ITimeSeries<double> series, int period) => series.LogReturn().EMA(3).EMA(3).EMA(period)[0];

                var bondIndexRising = BOND_INDEX_WGT/100.0 * Trend(bondIndex.Instrument.Close, BOND_INDEX_PER);
                var bondLtYieldFalling = -BOND_LT_YIELD_WGT / 100.0 * Trend(bondLtYield.Instrument.Close, BOND_LT_YIELD_PER);
                var bondStYieldFalling = -BOND_ST_YIELD_WGT / 100.0 * Trend(bondStYield.Instrument.Close, BOND_ST_YIELD_PER);
                var utilityIndexRising = UTILITY_INDEX_WGT/100.0 * Trend(utilityIndex.Instrument.Close, UTILITY_INDEX_PER);
                var commodityIndexFalling = -COMMODITY_INDEX_WGT/100.0 * Trend(commodityIndex.Instrument.Close, COMMODITY_INDEX_PER);

                var score = bondIndexRising + bondLtYieldFalling + bondStYieldFalling
                    + utilityIndexRising + commodityIndexFalling;
                var newHoldBonds = score >= 0;

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
                    var weight = holdBonds ? 1.0 : 0.0;
                    var shares = (int)Math.Floor(weight * NetAssetValue[0] / asset.Instrument.Close[0]);
                    asset.Instrument.Trade(shares - asset.Instrument.Position);
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    //_plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));
                    //if (Alloc.LastUpdate == SimTime[0])
                    //    _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Bond Index", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(bondIndex.Instrument.Name, bondIndex.Instrument.Close.EMA(5)[0]);
                    _plotter.Plot(bondIndex.Instrument.Name + " - MA", bondIndex.Instrument.Close.EMA(BOND_INDEX_PER)[0]);

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
                    _plotter.Plot(bondIndex.Instrument.Name + " Rising", 10.0 * bondIndexRising + 12.0);
                    _plotter.Plot(bondLtYield.Instrument.Name + " Falling", 10.0 * bondLtYieldFalling + 10.0);
                    _plotter.Plot(bondStYield.Instrument.Name + " Falling", 10.0 * bondStYieldFalling + 8.0);
                    _plotter.Plot(utilityIndex.Instrument.Name + " Rising", 10.0 * utilityIndexRising + 6.0);
                    _plotter.Plot(commodityIndex.Instrument.Name + " Falling", 10.0 * commodityIndexFalling + 4.0);
                    _plotter.Plot("Score", 10.0 * score + 2); // 2 - 7
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
    }

    #endregion

}
