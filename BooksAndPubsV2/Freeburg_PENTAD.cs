//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Freeburg_PENTAD_v2
// Description: PENTAD strategy, as published in Nelson Freeburg's
//              Formula Research newsletter, October 1995.
// History:     2023ix01, FUB, created
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

// Freeberg's trend-following mechanism uses hysteresis specified in percents
// of the value. However, for the Advance-Decline Line, this doesn't make sense,
// as the absolute value of the line depends on the series' start date.
// if FIX_ADL_HYSTERSIS is defined, we use an alternative hysteresis in absolute terms
#define FIX_ADL_HYSTERSIS

// if FULL_RANGE is defined, we start in 1990
//#define FULL_RANGE

#region libraries
using System;
using System.Collections.Generic;
using TuringTrader.GlueV2;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;
#endregion


namespace Algorithms.TTorg
{
    public class Freeburg_PENTAD : Algorithm
    {
        public override string Name => "Freeburg's PENTAD";

        #region configuration
        public enum MaType
        {
            WMA,
            SMA,
        };
        public enum SignalMode
        {
            CR,
#if FIX_ADL_HYSTERSIS
            CRabs,
#endif
            SL,
        };

#if FIX_ADL_HYSTERSIS
        [OptimizerParam(100, 1000, 25)]
        public int ADL_U { get; set; } = 125;
        [OptimizerParam(100, 1500, 25)]
        public int ADL_L { get; set; } = 1000;
#endif

        public HashSet<Tuple<object, MaType, int, SignalMode, double, double>> CONFIG =>
            new HashSet<Tuple<object, MaType, int, SignalMode, double, double>>
            {
                new Tuple<object, MaType, int, SignalMode, double, double>("$SPX",    MaType.WMA, 65 * 5, SignalMode.CR, 0.0, 3.0), // $SPX - S&P 500 Index
#if FIX_ADL_HYSTERSIS
                // Freeburg used the NYSE Advance-Decline Line. We are using
                // the Russell 1000 instead, so that we can scale the hysteresis
                // based on the total # of stocks observed.
                new Tuple<object, MaType, int, SignalMode, double, double>("#RUIAD",     MaType.WMA, 14 * 5, SignalMode.CRabs, 5 * 1000 * ADL_U / 100.0, 5 * 1000 * ADL_L / 100.0), // #RUIAD - Russell 1000 Cumulative Advance-Decline Line
                //new Tuple<object, MaType, int, SignalMode, double, double>("#SPXAD",     MaType.WMA, 14 * 5, SignalMode.CRabs, 5 * 500 * ADL_U / 100.0, 5 * 500 * ADL_L / 100.0), // #SPXAD - S&P 500 Cumulative Advance-Decline Line
                //new Tuple<object, MaType, int, SignalMode, double, double>("#RUAAD",     MaType.WMA, 14 * 5, SignalMode.CRabs, 5 * 500 * ADL_U / 100.0, 5 * 500 * ADL_L / 100.0), // #RUAAD - Russell 3000 Cumulative Advance-Decline Line
#else
                new Tuple<object, MaType, int, SignalMode, double, double>("#NYSEAD",    MaType.WMA, 14 * 5, SignalMode.CR, 0.5, 2.0), // #NYSEAD - NYSE Cumulative Advance-Decline Line
#endif
                new Tuple<object, MaType, int, SignalMode, double, double>("$DJT",       MaType.SMA, 25 * 5, SignalMode.SL, 0.5, 2.5), // $DJT - Dow Jones Transportation Average
                new Tuple<object, MaType, int, SignalMode, double, double>("$DJU",       MaType.SMA, 27 * 5, SignalMode.SL, 0.0, 3.0), // $DJU - Dow Jones Utility Average
                // Freeburg used the Dow Jones 20 Bond Index. This index is
                // no longer in existence. We replaced it with ETF.LQD, which
                // has a backfill to 1970.
                //new Tuple<object, MaType, int, SignalMode, double, double>("$DJCBP",     MaType.WMA, 38 * 5, SignalMode.CR, 1.0, 2.0), // $DJCBP - Dow Jones Equal Weight US Corporate Bond Index
                //new Tuple<object, MaType, int, SignalMode, double, double>("$SP5IGBIT",  MaType.WMA, 38 * 5, SignalMode.CR, 1.0, 2.0), // $SP5IGBIT - S&P 500 Investment Grade Corporate Bond Total Return Index
                new Tuple<object, MaType, int, SignalMode, double, double>(ETF.LQD,      MaType.WMA, 38 * 5, SignalMode.CR, 1.0, 2.0), // LQD - iShares iBoxx $ Investment Grade Corporate Bond ETF
            };

