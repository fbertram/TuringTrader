//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Soto_SectorRotation
// Description: Strategy, as published in Francois Soto's articles on
//              Factor-Based (SeekingAlpha):
//              https://factorbased.com/seekingalpha/
//              https://seekingalpha.com/article/4394646-this-sector-rotation-strategy-made-17-percent-year-since-1991
//              https://seekingalpha.com/article/4431639-this-sector-rotation-strategy-made-17-percent-each-year-since-1991-part-2
//              https://seekingalpha.com/article/4434713-sector-rotation-strategy-using-the-high-yield-spread
// History:     2021vii21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Solutions LLC
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
using System.Globalization;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class Soto_SectorRotation_HighYieldSpread : AlgorithmPlusGlue
    {
        public override string Name => "Soto's Sector Rotation (High Yield Spread)";

        #region inputs
        [OptimizerParam(5, 10, 1)]
        public int LONG_TERM_FLT_LEN = 10;

        [OptimizerParam(21, 252, 21)]
        public int TREND_FLT_LEN = 63;

        [OptimizerParam(5, 45, 5)]
        public int TREND_FLT_WIDTH = 15;

        public virtual Algorithm PF_DECLINE => new Soto_SectorRotation_Decline();
        public virtual Algorithm PF_RECOVERY => new Soto_SectorRotation_Recovery();
        public virtual Algorithm PF_EARLY => new Soto_SectorRotation_Early();
        public virtual Algorithm PF_LATE => new Soto_SectorRotation_Late();

        private readonly string BENCHMARK = Assets.STOCKS_US_LG_CAP;

        public virtual DateTime START_DATE { get; set; } = DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture);
        public virtual DateTime END_DATE { get; set; } = DateTime.Now.Date;
        #endregion
        #region separate portfolios for the four economic phases
        public class Soto_SectorRotation_Decline : LazyPortfolio
        {
            // Economic output and corporate earnings are negative;
            // Outperformers: Staples.
            public override string Name => "Soto's Sector Rotation (Decline)";
            public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.STOCKS_US_SECT_STAPLES, 0.0), // XLP
        };
            public override string BENCH => Assets.STOCKS_US_LG_CAP;
        }
        public class Soto_SectorRotation_Recovery : LazyPortfolio
        {
            // Economic output and corporate earnings are recovering;
            // Outperformers: Materials and discretionary.
            public override string Name => "Soto's Sector Rotation (Recovery)";
            public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.STOCKS_US_SECT_MATERIALS, 0.0), // XLB
            new Tuple<object, double>(Assets.STOCKS_US_SECT_DISCRETIONARY, 0.0), // XLY
        };
            public override string BENCH => Assets.STOCKS_US_LG_CAP;
        }
        public class Soto_SectorRotation_Early : LazyPortfolio
        {
            // Economic output and corporate earnings are growing;
            // Outperformers: Energy, financials and industrials
            public override string Name => "Soto's Sector Rotation (Early)";
            public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.STOCKS_US_SECT_ENERGY, 0.0), // XLE
            new Tuple<object, double>(Assets.STOCKS_US_SECT_FINANCIAL, 0.0), // XLF
            new Tuple<object, double>(Assets.STOCKS_US_SECT_INDUSTRIAL, 0.0), // XLI
        };
            public override string BENCH => Assets.STOCKS_US_LG_CAP;
        }
        public class Soto_SectorRotation_Late : LazyPortfolio
        {
            // Economic output and corporate earnings are slowing;
            // Outperformers: Info tech and health care.
            public override string Name => "Soto's Sector Rotation (Late)";
            public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.STOCKS_US_SECT_TECHNOLOGY, 0.0), // XLK
            new Tuple<object, double>(Assets.STOCKS_US_SECT_HEALTH_CARE, 0.0), // XLV
        };
            public override string BENCH => Assets.STOCKS_US_LG_CAP;
        }
        #endregion

        #region core logic
        public override void Run()
        {
            //========== initialization ==========

            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(1e6);

            // The high-yield spread is simply the difference between the
            // borrowing rate for below-investment-grade corporate bonds
            // and a treasury bond measure.
            // Soto uses the Barclays Corporate High Yield Index minus
            // the 10-Year Treasuries Yield. He also points at a similar 
            // spread on FRED: https://fred.stlouisfed.org/series/BAMLH0A0HYM2
            var treasuryYieldDs = AddDataSource("%10YTCM"); // US 10-Year Treasury Constant Maturity Yield
            var corporateYieldDs = AddDataSource("%COBAA"); // Moody's Seasoned Corporate Bonds BAA Yield

            var pfDecline = AddDataSource(PF_DECLINE);
            var pfRecovery = AddDataSource(PF_RECOVERY);
            var pfEarly = AddDataSource(PF_EARLY);
            var pfLate = AddDataSource(PF_LATE);
            var universe = new List<DataSource> { pfDecline, pfRecovery, pfEarly, pfLate };

            var bench = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            var yieldSpreadRising = true;

            foreach (var simTime in SimTimes)
            {
                if (!HasInstrument(treasuryYieldDs) || !HasInstrument(corporateYieldDs))
                    continue;

                var yieldSpread = corporateYieldDs.Instrument.Close
                    .Subtract(treasuryYieldDs.Instrument.Close)
                    .EMA(3).EMA(3).EMA(3);
                var yieldSpreadLongTermAvg = yieldSpread.EMA(LONG_TERM_FLT_LEN * 252);

                var yieldSignal = yieldSpread.Subtract(yieldSpreadLongTermAvg);
                var yieldSpreadHigh = yieldSignal[0] > 0.0;

                // NOTE: In his article, Soto does not disclose how he is determining
                // rising and falling trends. This our best guess, which is most likely
                // different from the Soto's method.
                var yieldSignalMax = yieldSignal.Highest(TREND_FLT_LEN);
                var yieldSignalMin = yieldSignal.Lowest(TREND_FLT_LEN);
                var yieldSignalR = (yieldSignal[0] - yieldSignalMin[0]) / Math.Max(1e-10, yieldSignalMax[0] - yieldSignalMin[0]);
                if (yieldSignalR > 1.0 - TREND_FLT_WIDTH / 100.0) yieldSpreadRising = true;
                else if (yieldSignalR < TREND_FLT_WIDTH / 100.0) yieldSpreadRising = false;

                if (!HasInstruments(universe))
                    continue;

                if (SimTime[0].Month != NextSimTime.Month)
                {
                    var weights = Instruments
                        .ToDictionary(i => i, i => 0.0);

                    if (yieldSpreadHigh)
                    {
                        if (yieldSpreadRising) weights[pfDecline.Instrument] = 1.0;
                        else weights[pfRecovery.Instrument] = 1.0;
                    }
                    else
                    {
                        if (yieldSpreadRising) weights[pfLate.Instrument] = 1.0;
                        else weights[pfEarly.Instrument] = 1.0;
                    }

                    foreach (var i in Instruments)
                    {
                        var shares = (int)Math.Round(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(shares - i.Position);

                        if (universe.Select(ds => ds.Instrument).Contains(i))
                            Alloc.Allocation[i] = weights[i];
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);

                    _plotter.SelectChart("Yield Spreads", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("High Yield Spread", yieldSpread[0]);
                    _plotter.Plot("Long-Term Average", yieldSpreadLongTermAvg[0]);

                    _plotter.SelectChart("Yield Signal", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Yield Signal", yieldSignal[0]);
                    _plotter.Plot("Trend+", yieldSignalMax[0]);
                    _plotter.Plot("Trend-", yieldSignalMin[0]);
                    _plotter.Plot("", 0.0);
                }

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
}

//==============================================================================
// end of file