//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Lazy Portfolios
// Description: Simple benchmarking portfolios.
// History:     2019xii04, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region LazyPortfolio core
    public abstract class LazyPortfolio : AlgorithmPlusGlue
    {
        #region inputs
        public abstract HashSet<Tuple<object, double>> ALLOCATION { get; }
        public virtual string BENCH => Indices.PORTF_60_40;
        public virtual DateTime START_TIME => Globals.START_TIME;
        public virtual DateTime END_TIME => Globals.END_TIME;
        public virtual double COMMISSION => 0.0; // lazy portfolios typically w/o commission
        public virtual double MGMT_FEE => 0.0; // no management fee
        public virtual bool IsTradingDay => SimTime[0].Month != NextSimTime.Month;
        #endregion

        #region IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

            StartTime = startTime ?? START_TIME;
            EndTime = endTime ?? END_TIME;
            WarmupStartTime = StartTime - TimeSpan.FromDays(365);

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = COMMISSION;

            var allocation = ALLOCATION
                .Select(a => Tuple.Create(AddDataSource(a.Item1), a.Item2))
                .ToList();
            var bench = AddDataSource(BENCH);

            //========== simulation loop ==========

            var accruedMgmtFee = 0.0;
            foreach (var s in SimTimes)
            {
                if (!HasInstruments(allocation.Select(a => a.Item1)))
                    continue;

                if (!IsDataSource && !HasInstrument(bench))
                    continue;

                if (IsTradingDay)
                {
                    foreach (var a in allocation)
                    {
                        var w = a.Item2 != 0.0 ? a.Item2 : 1.0 / ALLOCATION.Count;
                        var i = a.Item1.Instrument;
                        Alloc.Allocation[i] = w;

                        int targetShares = (int)Math.Floor(NetAssetValue[0] * w / i.Close[0]);
                        i.Trade(targetShares - i.Position);
                    }
                }

                // management fees: acrue daily, deduct monthly
                if (MGMT_FEE > 0.0)
                {
                    accruedMgmtFee += NetAssetValue[0] * MGMT_FEE / 252.0;
                    if (SimTime[0].Month != NextSimTime.Month)
                    {
                        Withdraw(accruedMgmtFee);
                        accruedMgmtFee = 0.0;
                    }
                }

                var p = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
                    p, p, p, p, 0);

                // plotter output
                if (!IsOptimizing && !IsDataSource && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, bench.Instrument);
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);
                    _plotter.AddStrategyHoldings(this, allocation.Select(a => a.Item1.Instrument));
                }
            }

            //========== post processing ==========

            if (!IsOptimizing && !IsDataSource)
            {
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                //_plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
    }
    #endregion

    #region all-cash/ zero-return portfolio
    public class Benchmark_Zero : LazyPortfolio
    {
        public override string Name => "All-Cash/ Zero-Return";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.SPY, 0.00),
        };
        public override string BENCH => Assets.SPY;
    }
    #endregion
    #region 60/40 benchmark
    public class Benchmark_60_40 : LazyPortfolio
    {
        public override string Name => "Vanilla 60/40";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(Assets.SPY, 0.60),
            new Tuple<object, double>(Assets.AGG, 0.40),
        };
        public override string BENCH => Indices.SPXTR;
    }
    #endregion
    #region Tony Robbins' All-Seasons Portfolio
    public class Robbins_AllSeasonsPortfolio : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            // See Tony Robbins "Money, Master the Game", Chapter 5
            new Tuple<object, double>(Assets.SPY,   0.30),  // 30% S&P 500
            new Tuple<object, double>(Assets.IEF, 0.15),  // 15% 7-10yr Treasuries
            new Tuple<object, double>(Assets.TLT, 0.40),  // 40% 20-25yr Treasuries
            new Tuple<object, double>(Assets.GLD,               0.075), // 7.5% Gold
            new Tuple<object, double>(Assets.DBC,        0.075), // 7.5% Commodities
        };
        public override string BENCH => Indices.PORTF_60_40;
        //public override DateTime START_TIME => DateTime.Parse("01/01/1900", CultureInfo.InvariantCulture);
    }
