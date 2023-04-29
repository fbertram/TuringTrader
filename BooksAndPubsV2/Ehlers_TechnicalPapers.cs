//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Ehlers_RocketScienceForTraders_v2.cs
// Description: Strategies as published by John F Ehlers at
//              https://mesasoftware.com/TechnicalArticles.htm
// History:     2023iii31, FUB, created
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

// NOTE: this code looks very different from typical TuringTrader code
//       because we ported it verbatim from John F Ehlers's code written
//       for TradeStation/ EasyLanguage.
//       this way, we can be sure our implementation is true to 
//       the original publication.

#region libraries
using System;
using System.Collections.Generic;
using TuringTrader.GlueV2;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region correlation as a trend indicator
    /// <summary>
    /// Experimental algorithm using correlation as a trend indicator.
    /// <see href="https://www.mesasoftware.com/papers/CORRELATION%20AS%20A%20TREND%20INDICATOR.pdf"/>
    /// </summary>
    public abstract class Ehlers_TechnicalPapers_CorrelationAsTrendIndicator_Core : Algorithm
    {
        public override string Name => "Ehlers' Correlation As A Trend Indicator";

        #region inputs
        public virtual object ASSET { get; set; } = ETF.SPY;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 0;

        [OptimizerParam(63, 252, 21)]
        public virtual int CORR_PERIOD { get; set; } = 252;
        [OptimizerParam(20, 95, 5)]
        public virtual int CORR_ENTRY { get; set; } = 50;
        [OptimizerParam(0, 75, 5)]
        public virtual int CORR_EXIT { get; set; } = 0;

        public override bool IsOptimizerParamsValid => CORR_ENTRY >= CORR_EXIT;
        #endregion
        public override void Run()
        {
            //========== initialization ==========

            //StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            StartDate = StartDate ?? DateTime.Parse("1970-01-01T16:00-05:00");
            EndDate = EndDate ?? AlgorithmConstants.END_DATE - TimeSpan.FromDays(5);
            WarmupPeriod = TimeSpan.FromDays(365);
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            var asset = Asset(ASSET);

            var ramp = Asset("ramp", () =>
            {
                var bars = new List<BarType<OHLCV>>();

                var y = 0.0;
                foreach (var t in TradingCalendar.TradingDays)
                {
                    y += 1.0;

                    bars.Add(new BarType<OHLCV>(
                        t, new OHLCV(y, y, y, y, 0.0)));
                }

                return bars;
            });

            //========== simulation loop ==========

            SimLoop(() =>
            {
                var correlation = asset.Close.Correlation(ramp.Close, CORR_PERIOD);

                // enter and exit positions with hysteresis
                var targetAllocation = Lambda("allocation", (prev) =>
                {
                    if (prev <= 0.0 && correlation[0] > CORR_ENTRY / 100.0)
                        return ALLOW_LONG != 0 ? 1.0 : 0.0;

                    if (prev >= 0.0 && correlation[0] < -CORR_ENTRY / 100.0)
                        return ALLOW_SHORT != 0 ? -1.0 : 0.0;

                    if (prev > 0.0 && correlation[0] < CORR_EXIT / 100.0
                    || prev < 0.0 && correlation[0] > -CORR_EXIT / 100.0)
                        return 0.0;

                    return prev;
                }, 0.0)[0];

                if (Math.Abs(targetAllocation - asset.Position) > 0.10)
                    asset.Allocate(targetAllocation, OrderType.openNextBar);

                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("buy & hold", asset.Close[0]);

#if true
                    // optional charts
                    Plotter.SelectChart("Correlation", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("price", Math.Log(asset.Close[0] / asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("correlation", correlation[0]);
                    Plotter.Plot("long entry", CORR_ENTRY / 100.0);
                    Plotter.Plot("long exit", CORR_EXIT / 100.0);
                    Plotter.Plot("short entry", -CORR_ENTRY / 100.0);
                    Plotter.Plot("short exit", -CORR_EXIT / 100.0);
#endif
                }
            });

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
    }

    public class Ehlers_TechnicalPapers_CorrelationAsTrendIndicator_SPY : Ehlers_TechnicalPapers_CorrelationAsTrendIndicator_Core
    {
        public override string Name => base.Name + " (SPY)";
        public override object ASSET { get; set; } = ETF.SPY;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 0;
        public override int CORR_PERIOD { get; set; } = 252;
        public override int CORR_ENTRY { get; set; } = 25;
        public override int CORR_EXIT { get; set; } = 0;
    }
    public class Ehlers_TechnicalPapers_CorrelationAsTrendIndicator_TLT : Ehlers_TechnicalPapers_CorrelationAsTrendIndicator_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET { get; set; } = ETF.TLT;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 0;
        public override int CORR_PERIOD { get; set; } = 21;
        public override int CORR_ENTRY { get; set; } = 55;
        public override int CORR_EXIT { get; set; } = 55;
    }
    #endregion
    #region correlation as a cycle indicator
    /// <summary>
    /// Experimental algorithm using correlation as a cycle indicator.
    /// <see href="https://www.mesasoftware.com/papers/CORRELATION%20AS%20A%20CYCLE%20INDICATOR.pdf"/>
    /// <see href="https://www.mesasoftware.com/papers/RECURRING%20PHASE%20OF%20CYCLE%20ANALYSIS.pdf"/>
    /// </summary>
    public abstract class Ehlers_TechnicalPapers_CorrelationAsCycleIndicator_Core : Algorithm
    {
        public override string Name => "Ehlers' Correlation As A Cycle Indicator";

        #region inputs
        public virtual object ASSET { get; set; } = ETF.SPY;
        public virtual object SAFE { get; set; } = null;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 1;

        [OptimizerParam(21, 252, 21)]
        public virtual int PERIOD { get; set; } = 30;

        [OptimizerParam(21, 252, 21)]
        public virtual int MAX_CYCLE { get; set; } = 60;
        #endregion
        public override void Run()
        {
            //========== initialization ==========

#if true
            //StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            StartDate = StartDate ?? DateTime.Parse("1970-01-01T16:00-05:00");
            EndDate = EndDate ?? AlgorithmConstants.END_DATE - TimeSpan.FromDays(5);
#else
            // Ehlers's paper shows RTX between March 2021 and August 2022
            StartDate = StartDate ?? DateTime.Parse("2021-03-01T16:00-05:00");
            EndDate = EndDate ?? DateTime.Parse("2022-08-01T16:00-05:00");
#endif
            WarmupPeriod = TimeSpan.FromDays(90);
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            var asset = Asset(ASSET);
            var safe = SAFE != null ? Asset(SAFE) : null;

            var lbg = new LookbackGroup();
            var Signal = lbg.NewLookback(0);
            var Sx = lbg.NewLookback(0);
            var Sy = lbg.NewLookback(0);
            var Sxx = lbg.NewLookback(0);
            var Sxy = lbg.NewLookback(0);
            var Syy = lbg.NewLookback(0);
            var X = lbg.NewLookback(0);
            var Y = lbg.NewLookback(0);
            var Real = lbg.NewLookback(0);
            var Imag = lbg.NewLookback(0);
            var Angle = lbg.NewLookback(0);
            var DerivedPeriod = lbg.NewLookback(0);
            var DeltaAngle = lbg.NewLookback(0);
            var AvgPeriod = lbg.NewLookback(0);
            var State = lbg.NewLookback(0);

            double Cosine(double a) => Math.Cos(Math.PI / 180.0 * a);
            double Sine(double a) => Math.Sin(Math.PI / 180.0 * a);

            //========== simulation loop ==========

            SimLoop(() =>
            {
                // advance all series
                lbg.Advance();

                Signal.Value = asset.TypicalPrice()[0];

                // this code is taken from the paper
                // 'Recurring Phase of Cycle Analysis", which
                // seems to be newer than the paper
                // 'Correlation as a Cycle Indicator'

                // correlate with cosine wave having a fixed period
                Sx.Value = 0.0;
                Sy.Value = 0.0;
                Sxx.Value = 0.0;
                Sxy.Value = 0.0;
                Syy.Value = 0.0;
                for (var count = 1; count <= PERIOD; count++)
                {
                    X.Value = Signal[count - 1];
                    Y.Value = Cosine(360.0 * (count - 1) / PERIOD);
                    Sx.Value = Sx[0] + X[0];
                    Sy.Value = Sy[0] + Y[0];
                    Sxx.Value = Sxx[0] + X[0] * X[0];
                    Sxy.Value = Sxy[0] + X[0] * Y[0];
                    Syy.Value = Syy[0] + Y[0] * Y[0];
                }
                if (PERIOD * Sxx[0] - Sx[0] * Sx[0] > 0.0
                    && PERIOD * Syy[0] - Sy[0] * Sy[0] > 0.0)
                    Real.Value = (PERIOD * Sxy[0] - Sx[0] * Sy[0])
                    / Math.Sqrt((PERIOD * Sxx[0] - Sx[0] * Sx[0]) * (PERIOD * Syy[0] - Sy[0] * Sy[0]));

                // correlate with a negative sine wave having a fixed period
                Sx.Value = 0.0;
                Sy.Value = 0.0;
                Sxx.Value = 0.0;
                Sxy.Value = 0.0;
                Syy.Value = 0.0;
                for (var count = 1; count <= PERIOD; count++)
                {
                    X.Value = Signal[count - 1];
                    Y.Value = -Sine(360.0 * (count - 1) / PERIOD);
                    Sx.Value = Sx[0] + X[0];
                    Sy.Value = Sy[0] + Y[0];
                    Sxx.Value = Sxx[0] + X[0] * X[0];
                    Sxy.Value = Sxy[0] + X[0] * Y[0];
                    Syy.Value = Syy[0] + Y[0] * Y[0];
                }
                if (PERIOD * Sxx[0] - Sx[0] * Sx[0] > 0
                    && PERIOD * Syy[0] - Sy[0] * Sy[0] > 0)
                    Imag.Value = (PERIOD * Sxy[0] - Sx[0] * Sy[0])
                    / Math.Sqrt((PERIOD * Sxx[0] - Sx[0] * Sx[0]) * (PERIOD * Syy[0] - Sy[0] * Sy[0]));

                // compute the angle as an arctangent function and resolve ambiguity
                // If Real <> 0 Then Angle = 90 - Arctangent(Imag / Real);
                // If Real < 0 Then Angle = Angle - 180;
                Angle.Value = 90.0 - 180.0 / Math.PI * Math.Atan2(Imag, Real); // simplified by FUB

                // compensate for angle wraparound
                // If AbsValue(Angle[1]) - AbsValue(Angle - 360) < Angle - Angle[1] and Angle > 90 and 
                // Angle[1] < -90 Then Angle = Angle - 360;
                if (Angle > 180) Angle.Value = Angle - 360; // simplified by FUB

                // angle cannot go backwards
                // If Angle < Angle[1]
                //     and ((Angle > -135 and Angle[1] < 135)
                //         or (Angle < -90 and Angle[1] < -90))
                // Then Angle = Angle[1];
                if ((Angle < Angle[1] && Angle[1] - Angle < 180)
                    || (Angle > Angle[1] && Angle - Angle[1] > 180)
                )
                    Angle.Value = Angle[1]; // simplified by FUB

                // frequency derived from rate-change of phase
                DeltaAngle.Value = Angle - Angle[1];
                if (DeltaAngle <= 0.0) DeltaAngle.Value = DeltaAngle[1];
                if (DeltaAngle != 0.0) DerivedPeriod.Value = 360 / DeltaAngle;
                if (DerivedPeriod > MAX_CYCLE) DerivedPeriod.Value = MAX_CYCLE;

                // trend state variable
                State.Value = 0.0; // cycling
                if (Angle - Angle[1] < 360.0 / MAX_CYCLE)
                {
                    if (Angle >= 90.0 || Angle < -90.0) State.Value = 1.0; // trending up
                    if (Angle > -90.0 && Angle < 90.0) State.Value = -1.0; // trending down
                }

                //--- trade trend
                var targetAlloc = State != 0
                    ? (State > 0 ? (ALLOW_LONG != 0 ? 1.0 : 0.0) : (ALLOW_SHORT != 0 ? -1.0 : 0.0))
                    : 0.0;

                if (Math.Abs(asset.Position - targetAlloc) > 0.10)
                {
                    asset.Allocate(targetAlloc, OrderType.openNextBar);

                    if (safe != null)
                        safe.Allocate(Math.Max(0.0, 1.0 - targetAlloc), OrderType.openNextBar);
                }

                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("buy & hold", asset.Close[0]);

#if true
                    // optional charts
                    Plotter.SelectChart("Phasor Indicator", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("price", 100.0 * Math.Log(asset.Close[0] / asset.Close[(DateTime)StartDate]));
                    //Plotter.Plot("price", 10.0 * (asset.Close[0] - asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("phasor", Angle[0]);
                    Plotter.Plot("+90", 90.0);
                    Plotter.Plot("-90", -90.0);

                    Plotter.SelectChart("Cycle Period", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("price", 100.0 * Math.Log(asset.Close[0] / asset.Close[(DateTime)StartDate]));
                    //Plotter.Plot("price", 10.0 * (asset.Close[0] - asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("period", DerivedPeriod[0]);

                    Plotter.SelectChart("Cycle State", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("price", 100.0 * Math.Log(asset.Close[0] / asset.Close[(DateTime)StartDate]));
                    //Plotter.Plot("price", 10.0 * (asset.Close[0] - asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("state", 10.0 * State[0]);
#endif
                }
            });

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
    }

    public class Ehlers_TechnicalPapers_CorrelationAsCycleIndicator_SPY : Ehlers_TechnicalPapers_CorrelationAsCycleIndicator_Core
    {
        public override string Name => base.Name + " (SPY)";
        public override object ASSET => ETF.SPY;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 0;
        public override int PERIOD { get; set; } = 252;
        public override int MAX_CYCLE { get; set; } = 147;

    }
    public class Ehlers_TechnicalPapers_CorrelationAsCycleIndicator_TLT : Ehlers_TechnicalPapers_CorrelationAsCycleIndicator_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET => ETF.TLT;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 1;
        public override int PERIOD { get; set; } = 21;
        public override int MAX_CYCLE { get; set; } = 42;

    }
    #endregion

    #region trend-following w/ error-corrected ema
    /// <summary>
    /// Simple trend-following strategy using an Error-Corrected EMA.
    /// <see href="https://www.mesasoftware.com/papers/ZeroLag.pdf"/>
    /// </summary>
    public abstract class Ehlers_TechnicalPapers_TrendFollowingWithECEMA_Core : Algorithm
    {
        public override string Name => "Ehlers' Trend-Following w/ EC-EMA";

        #region inputs
        public virtual object ASSET { get; set; } = ETF.SPY;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 0;

        [OptimizerParam(21, 252, 21)]
        public virtual int LENGTH { get; set; } = 105;

        [OptimizerParam(10, 50, 5)]
        public virtual int GAIN { get; set; } = 45;

        [OptimizerParam(50, 250, 25)]
        public virtual int THRESHOLD { get; set; } = 175; // 100 = 1%
        #endregion
        public override void Run()
        {
            //========== initialization ==========

            //StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            StartDate = StartDate ?? DateTime.Parse("1970-01-01T16:00-05:00");
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(90);
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            //========== simulation loop ==========

            SimLoop(() =>
            {
                var asset = Asset(ASSET);
                var price = asset.TypicalPrice();
                var fast = price.ErrorCorrectedEMA(LENGTH, GAIN);
                var slow = price.EMA(LENGTH);

                // delta between error-corrected EMA and regular EMA
                var relError = fast.Sub(slow)
                    .AbsValue()
                    .Div(price);

                // target allocation. taking a position requires a minimum
                // relative error between the lines to avoid whipsaws
                var allocation = 100.0 * relError[0] > THRESHOLD / 100.0
                    ? (fast[0] > slow[0]
                        ? (ALLOW_LONG != 0 ? 1.0 : 0.0)
                        : (ALLOW_SHORT != 0 ? -1.0 : 0.0))
                    : 0.0;

                // on the short side, we might want to
                // regularly adjust the position size
                if (Math.Abs(asset.Position - allocation) > 0.10)
                    asset.Allocate(allocation, OrderType.openNextBar);

                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("buy & hold", asset.Close[0]);

#if true
                    // optional charts
                    Plotter.SelectChart("Moving Averages", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 100.0 * Math.Log(asset.Close[0] / asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("EC-EMA", 100.0 * Math.Log(fast[0] / asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("EMA", 100.0 * Math.Log(slow[0] / asset.Close[(DateTime)StartDate]));
                    Plotter.Plot("Error bps", 10000.0 * relError.EMA(63)[0]);
                    Plotter.Plot("Allocation", 10.0 * allocation);
#endif
                }
            });

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
    }
    public class Ehlers_TechnicalPapers_TrendFollowingWithECEMA_SPY : Ehlers_TechnicalPapers_TrendFollowingWithECEMA_Core
    {
        public override string Name => base.Name + " (SPY)";
        public override object ASSET { get; set; } = ETF.SPY;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 0;
        public override int LENGTH { get; set; } = 252;
        public override int GAIN { get; set; } = 35;
        public override int THRESHOLD { get; set; } = 125;
    }
    public class Ehlers_TechnicalPapers_TrendFollowingWithECEMA_TLT : Ehlers_TechnicalPapers_TrendFollowingWithECEMA_Core
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET { get; set; } = ETF.TLT;
        public override int ALLOW_LONG { get; set; } = 1;
        public override int ALLOW_SHORT { get; set; } = 0;
        public override int LENGTH { get; set; } = 21;
        public override int GAIN { get; set; } = 40;
        public override int THRESHOLD { get; set; } = 50;
    }
    #endregion
    #region trend-following w/ cyber cycle
    /// <summary>
    /// Simple trend-following strategy using Ehlers's CyberCycle indicator.
    /// <see href="https://www.mesasoftware.com/papers/TheInverseFisherTransform.pdf"/>
    /// </summary>
    public class Ehlers_TechnicalPapers_TrendFollowingWithCyberCycle : Algorithm
    {
        public override string Name => "Ehlers' Trend-Following w/ CyberCycle";

        #region inputs
        public virtual object ASSET { get; set; } = ETF.TLT;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 0;

        [OptimizerParam(100, 6000, 25)]
        public virtual int ALPHA { get; set; } = 700; // in bps, 700 = 0.07
        #endregion
        public override void Run()
        {
            //========== initialization ==========

            //StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            StartDate = StartDate ?? DateTime.Parse("1970-01-01T16:00-05:00");
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(30);
            ((Account_Default)Account).Friction = 0.0; // AlgorithmConstants.FRICTION;

            //========== simulation loop ==========

            SimLoop(() =>
            {
                var asset = Asset(ASSET);
                var price = asset.TypicalPrice();
                var cc0 = price.CyberCycle(ALPHA / 10000.0);
                var cc1 = cc0.Delay(1);

                // target allocation: change on crossover points
                var allocation = Lambda("alloc", (prev) =>
                {
                    if (prev <= 0.0 && cc0[0] > cc1[0] && cc0[1] <= cc1[1])
                        return 1.0;
                    if (prev >= 0.0 && cc0[0] < cc1[0] && cc0[1] >= cc1[0])
                        return -1.0;
                    return prev;
                }, 0.0)[0];

                // on the short side, we might want to
                // regularly adjust the position size
                if (Math.Abs(asset.Position - allocation) > 0.10)
                    asset.Allocate(allocation, OrderType.openNextBar);

                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("buy & hold", asset.Close[0]);

#if true
                    // optional charts
                    Plotter.SelectChart("CyberCycle", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", price[0]);
                    Plotter.Plot("CyberCycle", 50.0 * cc0[0]);
                    Plotter.Plot("CyberCycle Delayed", 50.0 * cc1[0]);
#endif
                }
            });

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource && false)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }
    }
    #endregion
}

//==============================================================================
// end of file
