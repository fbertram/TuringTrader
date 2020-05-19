//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_LAA
// Description: Lethargic Asset Allocation (FAA) strategy, as published in 
//              Wouter J. Keller's paper 
//              'Growth-Trend Timing and 60-40 Variations: Lethargic Asset Allocation (LAA)'
//              https://ssrn.com/abstract=3498092
// History:     2020iv28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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

// also see
// http://www.philosophicaleconomics.com/2016/01/gtt/
// http://www.philosophicaleconomics.com/2016/02/uetrend/

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region LAA core
    public abstract class Keller_LAA_Core : Algorithm
    {
        #region inputs
        public abstract HashSet<Tuple<string, double>> RISKY_PORTFOLIO { get; }
        public abstract HashSet<Tuple<string, double>> CASH_PORTFOLIO { get; }
        // other possible indicators include
        // https://fred.stlouisfed.org/series/RRSFS
        // https://fred.stlouisfed.org/series/INDPRO
        public static string ECONOMY = "fred:UNRATE"; // Unemployment Rate, montly, seasonally adjusted
        public static string MARKET = "$SPX";
        public static string BENCHMARK = Assets.PORTF_60_40;
        #endregion
        #region internal data
        private Plotter _plotter = null;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Keller_LAA_Core()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            var risky = RISKY_PORTFOLIO
                .Select(a => Tuple.Create(AddDataSource(a.Item1), a.Item2))
                .ToList();
            var cash = CASH_PORTFOLIO
                .Select(a => Tuple.Create(AddDataSource(a.Item1), a.Item2))
                .ToList();
            var universe = risky
                .Select(a => a.Item1)
                .Concat(cash
                    .Select(a => a.Item1))
                .Distinct()
                .ToList();

            var economy = AddDataSource(ECONOMY);
            var market = AddDataSource(MARKET);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                // skip if there are any instruments missing from our universe
                if (!HasInstruments(universe) || !HasInstrument(benchmark) || !HasInstrument(economy))
                    continue;

                // calculate indicators
                var economyLagged = economy.Instrument.Close.Delay(25); // 1 month publication lag: March observation published April 03
                var economySMA = economyLagged.SMA(252);
                var economyGrowing = economyLagged[0] < economySMA[0];
                var marketSMA = market.Instrument.Close.SMA(200); // 10-months moving average
                var marketRising = market.Instrument.Close[0] > marketSMA[0];

                // trigger monthly rebalancing
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // determine target allocation: cash, if economy shrinking _and_ markets declining
                    var allocation = economyGrowing || marketRising
                        ? risky
                        : cash;

                    // determine weights
                    var weights = universe
                        .Select(ds => ds.Instrument)
                        .ToDictionary(
                            i => i,
                            i => allocation
                                .Where(a => a.Item1 == i.DataSource)
                                .Sum(a => a.Item2));

                    // submit orders
                    _alloc.LastUpdate = SimTime[0];
                    foreach (var i in weights.Keys)
                    {
                        _alloc.Allocation[i] = weights[i];
                        var shares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(shares - i.Position);
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));

