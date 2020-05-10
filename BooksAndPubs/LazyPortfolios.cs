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
using System.Globalization;
using TuringTrader.Indicators;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region LazyPortfolio core
    public abstract class LazyPortfolio : SubclassableAlgorithm
    {
        #region internal data
        private Plotter _plotter = null;
        private AllocationTracker _alloc = new AllocationTracker();
        private List<double> _nav = new List<double>();
        private Dictionary<int, double> _minCagr = new Dictionary<int, double>();
        private Dictionary<int, double> _maxCagr = new Dictionary<int, double>();
        private List<int> _cagrPeriods = new List<int> { 1 * 252, 5 * 252, 10 * 252, 20 * 252 };
        #endregion
        #region inputs
        public abstract HashSet<Tuple<string, double>> ALLOCATION { get; }
        public virtual string BENCH => Assets.PORTF_60_40;
        public virtual DateTime START_TIME => Globals.START_TIME;
        public virtual DateTime END_TIME => Globals.END_TIME;
        public virtual bool REBAL_MONTHLY => true;
        #endregion

        #region ctor
        public LazyPortfolio()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            StartTime = SubclassedStartTime ?? START_TIME;
            EndTime = SubclassedEndTime ?? END_TIME;
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = 0.0; // lazy portfolios w/o commissions

            var universe = AddDataSources(ALLOCATION.Select(u => u.Item1));
            var bench = AddDataSource(BENCH);

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(universe) || !HasInstrument(bench))
                    continue;

                //if (SimTime[0].Date.DayOfWeek > NextSimTime.Date.DayOfWeek)
                if (!REBAL_MONTHLY || SimTime[0].Date.Month != NextSimTime.Date.Month)
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

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    _plotter.AddStrategyHoldings(this, universe.Select(ds => ds.Instrument));
                }

#if false
                if (_nav.Count() == 0)
                {
                    Output.WriteLine("First simulation timestamp {0:MM/dd/yyyy}", SimTime[0]);
                    foreach (var ds in universe)
                        Output.WriteLine("  {0}: start at {1:MM/dd/yyyy}", ds.Instrument.Name, ds.FirstTime);
                }

                _nav.Add(NetAssetValue[0]);

                foreach (var d in _cagrPeriods)
                {
                    if (_nav.Count > d)
                    {
                        var cagr = Math.Exp(252.0 / d * Math.Log(_nav.Last() / _nav[_nav.Count() - 1 - d])) - 1.0;
                        if (!_minCagr.ContainsKey(d))
                        {
                            _minCagr[d] = cagr;
                            _maxCagr[d] = cagr;
                        } 
                        else
                        {
                            _minCagr[d] = Math.Min(_minCagr[d], cagr);
                            _maxCagr[d] = Math.Max(_maxCagr[d], cagr);
                        }
                    }
                }
#endif
            }

            //========== post processing ==========
#if false
            foreach (var d in _minCagr.Keys)
            {
                Output.WriteLine("{0}-year return: min = {1:P2}, max = {2:P2}", d / 252, _minCagr[d], _maxCagr[d]);
            }
#endif

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                //_plotter.AddParameters(this);
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

    #region 60/40 benchmark
    public class Benchmark_60_40 : LazyPortfolio
    {
        public override string Name => "Vanilla 60/40";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            Tuple.Create(Assets.STOCKS_US_LG_CAP, 0.60),
            Tuple.Create(Assets.BONDS_US_TOTAL, 0.40),
        };
        public override string BENCH => Assets.STOCKS_US_LG_CAP;
    }
    #endregion
    #region Tony Robbins' All-Seasons Portfolio
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
        //public override DateTime START_TIME => DateTime.Parse("01/01/1900", CultureInfo.InvariantCulture);
    }
    #endregion
    #region Harry Browne's Permanent Portfolio
    public class Browne_PermanentPortfolio : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See Harry Browne, Fail Safe Investing
            Tuple.Create(Assets.STOCKS_US_LG_CAP,   0.25),  // 25% S&P 500
            Tuple.Create(Assets.BONDS_US_TREAS_30Y, 0.25),  // 25% 20-25yr Treasuries
            //Tuple.Create(Assets.BONDS_US_TREAS_3M,  0.25),  // 25% Treasury Bills
            Tuple.Create(Assets.BONDS_US_TREAS_3Y,  0.25),  // 25% Short-Term Treasuries
            Tuple.Create(Assets.GOLD,               0.25),  // 25% Gold
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
    #endregion
}

//==============================================================================
// end of file