//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Lazy Portfolios
// Description: Simple benchmarking portfolios.
// History:     2019xii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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
using System.Linq;
using TuringTrader.Simulator;
using TuringTrader.Algorithms.Glue;
using System.Collections.Generic;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region LazyPortfolio
    public abstract class LazyPortfolio : SubclassableAlgorithm
    {
        private Plotter _plotter = null;
        private AllocationTracker _alloc = new AllocationTracker();
        public abstract HashSet<Tuple<string, double>> ALLOCATION { get; }
        public virtual string BENCH => Assets.PORTF_60_40;
        public LazyPortfolio()
        {
            _plotter = new Plotter(this);
        }

        public override void Run()
        {
            StartTime = SubclassedStartTime ?? Globals.START_TIME;
            EndTime = SubclassedEndTime ?? Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = 0.0; // lazy portfolios w/o commissions

            var universe = AddDataSources(ALLOCATION.Select(u => u.Item1));
            var bench = AddDataSource(BENCH);

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(universe) || !HasInstrument(bench))
                    continue;

                if (SimTime[0].Date.DayOfWeek > NextSimTime.Date.DayOfWeek)
                {
                    _alloc.LastUpdate = SimTime[0];
                    foreach (var a in ALLOCATION)
                    {
                        var w = a.Item2 != 0.0 ? a.Item2 : 1.0 / ALLOCATION.Count;
                        var i = FindInstrument(a.Item1);
                        _alloc.Allocation[i] = w;

                        int targetShares = (int)Math.Floor(NetAssetValue[0] * w / i.Close[0]);
                        i.Trade(targetShares - i.Position);
                    }
                }
                AddSubclassedBar(10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL);
                _plotter.AddNavAndBenchmark(this, bench.Instrument);
            }

            _plotter.AddTargetAllocation(_alloc);
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
    }
    #endregion

    public class Benchmark_60_40 : LazyPortfolio
    {
        public override string Name => "60/40 Portfolio";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create(Assets.STOCKS_US_LG_CAP, 0.60),
            Tuple.Create(Assets.BONDS_US_TOTAL, 0.40),
        };
        public override string BENCH => Assets.STOCKS_US_LG_CAP;
    }

    public class Robbins_AllSeasonsPortfolio : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See Tony Robbins "Money, Master the Game", Chapter 5
            Tuple.Create(Assets.STOCKS_US_LG_CAP,   0.30),  // 30% S&P 500
            Tuple.Create(Assets.BONDS_US_TREAS_10Y, 0.15),  // 15% 7-10yr Treasuries
            Tuple.Create(Assets.BONDS_US_TREAS_30Y, 0.40),  // 40% 20-25yr Treasuries
            Tuple.Create(Assets.GOLD,               0.075), // 7.5% Gold
            Tuple.Create(Assets.COMMODITIES,        0.075), // 7.5% Commodities
        };
        public override string BENCH => Assets.PORTF_60_40;
    }

#if false
    public class TT_AllSeasonsPortfolio_Leveraged : LazyPortfolio
    {
        public override string Name => "TuringTrader's Leveraged All-Seasons Portfolio";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // leveraged up to 130% total:
            // s&p 500:     30.0% =>  39.0%
            // 10yr t-bond: 15.0% =>  19.5%
            // 30yr t-bond: 40.0% =>   7.0%
            //                    +   15.0% as 3x leveraged ETF
            //                            (= 52.0% total exposure)
            // Gold:         7.5% =>   9.75%
            // Commodities:  7.5% =>   9.75%
            //             100.0% => 100.0% (130.0% exposure)
            Tuple.Create(Assets.STOCKS_US_LG_CAP,   0.39),
            Tuple.Create(Assets.BONDS_US_TREAS_10Y, 0.195),
            Tuple.Create(Assets.BONDS_US_TREAS_30Y, 0.07),
            Tuple.Create("TMF",                     0.15),
            Tuple.Create(Assets.GOLD,               0.0975),
            Tuple.Create(Assets.COMMODITIES,        0.0975),
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
}

//==============================================================================
// end of file