#if true
                    // additional plotter output
                    _plotter.SelectChart("Unemployment Trend", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(economy.Instrument.Name, economyLagged[0]);
                    _plotter.Plot(economy.Instrument.Name + "-SMA", economySMA[0]);

                    _plotter.SelectChart("Market Trend", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(market.Instrument.Name, market.Instrument.Close[0]);
                    _plotter.Plot(market.Instrument.Name + "-SMA", marketSMA[0]);
#endif
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
    #endregion

#if false
    // collection of supplemental simulations from the paper
    #region Fig. 2: The static 60-40 (SPY-IEF) benchmark
    public class Keller_LAA_Fig2_60_40_benchmark : LazyPortfolio
    {
        public override string Name => "Keller's LAA: The static 60-40 (SPY-IEF) benchmark (see Fig. 2)";

        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 0.60),
            Tuple.Create("IEF", 0.40),
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion
    #region Fig. 3: SPY (100%) switched to IEF (100%) using GT timing
    public class Keller_LAA_Fig3_SPY_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: SPY (100%) switched to IEF (100%) using GT Timing (Fig. 3)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 1.00),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 4: SPY+IEF (50-50%) switched to IEF (100%) using GT Timing
    public class Keller_LAA_Fig4_SPY_IEF_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: SPY+IEF (50-50%) switched to IEF (100%) using GT Timing (Fig. 4)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 0.50),
            Tuple.Create("IEF", 0.50),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 5: IWD+IEF (50-50%) switched to IEF (100%) using GT Timing
    public class Keller_LAA_Fig5_IWD_IEF_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: IWD+IEF (50-50%) switched to IEF (100%) using GT Timing (Fig. 5)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IWD", 0.50),
            Tuple.Create("IEF", 0.50),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 6: IWD+GLD+IEF (33.5% each) switched to IEF (100%) using GT Timing
    public class Keller_LAA_Fig6_IWD_GLD_IEF_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: IWD+GLD+IEF (33.5% each) switched to IEF (100%) using GT Timing (Fig. 6)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IWD", 0.333),
            Tuple.Create("GLD", 0.333),
            Tuple.Create("IEF", 0.333),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 7: QQQ+IWD+GLD+IEF (25% each) switched to IEF (100%) using GT Timing
    public class Keller_LAA_Fig7_QQQ_IWD_GLD_IEF_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: QQQ+IWD+GLD+IEF (25% each) switched to IEF (100%) using GT Timing (Fig. 7)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("QQQ", 0.25),
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 8: QQQ+IWD+GLD+IEF (25% each) static
    public class Keller_LAA_Fig8_QQQ_IWD_GLD_IEF_static : LazyPortfolio
    {
        public override string Name => "Keller's LAA: QQQ+IWD+GLD+IEF (25% each) static (see Fig. 8)";

        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("QQQ", 0.25),
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion
    #region Fig. 9: Permanent portfolio (SPY+GLD+BIL+TLT, 25% each), switched to IEF(100%) using GT timing
    public class Keller_LAA_Fig9_SPY_GLD_BIL_TLT_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: Permanent portfolio switched to IEF (100%) using GT Timing (Fig. 9)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("BIL", 0.25),
            Tuple.Create("TLT", 0.25),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 10: Golden Butterfly portfolio (SPY+IWN+GLD+SHY+TLT, 20% each), switched to IEF(100%) using GT timing
    public class Keller_LAA_Fig10_SPY_IWN_GLD_SHY_TLT_to_IEF : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: Golden Butterfly switched to IEF (100%) using GT Timing (Fig. 10)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 0.20),
            Tuple.Create("IWN", 0.20),
            Tuple.Create("GLD", 0.20),
            Tuple.Create("SHY", 0.20),
            Tuple.Create("TLT", 0.20),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IEF", 1.00),
        };
    }
    #endregion
    #region Fig. 11: QQQ+IWD+GLD+IEF (25% each) switched to BIL (100%) using GT Timing
    public class Keller_LAA_Fig11_QQQ_IWD_GLD_IEF_to_BIL : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA: QQQ+IWD+GLD+IEF (25% each) switched to BIL (100%) using GT Timing (Fig. 11)";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("QQQ", 0.25),
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("BIL", 1.00),
        };
    }
    #endregion
#endif

    #region LAA, see Fig. 12: QQQ+IWD+GLD+IEF (25% each), switched to SHY+IWD+GLD+IEF(25% each) using GT timing
    public class Keller_LAA : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
            Tuple.Create("QQQ", 0.25),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
            Tuple.Create("SHY", 0.25),
        };
    }
    #endregion

    #region constructed World ETF (named WRLD)
    public class Keller_LAA_WRLD : LazyPortfolio
    {
        public override string Name => "Keller's LAA: constructed World ETF (named WRLD)";

        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("SPY", 3.0 / 6.0),
            Tuple.Create("VEA", 2.0 / 6.0),
            Tuple.Create("VWO", 1.0 / 6.0),
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion
    #region LAA-G4, see Fig. 16: WRLD+IWD+GLD+IEF (25% each), switched to IWD+GLD+SHY+IEF(25% each) using GT timing
    public class Keller_LAA_G4 : Keller_LAA_Core
    {
        public override string Name => "Keller's LAA-G4";
        public override HashSet<Tuple<string, double>> RISKY_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("algo:Keller_LAA_WRLD", 0.25),
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("IEF", 0.25),
        };
        public override HashSet<Tuple<string, double>> CASH_PORTFOLIO => new HashSet<Tuple<string, double>>
        {
            Tuple.Create("IWD", 0.25),
            Tuple.Create("GLD", 0.25),
            Tuple.Create("SHY", 0.25),
            Tuple.Create("IEF", 0.25),
        };
    }
    #endregion
}

//==============================================================================
// end of file
