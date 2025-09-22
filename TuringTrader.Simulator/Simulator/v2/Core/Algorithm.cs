//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Algorithm
// Description: Algorithm base class/ simulator core.
// History:     2021iv23, EFB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2025, Bertram Enterprises LLC dba TuringTrader.
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

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2.Indicators;
using static TuringTrader.SimulatorV2.IAccount;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Base class for v2 trading algorithms.
    /// </summary>
    public abstract class Algorithm : Simulator.IAlgorithm
    {
        /// <summary>
        /// Return algorithm's friendly name.
        /// </summary>
        public virtual string Name => GetType().Name;

        #region instantiation
        /// <summary>
        /// Initialize trading algorithm. Most trading algorithms will
        /// only do very little here; the majority of the initialization
        /// should be performed in Run(), to allow multiple runs of
        /// the same instance.
        /// </summary>
        protected Algorithm()
        {
            // create a dictionary of optimizer parameters
            OptimizerParams = new Dictionary<string, OptimizerParam>();
            foreach (OptimizerParam param in OptimizerParam.GetParams(this))
                OptimizerParams[param.Name] = param;

            Account = new Account_Default(this);
            TradingCalendar = new TradingCalendar_US(this);
            Plotter = new Plotter(this);
        }
        /// <summary>
        /// Clone algorithm, including all optimizer parameters. The application uses
        /// this method to clone the 'master' instance, and create new algorithm 
        /// instances before running them.
        /// </summary>
        /// <returns>new algorithm instance</returns>
        public Simulator.IAlgorithm Clone()
        {
            Type algoType = GetType();
            Algorithm clonedInstance = (Algorithm)Activator.CreateInstance(algoType);

            // apply optimizer values to new instance
            foreach (OptimizerParam parameter in OptimizerParams.Values)
            {
                clonedInstance.OptimizerParams[parameter.Name].IsEnabled = parameter.IsEnabled;
                clonedInstance.OptimizerParams[parameter.Name].Start = parameter.Start;
                clonedInstance.OptimizerParams[parameter.Name].End = parameter.End;
                clonedInstance.OptimizerParams[parameter.Name].Step = parameter.Step;
                clonedInstance.OptimizerParams[parameter.Name].Value = parameter.Value;
            }

            return clonedInstance;
        }
        /// <summary>
        /// Return true, if algorithm is used as a data source. Use this feature
        /// to disable optional operations that are time or memory consuming.
        /// </summary>
        public bool IsDataSource { get; set; } = false;
        #endregion
        #region optimization
        /// <summary>
        /// Return full set of optimizer parameters.
        /// </summary>
        public Dictionary<string, OptimizerParam> OptimizerParams { get; private set; }
        /// <summary>
        /// String representation of the current settings of all
        /// optimizable parameters.
        /// </summary>
        public string OptimizerParamsAsString
        {
            get
            {
                string retval = "";
                foreach (var parameter in OptimizerParams.Values.OrderBy(p => p.Name))
                {
                    retval += retval.Length > 0 ? ", " : "";
                    retval += string.Format("{0}={1}", parameter.Name, parameter.Value);
                }
                return retval;
            }
        }
        /// <summary>
        /// Determine if optimizer parameter set is valid.
        /// </summary>
        public virtual bool IsOptimizerParamsValid => true;
        /// <summary>
        /// Return true, if algorithm is currently being optimized. Use this feature
        /// to disable optional operations that are time or memory consuming.
        /// </summary>
        public bool IsOptimizing { get; set; } = false;
        /// <summary>
        /// Return algorithm's fitness value (return component).
        /// </summary>
        public virtual double FitnessReturn { get; set; }
        /// <summary>
        /// Return algorithm's fitness value (risk component).
        /// </summary>
        public virtual double FitnessRisk { get; set; }
        /// <summary>
        /// Return algorithm's fitness value (composite value).
        /// </summary>
        public virtual double FitnessValue { get; set; }
        #endregion
        #region simulation range & loop
        /// <summary>
        /// Trading calendar, converting simulation date range to
        /// enumerable of valid trading days.
        /// </summary>
        public ITradingCalendar TradingCalendar { get; set; } = null; // instantiated in constructor

        /// <summary>
        /// Simulation start date.
        /// </summary>
        public virtual DateTime? StartDate { get; set; } = null;

        /// <summary>
        /// Simulation end date.
        /// </summary>
        public virtual DateTime? EndDate { get; set; } = null;

        /// <summary>
        /// Warmup period.This period comes before StartDate. It is crucial
        /// to have enough warmup before beginning to trade, so that
        /// indicators can settle on their correct values.
        /// </summary>
        public virtual TimeSpan WarmupPeriod { get; set; } = TimeSpan.FromDays(5);

        /// <summary>
        /// Cooldown period. This period follows EndDate. It is important to
        /// add a few days to the end of the backtest to make sure the simulator
        /// can calculate NextSimDate accordingly.
        /// </summary>
        public virtual TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromDays(5);
        /// <summary>
        /// Current simulation timestamp.
        /// </summary>
        public DateTime SimDate { get; private set; } = default;

        /// <summary>
        /// Next simulation timestamp. This is useful for determining 
        /// the end of the week/ month/ year.
        /// </summary>
        public DateTime NextSimDate { get; private set; } = default;

        /// <summary>
        /// Determine if this is the first bar.
        /// </summary>
        public bool IsFirstBar { get; private set; } = false;

        /// <summary>
        /// Determine if this is the last bar.
        /// </summary>
        public bool IsLastBar { get; private set; } = false;

        /// <summary>
        /// Equity curve generated by this algorithm.
        /// </summary>
        public List<BarType<OHLCV>> EquityCurve = null;

        private int? _threadId = null;
        private int? _taskId = null;
        private List<BarType<OHLCV>> _simLoop(Func<double, OHLCV> innerBarFun, double init, bool isLambda)
        {
            // NOTE: _simLoop may be reentered by Lambda. However,
            //       this reenantrance may only happen on the same
            //       thread as the algorithm's main SimLoop, resulting
            //       in _simLoop calls that are nested but not parallel.
            //       The code here saves and restores the properties
            //       SimDate, NextSimDate, IsFirstBar, and IsLastBar,
            //       so that this may happen without altering the
            //       simulator's state.

            // save current simulator state
            var _isFirstBar = IsFirstBar;
            var _isLastBar = IsLastBar;
            var _simDate = SimDate;
            var _nextSimDate = NextSimDate;

            // make sure _simloop calls are properly nested and
            // only called from single thread/ task to prevent corruption
            // of simulator state.
            // this may happen when using Lambda inside indicators
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            _threadId = _threadId ?? currentThreadId;
            if (!(_threadId == currentThreadId))
                throw new Exception("SimLoop called from multiple threads");

            var currentTaskId = Task.CurrentId;
            _taskId = _taskId ?? currentTaskId;
            if (!(_taskId == currentTaskId))
                throw new Exception("SimLoop called from multiple tasks");

            var bars = new List<BarType<OHLCV>>();

            {
                var tradingDays = TradingCalendar.TradingDays;
                IsFirstBar = true;
                IsLastBar = false;

                var prev = init;
                for (int idx = 0; idx < tradingDays.Count; idx++)
                {
                    var simDate = tradingDays[idx];
                    var nextSimDate = tradingDays[Math.Min(tradingDays.Count - 1, idx + 1)];

                    // NOTE: generally, we only call the client code within the sim range.
                    //       Lambdas are an exception here, as they likely require a warmup.
                    //       Data sources have their range set to include the parent's warmup.
                    if (isLambda || (simDate >= StartDate && simDate <= EndDate && simDate <= DateTime.Now))
                    {
                        SimDate = simDate;
                        NextSimDate = nextSimDate;
                        IsLastBar = NextSimDate > EndDate;

                        var ohlcv = innerBarFun(prev); // execute user logic
                        prev = ohlcv.Close;

                        //if (!IsOptimizing)
                        bars.Add(new BarType<OHLCV>(SimDate, ohlcv));
                        IsFirstBar = false;
                    }
                }
            }

            // restore previous simulator state
            // NOTE: we don't do this on the outermost _simloop,
            // so that we maintain the last valid SimDate. This
            // is required, so that NetAssetValue stays valid
            // even after the simulation finished.
            if (_simDate != default)
            {
                IsFirstBar = _isFirstBar;
                IsLastBar = _isLastBar;
                SimDate = _simDate;
                NextSimDate = _nextSimDate;
            }

            return bars;
        }
        private void _simLoopOuter(Func<OHLCV> innerBarFun)
        {

            var bars = _simLoop((prev) => innerBarFun(), default, false);

            //SimDate = default; // we need SimDate to calculate the last asset allocation
            //_cache.Clear(); // we need quote data to calculate the last asset allocation

            // NOTE: we only calculate fitness values for default accounts.
            //       for all other accounts, this value needs to be
            //       calculated at the end of the algorithm's Run method.
            var defaultAccount = Account as Account_Default;
            if (defaultAccount != null)
            {
#if false
                // retired 2024vi27: fitness = return-on-max-drawdown
                FitnessReturn = Account.NetAssetValue;
                FitnessRisk = defaultAccount.MaxDrawdown;
                FitnessValue = defaultAccount.AnnualizedReturn / defaultAccount.MaxDrawdown;
#else
                // new 2024vi27: fitness = martin ratio (ulcer performance index)
                var name = string.Format("{0}-{1:X}", GetType().Name, GetHashCode());
                var equityCurve = new TimeSeriesAsset(this, name, bars);
                var ulcerIndex = equityCurve.Close.UlcerIndex(bars.Count)[0];

                FitnessReturn = Account.NetAssetValue;
                FitnessRisk = defaultAccount.MaxDrawdown;
                FitnessValue = defaultAccount.AnnualizedReturn / ulcerIndex;
#endif
            }

            EquityCurve = bars;
        }

        /// <summary>
        /// Simulation loop. This override's bar function returns void.
        /// Therefore, the algorithm's output series is generated from
        /// the trading activity in the algorithm's Account object.
        /// </summary>
        /// <param name="barFun"></param>
        public void SimLoop(Action barFun)
        {
            _simLoopOuter(() =>
            {
                barFun();
                return Account.ProcessBar();
            });
        }

        /// <summary>
        /// Simulation loop. This override's bar function returns a
        /// bar object. This object is used to create teh algorithm's
        /// output series.
        /// </summary>
        /// <param name="barFun"></param>
        public void SimLoop(Func<OHLCV> barFun)
        {
            _simLoopOuter(() =>
            {
                var bar = barFun();
                var nav = Account.ProcessBar();
                return bar != null ? bar : nav;
            });
        }

        /// <summary>
        /// Calculate indicator from lambda function.
        /// </summary>
        /// <param name="cacheId">unique cache id</param>
        /// <param name="barFun">lambda function</param>
        /// <param name="init">initial value</param>
        /// <returns>output time series</returns>
        public TimeSeriesFloat Lambda(string cacheId, Func<double, double> barFun, double init)
        {
            // NOTE: we are assuming Lambda results to be private
            //       to the algorithm instance, because users might
            //       not be mindful about truly unique names.
            //       This has two implications:
            //       (1) we use the algorithm's hash code as part
            //           of the name
            //       (2) we do not store the results in DataCache

            var name = string.Format("Lambda({0}-{1:X})", cacheId, this.GetHashCode());

            return ObjectCache.Fetch(
                name,
                () =>
                {

                    // run simloop
                    var bars = _simLoop((prev) => new OHLCV(0.0, 0.0, 0.0, barFun(prev), 0.0), init, true);
                    var data = Task.FromResult(bars
                        .Select(ohlcv => new BarType<double>(ohlcv.Date, ohlcv.Value.Close))
                        .ToList());

                    return new TimeSeriesFloat(this, name, data);
                });
        }

        /// <summary>
        /// Calculate indicator from lambda function.
        /// </summary>
        /// <param name="cacheId">unique cache id></param>
        /// <param name="barFun">lambda function</param>
        /// <returns>output time series</returns>
        public TimeSeriesFloat Lambda(string cacheId, Func<double> barFun)
        {
            return Lambda(
                cacheId,
                (prev) => barFun(),
                default);
        }

        /// <summary>
        /// Return simulation progress as a number between 0 and 100.
        /// This is used to display a progress bar during simulation.
        /// Note that this method of calculation shows the percentage
        /// of simulation range completed, which is not identical to
        /// the percentage of simulation time completed.
        /// </summary>
        public virtual double Progress => StartDate != null && EndDate != null
            ? 100.0 * Math.Max(0.0, (SimDate - (DateTime)StartDate).TotalDays) / Math.Max(1.0, ((DateTime)EndDate - (DateTime)StartDate).TotalDays)
            : 0.0;

        #endregion
        #region cache functionality
        /// <summary>
        /// Object cache, used to store algorithm-specific objects. Most
        /// importantly, algorithms store all time series in the cache,
        /// including those for quotes and indicators. It is worth noting
        /// that there is a separate cache for  the actual raw data.
        /// </summary>
        public ICache ObjectCache = new Cache();
        /// <summary>
        /// Data cache, used to store algorithm data. The default behavior
        /// for this cache is a dummy, bypassing all requests directly to
        /// the miss function. Because the object cache is still active,
        /// this only prevents algorithms from sharing quotes and indicator
        /// results. However, the optimizer will activate this cache,
        /// reducing the memory footprint and increasing execution speed.
        /// </summary>
        public ICache DataCache = new DummyCache();
        #endregion
        #region assets & universes
        /// <summary>
        /// Load quotations for tradeable asset. Subsequent calls to
        /// this method with the same name will be served from a cache.
        /// </summary>
        /// <param name="name">name of asset</param>
        /// <returns>asset time series</returns>
        public virtual TimeSeriesAsset Asset(string name) => DataSource.LoadAsset(this, name);

        /// <summary>
        /// Run v2 algorithm and bring its results in as an asset.
        /// Subsequent calls to this method with the same generator
        /// will be served from a cache.
        /// </summary>
        /// <param name="generator">algorithm used as asset</param>
        /// <returns>asset time series</returns>
        public virtual TimeSeriesAsset Asset(Simulator.IAlgorithm generator) => DataSource.LoadAsset(this, generator);

        /// <summary>
        /// Load quotations or run algorithm, dependent on the type of 
        /// the object passed in.
        /// </summary>
        /// <param name="obj">string or algorithm</param>
        /// <returns>asset time series</returns>
        public virtual TimeSeriesAsset Asset(object obj)
        {
            var objString = obj as string;
            var objAlgorithm = obj as Simulator.IAlgorithm;

            if (objString != null) return Asset(objString);
            if (objAlgorithm != null) return Asset(objAlgorithm);

            throw new Exception(string.Format("Can't load asset for {0}", obj.ToString()));
        }

        /// <summary>
        /// Load asset data through custom code. Subsequent calls to this
        /// method with the same name will be served from the cache.
        /// </summary>
        /// <param name="name">name of asset</param>
        /// <param name="retrieve">retrieval function for custom data</param>
        /// <returns></returns>
        public virtual TimeSeriesAsset Asset(string name, Func<List<BarType<OHLCV>>> retrieve) => DataSource.CustomGetAsset(this, name, retrieve);

        /// <summary>
        /// Return constituents of universe at current simulator timestamp.
        /// Please note that not all data feeds support this feature. For those
        /// feeds, the list of symbols returned might be inaccurate or incomplete.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual HashSet<string> Universe(string name) => DataSource.Universe(this, name);
        #endregion
        #region reporting
        /// <summary>
        /// Plotter for default report.
        /// </summary>
        public Plotter Plotter = null; // instantiated in constructor
        /// <summary>
        /// Render default report.
        /// </summary>
        public virtual void Report() => Plotter.OpenWith("SimpleReport");
        #endregion
        #region orders & accounting
        /// <summary>
        /// Account model.
        /// </summary>
        public IAccount Account { get; set; } = null; // instantiated in constructor
        /// <summary>
        /// Positions currently held by algorithm. Returns a dictionary keyed
        /// with the nickname of the assets, and a value representing the fraction
        /// of the accounts NAV held.
        /// </summary>
        public Dictionary<string, double> Positions { get => Account.Positions; }
        /// <summary>
        /// Positions held by algorithm and its child algorithms.
        /// </summary>
        public Dictionary<string, double> PositionsFlattened
        {
            get
            {
                var holdings = new Dictionary<string, double>();

                void addAssetAllocation(Algorithm algo, double scale = 1.0)
                {
                    foreach (var kv in algo.Positions)
                    {
                        var asset = algo.Asset(kv.Key);
                        var child = asset.Meta.Generator;

                        if (child != null)
                        {
                            addAssetAllocation(child, kv.Value * scale);
                        }
                        else
                        {
                            var key = asset.Name;

                            if (!holdings.ContainsKey(key))
                                holdings[key] = 0.0;
                            holdings[key] += kv.Value * scale;
                        }
                    }
                }
                addAssetAllocation(this);

                return holdings;
            }
        }
        /// <summary>
        /// Retrieve trade log. Note that this log only contains trades executed
        /// and not orders that were not executed.
        /// </summary>
        public List<IAccount.OrderReceipt> TradeLog { get => Account.TradeLog; }
        /// <summary>
        /// Retrieve trade log of this algorithm and its childs.
        /// </summary>
        public List<IAccount.OrderReceipt> TradeLogFlattened
        {
            get
            {
                var tradelog = new List<IAccount.OrderReceipt>();

                if (Account.TradeLog == null || Account.TradeLog.Count == 0)
                    return tradelog;

                var allEodAllocations = new Dictionary<Algorithm, List<Tuple<DateTime, Dictionary<string, double>>>>();
                var allTradeDates = new HashSet<DateTime>();

                // collect EOD asset allocations
                // - one row for each day in the trade log
                // - assets referenced by their nickname
                void collectEodAllocation(Algorithm algo)
                {
                    var eodAllocation = new List<Tuple<DateTime, Dictionary<string, double>>>();

                    if (algo.Account.TradeLog != null)
                    {
                        foreach (var trade in algo.Account.TradeLog)
                        {
                            // new date: copy previous allocation
                            // BUGBUG: this is inaccurate. Due to the fluctuation
                            //         of asset prices, the new line has deviated
                            //         from the previous allocation.
                            //         However, because a typical strategy adjusts
                            //         all its assets weights simultaneously, this
                            //         shouldn't matter too much.
                            if (eodAllocation.Count == 0)
                            {
                                // create very first asset allocation entry
                                eodAllocation.Add(Tuple.Create(
                                    trade.OrderTicket.SubmitDate,
                                    new Dictionary<string, double>()));
                            }
                            else if (eodAllocation.Last().Item1 != trade.OrderTicket.SubmitDate)
                            {
                                // copy asset allocation entry from previous,
                                // but remove flat allocations
                                var baseAlloc = eodAllocation.Last().Item2
                                    .Where(kv => kv.Value != 0.0)
                                    .ToDictionary(
                                        kv => kv.Key,
                                        kv => kv.Value);

                                eodAllocation.Add(Tuple.Create(
                                    trade.OrderTicket.SubmitDate,
                                    new Dictionary<string, double>(baseAlloc)));
                            }

                            // adjust the asset allocation according to the order
                            eodAllocation.Last().Item2[trade.OrderTicket.Name] = trade.OrderTicket.TargetAllocation;

                            // if an asset is referring to a child strategy,
                            // collect that child strategy's allocations
                            if (algo.Asset(trade.OrderTicket.Name).Meta.Generator != null
                            && !allEodAllocations.ContainsKey(algo.Asset(trade.OrderTicket.Name).Meta.Generator))
                                collectEodAllocation(algo.Asset(trade.OrderTicket.Name).Meta.Generator);

                            // record each day with a trade
                            if (!allTradeDates.Contains(trade.OrderTicket.SubmitDate))
                                allTradeDates.Add(trade.OrderTicket.SubmitDate);
                        }
                    }
                    allEodAllocations[algo] = eodAllocation;
                }
                collectEodAllocation(this);

                // get asset allocation for specific date
                // - assets referenced by their nickname
                // - all child strategies resolved
                Tuple<DateTime, Dictionary<string, double>> getAllocation(Algorithm algo, DateTime date)
                {
                    // BUGBUG: this is inaccurate. Due to the fluctuation
                    //         of asset prices, the new line has deviated
                    //         from the previous allocation.
                    //         In this case, this may make a notible difference,
                    //         when the asset is a child strategy that is
                    //         then replaced with its internal holdings.
                    var eodAlloc = allEodAllocations[algo]
                        .Where(a => a.Item1 <= date)
                        .LastOrDefault();

                    if (eodAlloc == null) return Tuple.Create(date, new Dictionary<string, double>());

                    var resolvedAlloc = Tuple.Create(eodAlloc.Item1, new Dictionary<string, double>());

                    foreach (var asset in eodAlloc.Item2)
                    {
                        var childAlgo = algo.Asset(asset.Key).Meta.Generator;

                        if (childAlgo != null)
                        {
                            // asset is child strategy: resolve
                            var childAlloc = getAllocation(childAlgo, date);

                            foreach (var child in childAlloc.Item2)
                            {
                                if (!resolvedAlloc.Item2.ContainsKey(child.Key))
                                    resolvedAlloc.Item2[child.Key] = 0.0;
                                resolvedAlloc.Item2[child.Key] += asset.Value * child.Value;
                            }

                            // if the child strategy's last trade is more recent,
                            // use that date as the allocation's latest
                            if (resolvedAlloc.Item1 < childAlloc.Item1)
                                resolvedAlloc = Tuple.Create(childAlloc.Item1, resolvedAlloc.Item2);
                        }
                        else
                        {
                            // atomic asset, copy as-is
                            if (!resolvedAlloc.Item2.ContainsKey(asset.Key))
                                resolvedAlloc.Item2[asset.Key] = 0.0;
                            resolvedAlloc.Item2[asset.Key] += asset.Value;
                        }
                    }

                    return resolvedAlloc;
                }

                foreach (var date in allTradeDates.OrderBy(d => d))
                {
                    var alloc = getAllocation(this, date);

                    if (alloc != null)
                    {
                        foreach (var kv in alloc.Item2)
                        {
                            tradelog.Add(new IAccount.OrderReceipt(
                                new OrderTicket(
                                    date, // submitDate, 
                                    kv.Key, // symbol, 
                                    kv.Value, // targetAllocation, 
                                    OrderType.openNextBar, // orderType, 
                                    0.0 // orderPrice
                                ),
                                date, // execDate
                                kv.Value, // orderSize
                                0.0, // fillPrice,
                                0.0, // orderAmount,
                                0.0)); // frictionAmount));
                        }
                    }
                }

                return tradelog;
            }
        }
        /// <summary>
        /// Algorithm's current net asset value. Expressed in currency.
        /// </summary>
        public double NetAssetValue { get => Account.NetAssetValue; }
        /// <summary>
        /// Algorithm's current cash holdings. Expressed as a fraction of the
        /// account's NAV.
        /// </summary>
        public double Cash { get => Account.Cash; }
        #endregion

        /// <summary>
        /// Run backtest.
        /// </summary>
        public virtual void Run() { }
    }
}

//==============================================================================
// end of file
