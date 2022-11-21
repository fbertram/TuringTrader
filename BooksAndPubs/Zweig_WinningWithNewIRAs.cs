//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Zweig_WinningWithNewIRAs
// Description: Strategies as described in Martin Zweig's book
//              'Winning With New IRAs'.
//              This implementation takes some modifications from
//              Ned Davis's book 'Being Right Or Making Money' into account.
// History:     2022ii14, FUB, created
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

// USE_FULL_RANGE: if defined, start in 1967
#define USE_FULL_RANGE

// NED_DAVIS_MOD: if defined, implement modification according to Ned Davis 2014
#define NED_DAVIS_MOD

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Optimizer;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region Zweig Bond Model core
    abstract public class Zweig_BondModel : AlgorithmPlusGlue
    {
        public override string Name => "Zweig's Bond Trading Model";

        #region inputs
        public virtual object ASSET { get; set; } = null; // Zweig uses Dow Jones 20 Bond Average
        public virtual object SAFE { get; set; } = Assets.BIL;
        public virtual object DISCOUNT_RATE { get; set; } = "%FFYE"; // 30-day Federal Funds Rate (Zweig uses Discount Rate)
        public virtual object ST_RATE { get; set; } = "%FFYE"; // 30-day Federal Funds Rate (Zweig uses 90-day Commercial Paper Rate)
        public virtual object LT_RATE { get; set; } = "fred:AAA"; // Moody's AAA Corporate Bond Rate

        [OptimizerParam(10, 100, 10)]
        public virtual int TAPE_IND_A_BPTS { get; set; } = 60; // 0.6%
        [OptimizerParam(100, 1000, 25)]
        public virtual int TAPE_IND_B_BPTS { get; set; } = 180; // 1.8%
        [OptimizerParam(20, 100, 10)]
        public virtual int RATE_IND_BPTS { get; set; } = 50; // 0.5%

        [OptimizerParam(20, 100, 10)]
        public virtual int CURVE_IND_BULL_BPTS { get; set; } = 60; // 0.6%
        [OptimizerParam(-50, -10, 10)]
        public virtual int CURVE_IND_BEAR_BPTS { get; set; } = -20; // -0.2%
        [OptimizerParam(1, 3, 1)]
        public virtual int BUY_SELL_THRS { get; set; } = 3; // Zweig's book says 3. Ned Davis corrects this to 1.
        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
        #endregion
        #region strategy logic
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            #region initialization
#if USE_FULL_RANGE
            StartTime = startTime ?? DateTime.Parse("01/01/1965", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(180);
            //EndTime = DateTime.Parse("12/31/1986", CultureInfo.InvariantCulture);
            EndTime = endTime ?? DateTime.Now.Date - TimeSpan.FromDays(5);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Zweig is not considering commissions

            // tape indicators
            var asset = AddDataSource(ASSET);
            var tapeIndicatorABuy = false;
            var tapeIndicatorARef = (double?)null;
            var tapeIndicatorBBuy = false;
            var tapeIndicatorBRef = (double?)null;

            // additional trend indicator according to Ned Davis 2014
            var trendIndicatorBuy = false;

            // discount rate indicator
            var fedRate = AddDataSource(DISCOUNT_RATE); // Zweig uses Fed's discount rate
            var rateIndicatorBuy = false;
            var rateIndicatorRef = (double?)null;

            // yield curve indicator
            var stRate = AddDataSource(ST_RATE);
            var ltRate = AddDataSource(LT_RATE);
            var curveIndicatorBuy = false;
            var curveIndicatorSell = false;


            // aggregate buy/ sell signal
            var safe = SAFE != null ? AddDataSource(SAFE) : null;
            int combinedScore = 0;
            var aggregateBuy = false;
            #endregion
            #region simulation loop
            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(new List<DataSource> { asset, fedRate, stRate, ltRate }))
                    continue;

                if (safe != null && !HasInstrument(safe))
                    continue;

                var assetSma50 = asset.Instrument.Close.EMA(50);
                var filteredRate = fedRate.Instrument.Close.EMA(10).EMA(10); // Zweig does not filter
                var yieldCurve = ltRate.Instrument.Close.Subtract(stRate.Instrument.Close).EMA(10).EMA(10); // Zweig does not filter

                if (IsTradingDay)
                {
                    #region Tape Indicator A
                    if (tapeIndicatorARef == null)
                        tapeIndicatorARef = asset.Instrument.Close[0];

                    if (tapeIndicatorABuy)
                    {
                        // sell after 0.6% decline
                        if (asset.Instrument.Close[0] <= tapeIndicatorARef * (1.0 - TAPE_IND_A_BPTS / 10000.0))
                        {
                            tapeIndicatorABuy = false;
                            tapeIndicatorARef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorARef = Math.Max((double)tapeIndicatorARef, asset.Instrument.Close[0]);
                        }
                    }
                    else
                    {
                        // buy after 0.6% rise
                        if (asset.Instrument.Close[0] >= tapeIndicatorARef * (1.0 + TAPE_IND_A_BPTS / 10000.0))
                        {
                            tapeIndicatorABuy = true;
                            tapeIndicatorARef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorARef = Math.Min((double)tapeIndicatorARef, asset.Instrument.Close[0]);
                        }
                    }
                    #endregion
                    #region Tape Indicator B
                    if (tapeIndicatorBRef == null)
                        tapeIndicatorBRef = asset.Instrument.Close[0];

                    if (tapeIndicatorBBuy)
                    {
                        // sell after 1.8% decline
                        if (asset.Instrument.Close[0] <= tapeIndicatorBRef * (1.0 - TAPE_IND_B_BPTS / 10000.0))
                        {
                            tapeIndicatorBBuy = false;
                            tapeIndicatorBRef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorBRef = Math.Max((double)tapeIndicatorBRef, asset.Instrument.Close[0]);
                        }
                    }
                    else
                    {
                        // buy after 1.8% rise
                        if (asset.Instrument.Close[0] >= tapeIndicatorBRef * (1.0 + TAPE_IND_B_BPTS / 10000.0))
                        {
                            tapeIndicatorBBuy = true;
                            tapeIndicatorBRef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorBRef = Math.Min((double)tapeIndicatorBRef, asset.Instrument.Close[0]);
                        }
                    }
                    #endregion
                    #region Trend Indicator (Ned Davis 2014)
                    trendIndicatorBuy = trendIndicatorBuy
                        ? asset.Instrument.Close[0] > 0.99 * assetSma50[0]
                        : asset.Instrument.Close[0] > 1.01 * assetSma50[0];
                    #endregion
                    #region Discount Rate Indicator
                    if (rateIndicatorRef == null)
                        rateIndicatorRef = filteredRate[0];

                    if (rateIndicatorBuy)
                    {
                        // sell after 0.5% rate increase
                        if (filteredRate[0] >= rateIndicatorRef + RATE_IND_BPTS / 100.0)
                        {
                            rateIndicatorBuy = false;
                            tapeIndicatorBRef = filteredRate[0];
                        }
                        else
                        {
                            rateIndicatorRef = Math.Min((double)rateIndicatorRef, filteredRate[0]);
                        }
                    }
                    else
                    {
                        // buy after 0.5% rate decrease
                        if (filteredRate[0] <= rateIndicatorRef - RATE_IND_BPTS / 100.0)
                        {
                            rateIndicatorBuy = true;
                            rateIndicatorRef = filteredRate[0];
                        }
                        else
                        {
                            rateIndicatorRef = Math.Max((double)rateIndicatorRef, filteredRate[0]);
                        }
                    }
                    #endregion
                    #region Yield Curve Indicator
                    curveIndicatorBuy = yieldCurve[0] >= CURVE_IND_BULL_BPTS / 100.0;
                    curveIndicatorSell = yieldCurve[0] <= CURVE_IND_BEAR_BPTS / 100.0;
                    #endregion

                    #region place orders
                    combinedScore = (tapeIndicatorABuy ? 1 : -1)
                        + (tapeIndicatorBBuy ? 1 : -1)
#if NED_DAVIS_MOD
                    + (trendIndicatorBuy ? 1 : -1)
#endif
                    + (rateIndicatorBuy ? 1 : -1)
                        + (curveIndicatorBuy ? 1 : 0) + (curveIndicatorSell ? 0 : -1);

#if true
                    if (aggregateBuy)
                    {
                        // sell, if score reaches -3
                        if (combinedScore <= -BUY_SELL_THRS)
                            aggregateBuy = false;
                    }
                    else
                    {
                        // buy, if score reaches +3
                        if (combinedScore >= BUY_SELL_THRS)
                            aggregateBuy = true;
                    }
#endif

                    var assetWeight = aggregateBuy ? 1.0 : 0.0;
                    var assetShares = (int)Math.Floor(assetWeight * NetAssetValue[0] / asset.Instrument.Close[0]);
                    asset.Instrument.Trade(assetShares - asset.Instrument.Position);

                    if (safe != null)
                    {
                        var safeWeight = 1.0 - assetWeight;
                        var safeShares = (int)Math.Floor(safeWeight * NetAssetValue[0] / safe.Instrument.Close[0]);
                        safe.Instrument.Trade(safeShares - safe.Instrument.Position);
                    }
                    #endregion
                }

                #region output
                var p = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
                    p, p, p, p, 0);

                if (TradingDays > 0.0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, asset.Instrument);
                    _plotter.AddStrategyHoldings(this, safe != null ? new List<Instrument> { asset.Instrument, safe.Instrument } : new List<Instrument> { asset.Instrument });

                    _plotter.SelectChart("Tape Indicator A", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Buy Signal", tapeIndicatorABuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Tape Indicator B", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Buy Signal", tapeIndicatorBBuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Trend Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Log Price - SMA", Math.Log(assetSma50[0]));
                    _plotter.Plot("Buy Signal", trendIndicatorBuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Discount Rate Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Interest Rate", filteredRate[0]);
                    //_plotter.Plot("Interest Rate Ref", (double)rateIndicatorRef);
                    _plotter.Plot("Buy Signal", rateIndicatorBuy ? 3.0 : 0.0);

                    _plotter.SelectChart("Yield Curve Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Yield Spread", yieldCurve[0]);
                    _plotter.Plot("Buy Signal", (curveIndicatorBuy ? 1.0 : 0.0) + (curveIndicatorSell ? -1.0 : 0.0));

                    _plotter.SelectChart("Combine Indicators", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Tape A", (tapeIndicatorABuy ? 1.0 : 0.0) + 14.0);
                    _plotter.Plot("Tape B", (tapeIndicatorBBuy ? 1.0 : 0.0) + 12.0);
                    _plotter.Plot("Trend Indicator", (trendIndicatorBuy ? 1.0 : 0.0) + 10.0);
                    _plotter.Plot("Discount Rate", (rateIndicatorBuy ? 1.0 : 0.0) + 8.0);
                    _plotter.Plot("Yield Curve", (curveIndicatorBuy ? 0.5 : 0.0) + (curveIndicatorSell ? 0.0 : 0.5) + 6.0);
                    _plotter.Plot("Total Score", combinedScore);
                    _plotter.Plot("Total Buy", aggregateBuy ? 1.0 : 0.0);
                }
                #endregion
            }
            #endregion
            #region post-processing
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

            FitnessValue = this.CalcFitness();
            #endregion
        }
        #endregion
    }
    #endregion
    #region Zweig Bond Model instances
    public class Zweig_Bond_Model_AGG : Zweig_BondModel
    {
        public override string Name => base.Name + " (AGG)";
        public override object ASSET { get; set; } = Assets.AGG;
    }
    public class Zweig_Bond_Model_LQD : Zweig_BondModel
    {
        public override string Name => base.Name + " (LQD)";
        public override object ASSET { get; set; } = Assets.LQD;
    }
    public class Zweig_Bond_Model_JNK : Zweig_BondModel
    {
        public override string Name => base.Name + " (JNK)";
        public override object ASSET { get; set; } = Assets.JNK;
    }
    public class Zweig_Bond_Model_TLT : Zweig_BondModel
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET { get; set; } = Assets.TLT;
    }
    public class Zweig_Bond_Model_IEF : Zweig_BondModel
    {
        public override string Name => base.Name + " (IEF)";
        public override object ASSET { get; set; } = Assets.IEF;
    }
    public class Zweig_Bond_Model_SHY : Zweig_BondModel
    {
        public override string Name => base.Name + " (SHY)";
        public override object ASSET { get; set; } = Assets.SHY;
    }
    #endregion
}

//==============================================================================
// end of file
