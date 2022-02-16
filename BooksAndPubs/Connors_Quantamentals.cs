//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        TradingMarkets Quantamentals
// Description: Strategies as presented in TradingMarkets.com
//              Quantamentals Seminar in early 2020.
// History:     2022ii15, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region All-Market Fixed-Income
    public class TradingMarkets_Quantamentals_AllMarketFixedIncome : LazyPortfolio
    {
        public override string Name => "TradingMarkets: Quantamentals All-Market Fixed Income";

        #region child strategies
        public abstract class TradePair : AlgorithmPlusGlue
        {
            public override string Name => "TradingMarkets: Trade Pair";

            #region inputs
            public virtual string ASSET_A { get; set; } = null;
            public virtual string ASSET_B { get; set; } = null;
            public virtual string BENCH { get; set; } = null;

            public virtual List<int> PERIODS { get; set; } = new List<int>
            {
                21,
                63,
                126,
            };

            public virtual bool IsTradingDay() => SimTime[0].Month != NextSimTime.Month;
            public virtual double Momentum(Instrument i, int n) => i.Close[0] / i.Close[n] - 1.0;
            #endregion
            #region strategy logic
            public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
            {
                StartTime = startTime ?? DateTime.Parse("01/01/1965");
                EndTime = endTime ?? DateTime.Now.Date - TimeSpan.FromDays(5);

                Deposit(Globals.INITIAL_CAPITAL);
                CommissionPerShare = 0.015;

                var assetA = AddDataSource(ASSET_A);
                var assetB = AddDataSource(ASSET_B);
                var assets = new List<DataSource>
                {
                    assetA,
                    assetB,
                };
                var bench = AddDataSource(BENCH);

                foreach (var simTime in SimTimes)
                {
                    if (!HasInstruments(assets) || !HasInstrument(bench))
                        continue;

                    var roc = assets
                        .ToDictionary(
                            ds => ds,
                            ds => PERIODS
                                .ToDictionary(
                                    p => p,
                                    p => Momentum(ds.Instrument, p)));

                    var score = assets
                        .ToDictionary(
                            ds => ds,
                            ds =>
                            {
                                var ds2 = assets
                                    .Where(ds2 => ds2 != ds)
                                    .First();
                                var score = PERIODS
                                    .Select(p => roc[ds][p] > roc[ds2][p] ? 1 : 0)
                                    .Sum();
                                return score;
                            });

                    var ranked = assets
                        .OrderByDescending(ds => score[ds])
                        .ToList();

                    var weights = assets
                        .ToDictionary(
                            ds => ds,
                            ds => ds == ranked[0] ? 1.0 : 0.0);

                    if (IsTradingDay())
                    {
                        foreach (var ds in assets)
                        {
                            var shares = (int)Math.Floor(weights[ds] * NetAssetValue[0] / ds.Instrument.Close[0]);
                            ds.Instrument.Trade(shares - ds.Instrument.Position);
                        }
                    }

                    yield return Bar.NewValue(Name, SimTime[0], NetAssetValue[0] / Globals.INITIAL_CAPITAL);

                    if (TradingDays > 0 && !IsOptimizing)
                    {
                        _plotter.AddNavAndBenchmark(this, bench.Instrument);

                        _plotter.SelectChart("Momentum Comparison", "Date");
                        _plotter.SetX(SimTime[0]);
                        _plotter.Plot("Asset A Selected", ranked[0] == assetA ? 0.125 : 0.0);
                        for (int i = 0; i < PERIODS.Count(); i++)
                        {
                            //_plotter.Plot(string.Format("Asset A: {0}d", PERIODS[i]), roc[assetA][PERIODS[i]] + 0.25 * i + 0.25);
                            //_plotter.Plot(string.Format("Asset B: {0}d", PERIODS[i]), roc[assetB][PERIODS[i]] + 0.25 * i + 0.25);
                            _plotter.Plot(string.Format("Asset A - Asset B: {0}d", PERIODS[i]), (roc[assetA][PERIODS[i]] - roc[assetB][PERIODS[i]]) + 0.25 * i + 0.25);
                            _plotter.Plot(string.Format("Trigger Line: {0}d", PERIODS[i]), 0.25 * i + 0.25);
                        }

                        _plotter.SelectChart("Internal Scores", "Date");
                        _plotter.SetX(SimTime[0]);
                        _plotter.Plot("Asset A: " + assetA.Instrument.Name, 0.1 * assetA.Instrument.Close[0]);
                        _plotter.Plot("Asset B: " + assetB.Instrument.Name, 0.1 * assetB.Instrument.Close[0]);
                        _plotter.Plot("Asset A Score", score[assetA]);
                        _plotter.Plot("Asset B Score", PERIODS.Count() - score[assetA]);
                    }
                }

                FitnessValue = this.CalcFitness();
                yield break;
            }
            #endregion
        }

        public class TradePair_SHY_BIL : TradePair
        {
            public override string Name => base.Name + " (SHY/BIL)";
            public override string ASSET_A { get; set; } = Assets.SHY;
            public override string ASSET_B { get; set; } = Assets.BIL;
            public override string BENCH { get; set; } = Assets.SHY;
        }
        public class TradePair_IEF_TIP : TradePair
        {
            public override string Name => base.Name + " (IEF/TIP)";
            public override string ASSET_A { get; set; } = Assets.IEF;
            public override string ASSET_B { get; set; } = Assets.TIP;
            public override string BENCH { get; set; } = Assets.IEF;
        }
        public class TradePair_HYG_TLT : TradePair
        {
            public override string Name => base.Name + " (HYG/TLT)";
            public override string ASSET_A { get; set; } = Assets.HYG;
            public override string ASSET_B { get; set; } = Assets.TLT;
            public override string BENCH { get; set; } = Assets.TLT;
        }
        #endregion

        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(new TradePair_SHY_BIL(),    0.3333),
            new Tuple<object, double>(new TradePair_IEF_TIP(),    0.3333),
            new Tuple<object, double>(new TradePair_HYG_TLT(),    0.3333),
        };

        public override string BENCH => Assets.AGG;
        public override DateTime START_TIME => DateTime.Parse("01/01/1965");
    }

#if true
    public class TradingMarkets_Quantamentals_AllMarketFixedIncome_SHY_BIL : TradingMarkets_Quantamentals_AllMarketFixedIncome.TradePair_SHY_BIL { }
    public class TradingMarkets_Quantamentals_AllMarketFixedIncome_HYG_TLT : TradingMarkets_Quantamentals_AllMarketFixedIncome.TradePair_HYG_TLT { }
    public class TradingMarkets_Quantamentals_AllMarketFixedIncome_IEF_TIP : TradingMarkets_Quantamentals_AllMarketFixedIncome.TradePair_IEF_TIP { }
#endif
    #endregion
}

//==============================================================================
// end of file