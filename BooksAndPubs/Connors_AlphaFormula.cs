//==============================================================================
// Project:     TuringTrader.com
// Name:        Connors_AlphaFormula
// Description: Strategies as published in Chris Cain and Larry Connors book
//              'The Alpha Formula'.
// History:     2019xii12, FUB, created
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

// USE_BOOK_RANGE: if defined, restrict backtest to range from book
//#define USE_BOOK_RANGE

// USE_FULL_RANGE: if defined, expand backtest to longest possible range
//#define USE_FULL_RANGE

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
    #region public class Connors_AlphaFormula_RisingAssets
    public class Connors_AlphaFormula_RisingAssets : Algorithm
    {
        public override string Name => "Connors' Rising Assets";

        #region internal data
        private Plotter _plotter;
        #endregion
        #region inputs
        public virtual HashSet<string> UNIVERSE => new HashSet<string>
        {
            //--- Risk Assets
            "SPY", // S&P 500
            "IWM", // US Small Cap
            "QQQ", // NASDAQ
            "EFA", // Developed Ex US
            "EEM", // Emerging Markets
            "VNQ", // US Real Estate
            "LQD", // US Inv. Grade Corps
            //--- Risk Off Assets
            "GLD", // Gold
            "SHY", // 1-3yr Treasuries
            "IEF", // 7-10yr Treasuries
            "TLT", // 20+yr Treasuries
            "AGG", // US Aggregate Bonds
        };

        public virtual string BENCHMARK => Indices.PORTF_60_40;

        public virtual double MOMENTUM(Instrument i) =>
            ((i.Close[0] - i.Close[21]) / i.Close[21]
            + (i.Close[0] - i.Close[63]) / i.Close[63]
            + (i.Close[0] - i.Close[126]) / i.Close[126]
            + (i.Close[0] - i.Close[252]) / i.Close[252]) / 4.0;
        #endregion
        #region ctor
        public Connors_AlphaFormula_RisingAssets()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if USE_BOOK_RANGE
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/01/2005", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#else
            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
#endif
            WarmupStartTime = StartTime - TimeSpan.FromDays(252);

            Deposit(Globals.INITIAL_CAPITAL);
            //CommissionPerShare = Globals.COMMISSION; // Connors does not consider commissions

            var universe = AddDataSources(UNIVERSE);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(universe) || !HasInstrument(benchmark))
                    continue;

                // calculate momentum and volatility
                var indicators = universe
                    .ToDictionary(
                        d => d.Instrument,
                        d => new
                        {
                            momentum = MOMENTUM(d.Instrument),
                            volatility = d.Instrument.Close.Volatility(63)[0],
                        });

                // take the top 5 assets with the highest momentum
                var top5 = indicators.Keys
                    .OrderByDescending(i => indicators[i].momentum)
                    .Take(5)
                    .ToList();

                // place orders on last business day of the month
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // determine sum of the inverse volatilities
                    var sumInverseVol = top5
                        .Sum(i => 1.0 / Math.Max(1e-10, indicators[i].volatility));

                    foreach (var d in universe)
                    {
                        double weight = top5.Contains(d.Instrument)
                            ? 1.0 / indicators[d.Instrument].volatility / sumInverseVol
                            : 0.0;

                        int targetShares = (int)Math.Floor(weight * NetAssetValue[0] / d.Instrument.Close[0]);
                        d.Instrument.Trade(targetShares - d.Instrument.Position);
                    }
                }

                if (TradingDays > 0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    _plotter.AddStrategyHoldings(this, universe.Select(u => u.Instrument));
                }

                if (IsDataSource)
                {
                    var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                    yield return Bar.NewOHLC(
                        this.GetType().Name, SimTime[0],
                        v, v, v, v, 0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                //_plotter.AddTargetAllocation(_alloc);
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
    #region public class Connors_AlphaFormula_WeeklyMeanReversion
    public class Connors_AlphaFormula_WeeklyMeanReversion : Algorithm
    {
        public override string Name => "Connors' Weekly Mean Reversion";

        #region internal data
        private Plotter _plotter;
        #endregion
        #region inputs
        private Universe _universe = Universes.STOCKS_US_LG_CAP;
        public virtual Universe UNIVERSE => _universe;
        public virtual string IDLE_CASH => "splice:SHY,VFIRX";
        public virtual string BENCHMARK => "SPY";
        public virtual string SPX => "$SPX";
        #endregion
        #region ctor
        public Connors_AlphaFormula_WeeklyMeanReversion()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if USE_BOOK_RANGE
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/01/2003", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#else
            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
#endif
            WarmupStartTime = StartTime - TimeSpan.FromDays(100);

            Deposit(Globals.INITIAL_CAPITAL);
            //CommissionPerShare = Globals.COMMISSION; // Connors does not consider commissions

            var universe = AddDataSources(UNIVERSE.Constituents);
            var idle = AddDataSource(IDLE_CASH);
            var spx = AddDataSource(SPX);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            var weeklyBars = new Dictionary<Instrument, TimeSeries<double>>();
            var entryPrices = new Dictionary<Instrument, double>();

            foreach (var s in SimTimes)
            {
                if (!HasInstrument(benchmark) || !HasInstrument(idle))
                    continue;

                // determine universe constituents
                var constituents = Instruments
                    .Where(i => UNIVERSE.IsConstituent(i.Nickname, SimTime[0]))
                    .ToList();

                // calculate daily indicators
                var dailyIndicators = Instruments
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            volatility = i.Close.Volatility(100)[0],
                        });

                // start with weights representing current positions
                const int NUM_POS = 10;
                var weights = Instruments
                    .ToDictionary(
                        i => i,
                        i => constituents.Contains(i) && i.Position > 0
                            ? 1.0 / NUM_POS
                            : 0.0);

                // sell, if stock closes 10% below entry price
                foreach (var i in Instruments)
                    if (i != idle.Instrument && i.Position > 0.0 && i.Close[0] < 0.90 * entryPrices[i])
                        weights[i] = 0.0;

                // weekly logic on last business day of the week
                if (SimTime[0].DayOfWeek > NextSimTime.DayOfWeek)
                {
                    foreach (var i in Instruments)
                        if (!weeklyBars.ContainsKey(i))
                            weeklyBars[i] = new TimeSeries<double>();

                    // advance weekly bars
                    foreach (var i in Instruments)
                        weeklyBars[i].Value = i.Close[0];

                    // calculate weekly indicators
                    var weeklyIndicators = Instruments
                        .ToDictionary(
                            i => i,
                            i => new
                            {
                                rsi = weeklyBars[i].RSI(2)[0],
                            });

                    // sell, if RSI is above 80
                    foreach (var i in Instruments)
                        if (weeklyIndicators[i].rsi > 80)
                            weights[i] = 0.0;

                    // determine number of new entries
                    var numCurrentEntries = weights
                        .Where(w => w.Value > 0.0)
                        .Count();
                    var numNewEntries = NUM_POS - numCurrentEntries;

                    // buy, if 
                    // - we don't have a position
                    // - trend filter is positive
                    // - RSI is below 20
                    var newEntries = constituents
                        .Where(i => i.Position == 0)
                        .Where(i => spx.Instrument.Close[0] > spx.Instrument.Close[126])
                        .Where(i => weeklyIndicators[i].rsi < 20)
                        .OrderBy(i => dailyIndicators[i].volatility)
                        .Take(numNewEntries)
                        .ToList();

                    foreach (var i in newEntries)
                    {
                        weights[i] = 1.0 / NUM_POS;
                        entryPrices[i] = i.Close[0];
                    }

                    // invest in the idle instrument, if we are not planning to
                    // fill all slots for mean-reversion instruments
                    double idleWeight = (numNewEntries - newEntries.Count()) / NUM_POS;
                    weights[idle.Instrument] = idleWeight;
                }

                foreach (var i in Instruments)
                {
                    int targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                    // don't rebalance our mean-reversion instruments
                    if (i != idle.Instrument && i.Position > 0 && targetShares > 0)
                        targetShares = i.Position;
                    i.Trade(targetShares - i.Position, OrderType.closeThisBar);
                }

                if (TradingDays > 0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    //_plotter.AddStrategyHoldings(this, universe.Select(u => u.Instrument));
                }

                if (IsDataSource)
                {
                    var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                    yield return Bar.NewOHLC(
                        this.GetType().Name, SimTime[0],
                        v, v, v, v, 0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                //_plotter.AddTargetAllocation(_alloc);
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
    #region public class Connors_AlphaFormula_DynanmicTreasuries
    public class Connors_AlphaFormula_DynamicTreasuries : AlgorithmPlusGlue
    {
        public override string Name => "Connors' Dynamic Treasuries";

        #region inputs
        public virtual HashSet<string> UNIVERSE => new HashSet<string>
        {
            Assets.IEF, // 7-10yr Treasuries (duration = 7.5 years)
            Assets.TLH, // 10-20yr Treasuries (duration = 11.5 years)
            Assets.TLT, // 20+yr Treasuries (duration = 17.4 years)
        };
        public virtual string FALLBACK => Assets.IEI; // 3-7yr Treasuries (duration = 4.5 years)
        public virtual string BENCHMARK => Assets.IEF;
        public virtual HashSet<int> LOOKBACKS => new HashSet<int>
        {
            21,  // 1 month
            42,  // 2 months
            63,  // 3 months
            84,  // 4 months
            105, // 5 months
        };
        #endregion
        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if USE_BOOK_RANGE
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/01/2005", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#elif USE_FULL_RANGE
            StartTime = startTime ?? DateTime.Parse("01/01/1965", CultureInfo.InvariantCulture);
            EndTime = endTime ?? DateTime.Now.Date - TimeSpan.FromDays(5);
#else
            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
#endif
            WarmupStartTime = StartTime - TimeSpan.FromDays(105);

            Deposit(Globals.INITIAL_CAPITAL);
            //CommissionPerShare = Globals.COMMISSION; // Connors does not consider commissions

            var universe = AddDataSources(UNIVERSE);
            var fallback = AddDataSource(FALLBACK);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            var TRANCHE_SIZE = 1.0 / (UNIVERSE.Count() + 1);
            var ALLOC_PER_LOOKBACK = TRANCHE_SIZE / LOOKBACKS.Count();

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(universe) || !HasInstrument(fallback) || !HasInstrument(benchmark))
                    continue;

                var weights = universe
                    .ToDictionary(
                        ds => ds.Instrument,
                        ds => LOOKBACKS
                            .Select(lb => ds.Instrument.Close[0] > ds.Instrument.Close[lb] ? ALLOC_PER_LOOKBACK : 0.0)
                            .Sum(w => w));

                weights[fallback.Instrument] = 1.0 - weights.Sum(w => w.Value);

                if (SimTime[0].DayOfWeek > NextSimTime.DayOfWeek)
                {
                    foreach (var i in weights.Keys)
                    {
                        int targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(targetShares - i.Position);
                    }
                }

                if (TradingDays > 0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    _plotter.AddStrategyHoldings(this, Instruments.Where(i => universe.Contains(i.DataSource) || fallback == i.DataSource));

                    foreach (var ds in universe)
                    {
                        var weight = 100.0 * ds.Instrument.Position * ds.Instrument.Close[0] / NetAssetValue[0];
                        _plotter.SelectChart(ds.Instrument.Symbol + " Exposure", "Date");
                        _plotter.SetX(SimTime[0]);
                        _plotter.Plot(ds.Instrument.Name, 10.0 * Math.Log(ds.Instrument.Close[0]) + 25.0);
                        _plotter.Plot("Exposure", weight);
                    }
                }

                if (IsDataSource)
                {
                    var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                    yield return Bar.NewOHLC(
                        this.GetType().Name, SimTime[0],
                        v, v, v, v, 0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                //_plotter.AddTargetAllocation(_alloc);
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
        public override void Report() => _plotter.OpenWith("SimpleReport");
        #endregion
    }
    #endregion
    #region public class Connors_AlphaFormula_EtfAvalanches
    public class Connors_AlphaFormula_EtfAvalanches : Algorithm
    {
        public override string Name => "Connors' ETF Avalanches";

        #region internal data
        private Plotter _plotter;
        #endregion
        #region inputs
        public virtual HashSet<string> UNIVERSE => new HashSet<string>
        {
            "SPY", // S&P 500
            "MDY", // US MidCap
            "EWA", // Australia
            "EWC", // Canada
            "EWD", // Sweden
            "EWG", // Germany
            "EWH", // Hong Kong Index
            "EWI", // Italy
            "EWJ", // Japan
            "EWK", // Belgium
            "EWL", // Switzerland
            "EWM", // Malaysia
            "EWN", // Netherlands
            "EWO", // Austria
            "EWP", // Spain
            "EWQ", // France
            "EWS", // Singapore
            "EWU", // UK
            "EWW", // Mexico
            "XLB", // US Materials
            "XLE", // US Energy
            "XLF", // US Financial
            "XLI", // US Industrial
            "XLK", // US Technology
            "XLP", // US Consumer Staples
            "XLU", // US Utilities
            "XLV", // US Health Care
            "XLY", // US Consumer Discretionary
            "DIA", // Dow Jones
            "QQQ", // NASDAQ
            "EZU", // Eurozone
            "EWZ", // Brazil
            "EWT", // Taiwan
            "IWM", // Russell 2000
            "EWY", // South Korea
            "EPP", // Pacific Ex-Japan
            "EFA", // EAFE
            "EEM", // Emerging Markets
            "EZA", // South Africa Index
            "FXI", // China
        };
        public virtual string IDLE_CASH => "SHY"; // 1-3yr Treasuries
        public virtual string BENCHMARK => "SH"; // S&P 500 Short
        #endregion
        #region ctor
        public Connors_AlphaFormula_EtfAvalanches()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //========== initialization ==========

#if USE_BOOK_RANGE
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndTime = SubclassedEndTime ?? DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#else
            StartTime = startTime ?? Globals.START_TIME;
            EndTime = endTime ?? Globals.END_TIME;
#endif
            WarmupStartTime = StartTime - TimeSpan.FromDays(252);

            Deposit(Globals.INITIAL_CAPITAL);
            //CommissionPerShare = Globals.COMMISSION; // Connors does not consider commissions

            var universe = AddDataSources(UNIVERSE);
            var idle = AddDataSource(IDLE_CASH);
            var benchmark = AddDataSource(BENCHMARK);

            //========== simulation loop ==========

            foreach (var s in SimTimes)
            {
                if (!HasInstruments(universe) || !HasInstrument(benchmark))
                    continue;

                var indicators = Instruments
                    .ToDictionary(
                        i => i,
                        i => new
                        {
                            longTermTrend = i.Close.Momentum(252)[0],
                            intermediateTermTrend = i.Close.Momentum(21)[0],
                            rsi = i.Close.RSI(2)[0],
                            volatility = i.Close.Volatility(100)[0]
                        });

                // start with weights representing current positions
                // note that the idle instrument is not included here
                const int NUM_POS = 5;
                var weights = Instruments
                    .ToDictionary(
                        i => i,
                        i => universe.Contains(i.DataSource) && i.Position < 0
                            ? -1.0 / NUM_POS
                            : 0.0);

                // exit, if RSI is below 15
                foreach (var i in Instruments)
                    if (i.Position < 0.0 && indicators[i].rsi < 15)
                        weights[i] = 0.0;

                // exit, if intermediate-term momentum turns positive
                foreach (var i in Instruments)
                    if (i.Position < 0.0 && indicators[i].intermediateTermTrend > 0.0)
                        weights[i] = 0.0;

                // determine number of new entries
                var numCurrentEntries = weights
                    .Where(w => w.Value < 0.0)
                    .Count();
                var numNewEntries = NUM_POS - numCurrentEntries;

                // sell short, if 
                // - we don't have a position
                // - trend filters are both negative
                // - RSI is above 70
                var newEntries = universe
                    .Select(ds => ds.Instrument)
                    .Where(i => i.Position == 0)
                    .Where(i => indicators[i].longTermTrend < 0.0 && indicators[i].intermediateTermTrend < 0.0)
                    .Where(i => indicators[i].rsi > 70)
                    .OrderByDescending(i => indicators[i].volatility)
                    .Take(numNewEntries)
                    .ToList();

                foreach (var i in newEntries)
                    weights[i] = -1.0 / NUM_POS;

                // invest in the idle instrument, if we are not planning to
                // fill all slots for mean-reverting instruments
                double idleWeight = ((double)numNewEntries - newEntries.Count()) / NUM_POS;
                weights[idle.Instrument] = idleWeight;

                // place orders
                foreach (var i in Instruments)
                {
                    int targetShares = (int)(Math.Sign(weights[i]) * Math.Floor(Math.Abs(weights[i]) * NetAssetValue[0] / i.Close[0]));

                    if (i != idle.Instrument)
                    {
                        // note: we don't rebalance our mean-reverting instruments

                        // enter positions w/ limit order
                        if (i.Position == 0 && targetShares != 0)
                            i.Trade(targetShares, OrderType.limitNextBar, i.Close[0] * 1.03);

                        // exit positions w/ market order
                        if (i.Position != 0 && targetShares == 0)
                            i.Trade(-i.Position);
                    }
                    else
                    {
                        // idle instrument
                        i.Trade(targetShares - i.Position);
                    }
                }

                if (TradingDays > 0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, benchmark.Instrument);
                    _plotter.AddStrategyHoldings(this, universe.Select(u => u.Instrument).Concat(new List<Instrument> { idle.Instrument }));
                }

                if (IsDataSource)
                {
                    var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                    yield return Bar.NewOHLC(
                        this.GetType().Name, SimTime[0],
                        v, v, v, v, 0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                //_plotter.AddTargetAllocation(_alloc);
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

    #region public class Connors_AlphaFormula_NonLeveragedPortfolio
    public class Connors_AlphaFormula_NonLeveragedPortfolio : LazyPortfolio
    {
        public override string Name => "Connors' Alpha Portfolio (No Leverage)";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>("algo:Connors_AlphaFormula_RisingAssets",        0.30),
            new Tuple<object, double>("algo:Connors_AlphaFormula_WeeklyMeanReversion", 0.30),
            new Tuple<object, double>("algo:Connors_AlphaFormula_DynamicTreasuries",   0.20),
            new Tuple<object, double>("algo:Connors_AlphaFormula_EtfAvalanches",       0.20),
        };

        public override string BENCH => Indices.PORTF_60_40;

#if USE_BOOK_RANGE
        public override DateTime START_TIME => DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
        public override DateTime END_TIME => DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#endif
    }
    #endregion
    #region public class Connors_AlphaFormula_LeveragedPortfolio
    public class Connors_AlphaFormula_LeveragedPortfolio : LazyPortfolio
    {
        public override string Name => "Connors' Alpha Portfolio (With 1.5x Leverage)";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>("algo:Connors_AlphaFormula_RisingAssets",        0.45),
            new Tuple<object, double>("algo:Connors_AlphaFormula_WeeklyMeanReversion", 0.45),
            new Tuple<object, double>("algo:Connors_AlphaFormula_DynamicTreasuries",   0.30),
            new Tuple<object, double>("algo:Connors_AlphaFormula_EtfAvalanches",       0.30),
        };
        public override string BENCH => Indices.PORTF_60_40;

#if USE_BOOK_RANGE
        public override DateTime START_TIME => DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
        public override DateTime END_TIME => DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);
#endif
    }
    #endregion
}

//==============================================================================
// end of file