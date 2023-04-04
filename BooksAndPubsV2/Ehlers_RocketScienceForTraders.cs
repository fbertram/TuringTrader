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
#define USE_EHLERS_RANGE

#region libraries
using System;
using TuringTrader.GlueV2;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace TuringTrader.BooksAndPubsV2
{
    #region SineTrend core
    public abstract class Ehlers_RocketScienceForTraders_SineTrend : Algorithm
    {
        public override string Name => "Ehlers' SineTrend";

        #region inputs
        public abstract object ASSET { get; set; }
        /// <summary>
        /// InstantaneousTrendline: Ehler's CycPart parameter. default = 100 = 100%
        /// </summary>
        [OptimizerParam(65, 150, 5)]
        public virtual int CYCLE_ADJUST { get; set; } = 100;
        /// <summary>
        /// MarketMode: price deviation from trend line to break cycle. default = 150 = 1.5%
        /// </summary>
        [OptimizerParam(0, 500, 50)]
        public virtual int BREAK_CYCLE { get; set; } = 150;

        /// <summary>
        /// Enable trading in trend mode.
        /// </summary>
        [OptimizerParam(0, 1, 1)]
        public virtual int TRADE_TREND { get; set; } = 1;

        /// <summary>
        /// Enable trading in cycle mode.
        /// </summary>
        [OptimizerParam(0, 1, 1)]
        public virtual int TRADE_CYCLE { get; set; } = 1;

        /// <summary>
        /// Enable long trades.
        /// </summary>
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_LONG { get; set; } = 1;

        /// <summary>
        /// Enable short trades.
        /// </summary>
        [OptimizerParam(0, 1, 1)]
        public virtual int ALLOW_SHORT { get; set; } = 1;

        //[OptimizerParam(1, 5, 1)]
        public int NUM_FLT = -1;
        //[OptimizerParam(2, 10, 1)]
        public int FLT_PERIOD = -1;

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

            var asset = Asset(ASSET);

            //========== simulation loop ==========

            SimLoop(() =>
            {
                var Price = asset.High
                    .Add(asset.Low)
                    .Div(2.0);

                var SmoothPrice = Price.WMA(4);

                // all of Ehlers's complex calculations are provided
                // through a family of easy-to-use indicators
                var Trend = Price.MarketMode(BREAK_CYCLE / 10000.0);
                var Sine = Price.SinewaveIndicator().Sine;
                var LeadSine = Price.SinewaveIndicator().LeadSine;
                var Trendline = Price.InstantaneousTrendline(CYCLE_ADJUST / 100.0);

                //--- trade trend mode
                double targetAllocation;
                if (Trend[0] == 1.0 && TRADE_TREND != 0)
                    targetAllocation = SmoothPrice[0] > Trendline[0]
                        ? (ALLOW_LONG != 0 ? 1.0 : 0.0)
                        : (ALLOW_SHORT != 0 ? -1.0 : 0.0);

                //--- trade cycle mode
                else if (Trend[0] == 0.0 && TRADE_CYCLE != 0)
                    targetAllocation = LeadSine[0] > Sine[0]
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
                    Plotter.SelectChart("Sinewave Indicator", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Sine", 10.0 * Sine[0] + 20.0);
                    Plotter.Plot("Lead Sine", 10.0 * LeadSine[0] + 20.0);
                    Plotter.Plot("Trending", Trend[0] != 0.0 ? 10.0 : 0.0);

                    Plotter.SelectChart("Instantaneous Trendline", "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot("Price", 25.0 * Math.Log(Price[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Smoothed Price", 25.0 * Math.Log(SmoothPrice[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Trendline", 25.0 * Math.Log(Trendline[0] / Price[(DateTime)StartDate]));
                    Plotter.Plot("Trending", Trend[0] != 0.0 ? 10.0 : 0.0);
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
    #region SineTrend - TLT
    public class Ehlers_RocketScienceForTraders_SineTrend_TLT : Ehlers_RocketScienceForTraders_SineTrend
    {
        public override string Name => "Ehlers' SineTrend (TLT)";

        public override object ASSET { get; set; } = ETF.TLT;

        [OptimizerParam(65, 150, 5)]
        public override int CYCLE_ADJUST { get; set; } = 70;
        [OptimizerParam(0, 500, 50)]
        public override int BREAK_CYCLE { get; set; } = 50;
    }
    #endregion
}

//==============================================================================
// end of file
