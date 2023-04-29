﻿//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Algorithm
// Description: Algorithm base class/ simulator core.
// History:     2021iv23, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TuringTrader.Optimizer;

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
        public ITradingCalendar TradingCalendar { get; set; } = new TradingCalendar_US();

        private DateTime? _startDate = null;
        /// <summary>
        /// Simulation start date.
        /// </summary>
        public DateTime? StartDate { get => _startDate; set { _startDate = value; if (value != null) TradingCalendar.StartDate = (DateTime)_startDate - WarmupPeriod; } }

        private DateTime? _endDate = null;
        /// <summary>
        /// Simulation end date.
        /// </summary>
        public DateTime? EndDate { get => _endDate; set { _endDate = value; if (value != null) TradingCalendar.EndDate = (DateTime)value + CooldownPeriod; } }

        private TimeSpan _warmupPeriod = TimeSpan.FromDays(5);

        /// <summary>
        /// Warmup period.This period comes before StartDate. It is crucial
        /// to have enough warmup before beginning to trade, so that
        /// indicators can settle on their correct values.
        /// </summary>
        public TimeSpan WarmupPeriod { get => _warmupPeriod; set { _warmupPeriod = value; TradingCalendar.StartDate = (DateTime)_startDate - WarmupPeriod; } }

        private TimeSpan _cooldownPeriod = TimeSpan.FromDays(5);
        /// <summary>
        /// Cooldown period. This period follows EndDate. It is important to
        /// add a few days to the end of the backtest to make sure the simulator
        /// can calculate NextSimDate accordingly.
        /// </summary>
        public TimeSpan CooldownPeriod { get => _cooldownPeriod; set { _cooldownPeriod = value; TradingCalendar.EndDate = (DateTime)_endDate + CooldownPeriod; } }
        /// <summary>
        /// Current simulation timestamp.
        /// </summary>
        public DateTime SimDate { get => _simDate; private set { _simDate = value; } }
        [ThreadStatic]
        private static DateTime _simDate;
        //public DateTime SimDate { get; private set; } = default;

        /// <summary>
        /// Next simulation timestamp. This is useful for determining 
        /// the end of the week/ month/ year.
        /// </summary>
        public DateTime NextSimDate { get => _nextSimDate; private set { _nextSimDate = value; } }
        [ThreadStatic]
        private static DateTime _nextSimDate;
        //public DateTime NextSimDate { get; private set; } = default;

        /// <summary>
        /// Determine if this is the first bar.
        /// </summary>
        public bool IsFirstBar { get => _isFirstBar; set { _isFirstBar = value; } }
        [ThreadStatic]
        private static bool _isFirstBar;
        //public bool IsFirstBar { get; private set; } = false;

        /// <summary>
        /// Determine if this is the last bar.
        /// </summary>
        public bool IsLastBar { get => _isLastBar; set { _isLastBar = value; } }
        [ThreadStatic]
        private static bool _isLastBar;
        //public bool IsLastBar { get; private set; } = false;

        /// <summary>
        /// Equity curve generated by this algorithm.
        /// </summary>
        public List<BarType<OHLCV>> EquityCurve = null;

        private List<BarType<OHLCV>> _simLoop(Func<double, OHLCV> innerBarFun, double init = 0.0)
        {
            // NOTE: the properties SimDate, NextSimDate, IsFirstBar, and
            //       IsLastBar are used by the simulator engine and
            //       indicators alike when accessing any TimeSeries.
            //       Because indicators run in separate tasks and because
            //       Lambda causes reentrance to _simLoop, it is crucial
            //       that the properties are private to the current task.
            //       It is assumed that parallel tasks run in separate threads,
            //       so that this is possible.

            var tradingDays = TradingCalendar.TradingDays;
            IsFirstBar = true;
            IsLastBar = false;
            var bars = new List<BarType<OHLCV>>();

            var prev = init;
            for (int idx = 0; idx < tradingDays.Count; idx++)
            {
                var simDate = tradingDays[idx];
                var nextSimDate = tradingDays[Math.Min(tradingDays.Count - 1, idx + 1)];

                if (simDate >= StartDate && simDate <= EndDate)
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

            return bars;
        }
        private void _simLoopOuter(Func<OHLCV> innerBarFun)
        {

            var bars = _simLoop((prev) => innerBarFun());

            //SimDate = default; // we need SimDate to calculate the last asset allocation
            //_cache.Clear(); // we need quote data to calculate the last asset allocation

            // NOTE: we only calculate fitness values for default accounts.
            //       for all other accounts, this value needs to be
            //       calculated at the end of the algorithm's Run method.
            var defaultAccount = Account as Account_Default;
            if (defaultAccount != null)
            {
                FitnessReturn = Account.NetAssetValue;
                FitnessRisk = defaultAccount.MaxDrawdown;
                FitnessValue = defaultAccount.AnnualizedReturn / defaultAccount.MaxDrawdown;
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
                Account.ProcessBar();
                return bar;
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
                    // save current simloop status
                    // NOTE: this is required, even if the
                    //       properties are [ThreadStatic],
                    //       because Lambda may be running
                    //       in the *same* task as SimLoop
                    var _isFirstBar = IsFirstBar;
                    var _isLastBar = IsLastBar;
                    var _simDate = SimDate;
                    var _nextSimDate = NextSimDate;

                    // run simloop
                    var bars = _simLoop((prev) => new OHLCV(0.0, 0.0, 0.0, barFun(prev), 0.0), init);
                    var data = Task.FromResult(bars
                        .Select(ohlcv => new BarType<double>(ohlcv.Date, ohlcv.Value.Close))
                        .ToList());

                    // restore previous simloop status
                    IsFirstBar = _isFirstBar;
                    IsLastBar = _isLastBar;
                    SimDate = _simDate;
                    NextSimDate = _nextSimDate;

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
                0.0);
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