#if false
    public class Robbins_AllSeasonsPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio (2x Leverage)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // see https://www.optimizedportfolio.com/all-weather-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_2X,   0.30),  // 30% 2x S&P 500 (SSO)
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_2X, 0.40),  // 40% 2x 20-25yr Treasuries (UBT)
            Tuple.Create(Assets.BONDS_US_TREAS_10Y_2X, 0.15),  // 15% 2x 7-10yr Treasuries (UST)
            Tuple.Create(Assets.GOLD_2X,               0.075), // 7.5% 2x Gold (UGL)
            Tuple.Create("DIG",                        0.075), // 7.5% 2x Oil & Gas (DIG)
        };
        public override string BENCH => Assets.PORTF_60_40;
        //public override DateTime START_TIME => DateTime.Parse("01/01/1900", CultureInfo.InvariantCulture);
    }
#endif
#if false
    public class Robbins_AllSeasonsPortfolio_3x : LazyPortfolio
    {
        public override string Name => "Robbins' All-Seasons Portfolio (2.89x Leverage)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // replacing commodities w/ utilities
            // see https://www.optimizedportfolio.com/all-weather-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_3X,   0.289),  // 30% 3x S&P 500 (UPRO)
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_3X, 0.385),  // 40% 3x 20-25yr Treasuries (TMF)
            Tuple.Create(Assets.BONDS_US_TREAS_10Y_3X, 0.145),  // 15% 3x 7-10yr Treasuries (TYD)
            Tuple.Create(Assets.GOLD_2X,               0.073),  // 7.5% 2x Gold (UGL)
            Tuple.Create("UTSL",                       0.108),  // 7.5% 3x Utilities (UTSL)
        };
        public override string BENCH => Assets.PORTF_60_40;
        //public override DateTime START_TIME => DateTime.Parse("01/01/1900", CultureInfo.InvariantCulture);
    }
#endif
    #endregion
    #region Harry Browne's Permanent Portfolio
    public class Browne_PermanentPortfolio : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            // See Harry Browne, Fail Safe Investing
            new Tuple<object, double>(Assets.SPY,   0.25),  // 25% S&P 500
            new Tuple<object, double>(Assets.TLT, 0.25),  // 25% 20-25yr Treasuries
            //new Tuple<object, double>(Assets.BONDS_US_TREAS_3M,  0.25),  // 25% Treasury Bills
            new Tuple<object, double>(Assets.SHY,  0.25),  // 25% Short-Term Treasuries
            new Tuple<object, double>(Assets.GLD,               0.25),  // 25% Gold
        };
        public override string BENCH => Indices.PORTF_60_40;
    }
#if false
    // NOTE: 3x Gold not available after summer 2020
    public class Browne_PermanentPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio (2x leveraged)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See https://www.optimizedportfolio.com/permanent-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_3X,   0.167),  // 25% S&P 500
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_3X, 0.167),  // 25% 20-25yr Treasuries
            Tuple.Create(Assets.GOLD_3X,               0.166),  // 25% Gold
            Tuple.Create(Assets.BONDS_US_TREAS_3Y,     0.500),  // 25% Short-Term Treasuries
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
#if false
    public class Browne_PermanentPortfolio_2x : LazyPortfolio
    {
        public override string Name => "Browne's Permanent Portfolio (1.82x leveraged)";
        public override HashSet<Tuple<string, double>> ALLOCATION => new HashSet<Tuple<string, double>>
        {
            // See https://www.optimizedportfolio.com/permanent-portfolio/
            Tuple.Create(Assets.STOCKS_US_LG_CAP_3X,   0.15),  // 25% S&P 500 (UPRO)
            Tuple.Create(Assets.BONDS_US_TREAS_30Y_3X, 0.15),  // 25% 20-25yr Treasuries (TMF)
            Tuple.Create(Assets.GOLD_2X,               0.22),  // 25% Gold (UGL)
            Tuple.Create(Assets.BONDS_US_TREAS_3Y,     0.48),  // 25% Short-Term Treasuries (BIL)
        };
        public override string BENCH => Assets.PORTF_60_40;
    }
#endif
    #endregion
}

//==============================================================================
// end of file