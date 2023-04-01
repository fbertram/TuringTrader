//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Ehlers_RocketScienceForTraders_v2.cs
// Description: Strategy, as published in John F. Ehlers book
//              'Rocket Science for Traders'
// History:     2023iii29, FUB, created
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

// USE_EHLERS_RANGE
// if defined, set simulation range to match charts in Ehlers's book
//#define USE_EHLERS_RANGE

#region libraries
using System;
using System.Collections.Generic;
using TuringTrader.GlueV2;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Porting;
#endregion

namespace TuringTrader.SimulatorV2.Porting
{
    #region helpers to port from TradeStation EasyLanguage
    public class TradeStation
    {
        #region time series
        public class FloatSeriesManager
        {
            private List<FloatSeries> _instances = new List<FloatSeries>();
            public interface IFloatSeries
            {
                double this[int offset] { get; }
                double Value { get; set; }
            }
            private class FloatSeries : IFloatSeries
            {
                private List<double> values = new List<double>();

                public FloatSeries(double init)
                {
                    values.Add(init);
                }

                public double this[int offset]
                    => values[Math.Min(values.Count - 1, offset)];

                public double Value
                {
                    get => values[0];
                    set => values[0] = value;
                }

                public void Advance()
                {
                    values.Insert(0, values[0]);

                    if (values.Count > 256)
                        values.Remove(values.Count - 1);
                }

                public static implicit operator double(FloatSeries s) => s[0];
            }

            public IFloatSeries NewSeries(double init)
            {
                var i = new FloatSeries(init);
                _instances.Add(i);

                return i;
            }

            public void Advance()
            {
                foreach (var i in _instances)
                    i.Advance();
            }
        }
        #endregion
        #region trigonometry
        public static double Sine(double angle) => Math.Sin(Math.PI / 180.0 * angle);
        public static double Cosine(double angle) => Math.Cos(Math.PI / 180.0 * angle);
        public static double ArcTangent(double f)
            // Ehlers' code expects ArcTangent to return angles between
            // 0 and 180 degrees. In contrast, Math.Atan returns angles
            // between -Pi/2 and +Pi/2.
            => 180.0 / Math.PI * Math.Atan(f) + (f < 0.0 ? 180.0 : 0.0);
            //=> 180.0 / Math.PI * Math.Atan(f);
        #endregion
    }
    #endregion
}

namespace TuringTrader.BooksAndPubsV2
{
    #region SineTrend strategy
    public class Ehlers_RocketScienceForTraders_SineTrend : Algorithm
    {
        public override string Name => "Ehlers' SineTrend";

        #region inputs
        //public virtual object ASSET { get; set; } = ETF.SPY;
        public virtual object ASSET { get; set; } = ETF.TLT;
        //public virtual object ASSET { get; set; } = "EURUSD";
        //public virtual object ASSET { get; set; } = "&ES";
        //public virtual object ASSET { get; set; } = ETF.VXX;
        /// <summary>
        /// Ehler's CycPart parameter
        /// </summary>
        [OptimizerParam(65, 150, 5)]
        public virtual int CYC_PART { get; set; } = 90;
        /// <summary>
        /// Maximum cycle amplitude, before forcing trend mode.
        /// Measured in bps, default 150 = 1.5%
        /// </summary>
        [OptimizerParam(150, 1000, 50)]
        public virtual int MAX_CYC_AMP { get; set; } = 150;