        public virtual string STOCKS { get; set; } = ETF.SPY;
        public virtual string SAFE { get; set; } = ETF.BIL;
        //public virtual string BENCH { get; set; } = MarketIndex.SPXTR;
        public virtual string BENCH { get; set; } = Benchmark.PORTFOLIO_60_40;
        #endregion
        #region strategy logic
        public override void Run()
        {
            //========== initialization ==========

#if FULL_RANGE
            StartDate = StartDate ?? DateTime.Parse("1990-01-01T16:00-05:00");
#else
            StartDate = StartDate ?? AlgorithmConstants.START_DATE;
#endif
            EndDate = EndDate ?? AlgorithmConstants.END_DATE;
            WarmupPeriod = TimeSpan.FromDays(365);

            // Freeburg does not mention transaction cost
            ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

            //========== simulation loop ==========

            var isBullish = false;
            var isFirstWeek = true;
            SimLoop(() =>
            {
                // Freeburg's strategy only trades once per week
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    //----- indicator evaluation

                    int numBullishIndicators = 0;
                    foreach (var indicator in CONFIG)
                    {
                        var quote = Asset(indicator.Item1).TypicalPrice();

                        var movingAverage = indicator.Item2 switch
                        {
                            MaType.WMA => quote.WMA(indicator.Item3),
                            MaType.SMA => quote.SMA(indicator.Item3),
                            _ => throw new Exception("unexpected ma type"),
                        };

                        var tempQuoteRef = 1e99;
                        var signal = indicator.Item4 switch
                        {
                            SignalMode.CR =>
                                Lambda(
                                    string.Format("{0}-cr", movingAverage.Name),
                                    prevRegime => (prevRegime < 0.5
                                            ? quote[0] > movingAverage[0] * (1.0 + indicator.Item5 / 100.0)
                                            : quote[0] > movingAverage[0] * (1.0 - indicator.Item6 / 100.0)) ? 1.0 : 0.0,
                                    0.0),

#if FIX_ADL_HYSTERSIS
                            SignalMode.CRabs =>
                                Lambda(
                                    string.Format("{0}-cr-abs", movingAverage.Name),
                                    prevRegime => (prevRegime < 0.5
                                            ? quote[0] > movingAverage[0] + indicator.Item5 / 100.0
                                            : quote[0] > movingAverage[0] - indicator.Item6 / 100.0) ? 1.0 : 0.0,
                                    0.0),
#endif

                            SignalMode.SL =>
                                Lambda(
                                    string.Format("{0}-sl", movingAverage.Name),
                                    prevRegime =>
                                    {
                                        var nextRegime = (prevRegime < 0.5
                                            ? movingAverage[0] > tempQuoteRef * (1.0 + indicator.Item5 / 100.0)
                                            : movingAverage[0] > tempQuoteRef * (1.0 - indicator.Item6 / 100.0)) ? 1.0 : 0.0;

                                        tempQuoteRef = prevRegime == nextRegime
                                            ? (prevRegime > 0.5 ? Math.Max(movingAverage[0], tempQuoteRef) : Math.Min(movingAverage[0], tempQuoteRef))
                                            : movingAverage[0];

                                        return nextRegime;
                                    },
                                    1.0),

                            _ => throw new Exception("unexpected signal type"),
                        };

                        numBullishIndicators += (int)signal[0];

                        //----- additional charts
                        if (Plotter.AllData.Count > 0)
                        {
                            Plotter.SelectChart(Asset(indicator.Item1).Description, "Date");
                            Plotter.SetX(SimDate);
                            Plotter.Plot("Quote", Asset(indicator.Item1).Close[0] / Math.Abs(Asset(indicator.Item1).Close[(DateTime)StartDate]));
                            Plotter.Plot("Moving Average", movingAverage[0] / Math.Abs(Asset(indicator.Item1).Close[(DateTime)StartDate]));
                            Plotter.Plot("Signal", signal[0]);
                        }
                    }

                    var nextIsBullish = isBullish
                         ? numBullishIndicators > 3   // sell on 3 or less bullish signals
                         : numBullishIndicators >= 5; // buy on 5 bullish signals

                    //----- additional charts
                    if (Plotter.AllData.Count > 0)
                    {
                        Plotter.SelectChart("Bullish Indicators", "Date");
                        Plotter.SetX(SimDate);
                        //Plotter.Plot(Name, NetAssetValue / 1000.0);
                        Plotter.Plot(Asset(STOCKS).Description, Asset(STOCKS).Close[0] / Asset(STOCKS).Close[(DateTime)StartDate]);
                        Plotter.Plot("# of Bullish Signals", numBullishIndicators);
                        Plotter.Plot("Trade Position", isBullish ? 1.0 : 0.0);
                    }

                    //----- order management

                    if (nextIsBullish != isBullish || isFirstWeek)
                    {
                        isFirstWeek = false;
                        isBullish = nextIsBullish;
                        Asset(STOCKS).Allocate(isBullish ? 1.0 : 0.0, OrderType.openNextBar);
                        Asset(SAFE).Allocate(isBullish ? 0.0 : 1.0, OrderType.openNextBar);
                    }
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
}

//==============================================================================
// end of file