        [OptimizerParam(0, 1, 1)]
        public virtual int TRADE_TREND { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int TRADE_CYCLE { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 1;
        #endregion
        #region strategy logic
        public override void Run()
        {
            //========== initialization ==========

#if USE_EHLERS_RANGE
            StartDate = StartDate ?? DateTime.Parse("1984-07-09T16:00-05:00");
            EndDate = EndDate ?? DateTime.Parse("2000-06-16T16:00-05:00");
#else
            //StartDate = StartDate ?? AlgorithmConstants.START_DATE;
            StartDate = StartDate ?? DateTime.Parse("1970-01-01T16:00-05:00");
            EndDate = EndDate ?? AlgorithmConstants.END_DATE - TimeSpan.FromDays(5);
#endif
            WarmupPeriod = TimeSpan.FromDays(365);
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

#if true
            var asset = Asset(ASSET);
#else
            // synthetic data for testing
            var asset = Asset("synth", () =>
            {
                var bars = new List<BarType<OHLCV>>();

                int barNumber = 0;
                double accumulatedPhase = 0.0;
                foreach (var t in TradingCalendar.TradingDays)
                {
                    var T = barNumber % 1000 < 500 ? 10.0 : 25.0;
                    var p = 100.0 + 1.5 * Math.Sin(accumulatedPhase);

                    bars.Add(new BarType<OHLCV>(
                        t,
                        new OHLCV(p, p, p, p, 0.0)));

                    barNumber += 1;
                    accumulatedPhase += 2.0 * Math.PI / T;
                }
                return bars;
            });
#endif
            var Price = asset.High
                .Add(asset.Low)
                .Div(2.0);

            var seriesManager = new TradeStation.FloatSeriesManager();

            // the code is taken from the book almost verbatim,
            // including the variable names.
            var Smooth = seriesManager.NewSeries(0);
            var Detrender = seriesManager.NewSeries(0);
            var I1 = seriesManager.NewSeries(0);
            var Q1 = seriesManager.NewSeries(0);
            var jI = seriesManager.NewSeries(0);
            var jQ = seriesManager.NewSeries(0);
            var I2 = seriesManager.NewSeries(0);
            var Q2 = seriesManager.NewSeries(0);
            var Re = seriesManager.NewSeries(0);
            var Im = seriesManager.NewSeries(0);
            var Period = seriesManager.NewSeries(0);
            var SmoothPeriod = seriesManager.NewSeries(0);
            var SmoothPrice = seriesManager.NewSeries(0);
            var DCPeriod = seriesManager.NewSeries(0);
            var IntPeriod = seriesManager.NewSeries(0);
            var RealPart = seriesManager.NewSeries(0);
            var ImagPart = seriesManager.NewSeries(0);
            var DCPhase = seriesManager.NewSeries(0);
            var DCSine = seriesManager.NewSeries(0);
            var LeadSine = seriesManager.NewSeries(0);
            var ITrend = seriesManager.NewSeries(0);
            var Trendline = seriesManager.NewSeries(0);
            var Trend = seriesManager.NewSeries(0);
            var DaysInTrend = seriesManager.NewSeries(0);

            //========== simulation loop ==========

            SimLoop(() =>
            {
                // advance all series
                seriesManager.Advance();

                // note the complicated feedback through the
                // period value, which makes porting to 
                // TuringTrader's indicators tricky

                Smooth.Value = (4.0 * Price[0] + 3.0 * Price[1]
                    + 2.0 * Price[2] + Price[3])
                    / 10.0;

                Detrender.Value = (0.0962 * Smooth[0] + 0.5769 * Smooth[2]
                    - 0.5769 * Smooth[4] - 0.0962 * Smooth[6])
                    * (0.075 * Period[1] + 0.54);

                //--- compute in-phase and quadrature components
                Q1.Value = (0.0962 * Detrender[0] + 0.5769 * Detrender[2]
                    - 0.5769 * Detrender[4] - 0.0962 * Detrender[6])
                    * (0.075 * Period[1] + 0.54);

                I1.Value = Detrender[3];

                //--- advance the phase of I1 and Q1 by 90 degrees
                jI.Value = (0.0962 * I1[0] + 0.5769 * I1[2]
                    - 0.5769 * I1[4] - 0.0962 * I1[6])
                    * (0.075 * Period[1] + 0.54);

                jQ.Value = (0.0962 * Q1[0] + 0.5769 * Q1[2]
                    - 0.5769 * Q1[4] - 0.0962 * Q1[6])
                    * (0.075 * Period[1] + 0.54);

                //--- phasor addition for 3-bar averaging
                I2.Value = I1[0] - jQ[0];
                Q2.Value = Q1[0] + jI[0];

                //--- smooth the i and q components before applying the discriminator
                I2.Value = 0.2 * I2[0] + 0.8 * I2[1];
                Q2.Value = 0.2 * Q2[0] + 0.8 * Q2[1];

                //--- homodyne discriminator
                Re.Value = I2[0] * I2[1] + Q2[0] * Q2[1];
                Im.Value = I2[0] * Q2[1] - Q2[0] * I2[1];

                Re.Value = 0.2 * Re[0] + 0.8 * Re[1];
                Im.Value = 0.2 * Im[0] + 0.8 * Im[1];

                if (Im[0] != 0.0 && Re[0] != 0.0) Period.Value = 360.0 / TradeStation.ArcTangent(Im[0] / Re[0]);
                if (Period[0] > 1.5 * Period[1]) Period.Value = 1.5 * Period[1];
                if (Period[0] < 0.67 * Period[1]) Period.Value = 0.67 * Period[1];
                if (Period[0] < 6.0) Period.Value = 6.0;
                if (Period[0] > 50.0) Period.Value = 50.0;

                Period.Value = 0.2 * Period[0] + 0.8 * Period[1];
                SmoothPeriod.Value = 0.33 * Period[0] + 0.67 * SmoothPeriod[1];

                //--- compute dominant cycle phase
                SmoothPrice.Value = (4.0 * Price[0] + 3.0 * Price[1]
                    + 2.0 * Price[2] + Price[3]) / 10.0;

                DCPeriod.Value = Math.Floor(SmoothPeriod[0] + 0.5);

                RealPart.Value = 0.0;
                ImagPart.Value = 0.0;
                for (var count = 0; count < DCPeriod[0]; count++)
                {
                    RealPart.Value = RealPart[0]
                        + TradeStation.Cosine(360.0 * count / DCPeriod[0]) * SmoothPrice[count];
                    ImagPart.Value = ImagPart[0]
                        + TradeStation.Sine(360.0 * count / DCPeriod[0]) * SmoothPrice[count];
                }

                if (Math.Abs(RealPart[0]) > 0.0) DCPhase.Value = TradeStation.ArcTangent(ImagPart[0] / RealPart[0]);
                if (Math.Abs(RealPart[0]) <= 0.001) DCPhase.Value = 90.0 * Math.Sign(ImagPart[0]);
                DCPhase.Value = DCPhase[0] + 90.0;
                var phase1 = DCPhase[0];

                //--- compensate for one bar lag of weighted moving average
                DCPhase.Value = DCPhase[0] + 360.0 / SmoothPeriod[0];
                var phase2 = DCPhase[0];

                if (ImagPart[0] < 0.0) DCPhase.Value = DCPhase[0] + 180.0;
                if (DCPhase[0] > 315.0) DCPhase.Value = DCPhase[0] - 360.0;
                var phase3 = DCPhase[0];

                //--- compute the Sine and LeadSine indicators
                DCSine.Value = TradeStation.Sine(DCPhase[0]);
                LeadSine.Value = TradeStation.Sine(DCPhase[0] + 45.0);

                //--- compute trendline as simple average over the measured dominant cycle period
                ITrend.Value = 0.0;
#if true
                // variant w/ configurable CycPart parameter (see fig 12.4.),
                // presented by Ehlers as an additional improvement
                IntPeriod.Value = Math.Floor(CYC_PART / 100.0 * SmoothPeriod[0] + 0.5);
                for (var count = 0; count < IntPeriod[0]; count++)
                    ITrend.Value = ITrend[0] + Price[count];
                if (DCPeriod[0] > 0.0) ITrend.Value = ITrend[0] / IntPeriod[0];
#else
                // orignal variant (see fig 12.1.), equivalent to CycPart = 1.0
                for (var count = 0; count < DCPeriod[0]; count++)
                    ITrend.Value = ITrend[0] + Price[count];
                if (DCPeriod[0] > 0.0) ITrend.Value = ITrend[0] / DCPeriod[0];
#endif

                Trendline.Value = (4.0 * ITrend[0] + 3.0 * ITrend[1]
                    + 2.0 * ITrend[2] + ITrend[3])
                    / 10.0;

                // if CurrentBar < 12 then Trendline = Price;

                //--- assume trend mode
                Trend.Value = 1.0;

                //--- measure days in trend from last crossing of the sinewave indicator lines
                if (DCSine[0] > LeadSine[0] && DCSine[1] <= LeadSine[1]
                    || DCSine[0] < LeadSine[0] && DCSine[1] >= LeadSine[1])
                {
                    DaysInTrend.Value = 0.0;
                    Trend.Value = 0.0;
                }

                DaysInTrend.Value = DaysInTrend[0] + 1.0;
                if (DaysInTrend[0] < 0.5 * SmoothPeriod[0]) Trend.Value = 0.0;

                //--- cycle mode if delta phase is +/-50% of dominant cycle change of phase
                if (SmoothPeriod[0] != 0.0
                    && DCPhase[0] - DCPhase[1] > 0.67 * 360.0 / SmoothPeriod[0]
                    && DCPhase[0] - DCPhase[1] < 1.5 * 360.0 / SmoothPeriod[0])
                    Trend.Value = 0.0;

                //--- declare a trend mode if the smoothprice is more than 1.5% from the trendline
                if (Math.Abs((SmoothPrice[0] - Trendline[0]) / Trendline[0]) >= 0.015)
                    Trend.Value = 1.0;

                //--- trade trend mode
                // if Trend = 1 then begin
                //     if Trend[1] = 0 then begin
                //         if MarketPosition = -1 and SmoothPrice >= Trendline then buy;
                //         if MarketPosition = 1 and SmoothPrice < Trendline then sell;
                //     end;
                //     if SmoothPrice crosses over Trendline then buy;
                //     if SmoothPrice crosses under Trendline then sell;
                // end;
                double targetAllocation;
                if (Trend[0] == 1.0 && TRADE_TREND != 0)
                    targetAllocation = SmoothPrice[0] > Trendline[0]
                        ? (ALLOW_LONG != 0 ? 1.0 : 0.0)
                        : (ALLOW_SHORT != 0 ? -1.0 : 0.0);

                //--- trade cycle mode
                // if Trend = 0 then begin
                //     if LeadSine crosses over  DCSine then buy;
                //     if LeadSine crosses under DCSinde then sell;
                // end;
                else if (Trend[0] == 0.0 && TRADE_CYCLE != 0)
                    targetAllocation = LeadSine[0] > DCSine[0]
                        ? (ALLOW_LONG != 0 ? 1.0 : 0.0)
                        : (ALLOW_SHORT != 0 ? -1.0 : 0.0);

                //--- fallback when long or short side are disabled
                else
                    targetAllocation = 0.0;

                if (Math.Abs(asset.Position - targetAllocation) > 0.10)
                    asset.Allocate(targetAllocation, OrderType.openNextBar);

                //--- main chart
                if (!IsOptimizing && !IsDataSource)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("buy & hold", asset.Close[0]);

#if true
                    // optional charts of internal signals
                    Plotter.SelectChart("Dominant Cycle", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("DC Period", DCPeriod[0]);

                    Plotter.SelectChart("Market Mode", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Trending", Trend[0] != 0.0 ? 25.0 : 0.0);
                    Plotter.Plot("Days in Trend", Math.Min(50.0, DaysInTrend[0]));

                    Plotter.SelectChart("Trend Trading", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Smoothed Price", 25.0 * Math.Log(SmoothPrice[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Trendline", 25.0 * Math.Log(Trendline[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Trending", Trend[0] != 0.0 ? 25.0 : 0.0);

                    Plotter.SelectChart("Cycle Trading", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("DC Sine", 10.0 * DCSine[0]);
                    Plotter.Plot("Lead Sine", 10.0 * LeadSine[0]);
                    Plotter.Plot("Trending", Trend[0] != 0.0 ? 25.0 : 0.0);
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
        #endregion
    }
    #endregion
}

//==============================================================================
// end of file
