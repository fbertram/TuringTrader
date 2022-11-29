//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Algorithm
// Description: Algorithm base class/ simulator core.
// History:     2021iv23, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
        public virtual string Name => this.GetType().Name;

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

            Account = new Account(this);
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
        public bool IsOptimizing { get; set; }
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
        public DateTime? StartDate { get => _startDate; set { _startDate = value; TradingCalendar.StartDate = (DateTime)_startDate - WarmupPeriod; } }

        private DateTime? _endDate = null;
        /// <summary>
        /// Simulation end date.
        /// </summary>
        public DateTime? EndDate { get => _endDate; set { _endDate = value; TradingCalendar.EndDate = (DateTime)value + TimeSpan.FromDays(7); } }

        private TimeSpan _warmupPeriod = TimeSpan.FromDays(5);

        /// <summary>
        /// Warmup period.
        /// </summary>
        public TimeSpan WarmupPeriod { get => _warmupPeriod; set { _warmupPeriod = value; TradingCalendar.StartDate = (DateTime)_startDate - WarmupPeriod; } }

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
        /// Algorithm's result as a list of bars.
        /// </summary>
        public List<BarType<OHLCV>> Result = null;

        private void _simLoop(Func<OHLCV> innerBarFun)
        {
            var tradingDays = TradingCalendar.TradingDays;
            IsFirstBar = true;
            IsLastBar = false;
            var bars = new List<BarType<OHLCV>>();

            for (int idx = 0; idx < tradingDays.Count; idx++)
            {
                SimDate = tradingDays[idx];

                if (SimDate >= StartDate && SimDate <= EndDate)
                {
                    NextSimDate = tradingDays[Math.Min(tradingDays.Count - 1, idx + 1)];
                    IsLastBar = NextSimDate > EndDate;

                    var ohlcv = innerBarFun();
                    bars.Add(new BarType<OHLCV>(SimDate, ohlcv));
                    IsFirstBar = false;
                }
            }

            //SimDate = default; // we need SimDate to calculate the last asset allocation
            //_cache.Clear(); // we need quote data to calculate the last asset allocation

            FitnessReturn = Account.NetAssetValue;
            FitnessRisk = Account.MaxDrawdown;
            FitnessValue = Account.AnnualizedReturn / Account.MaxDrawdown;

            Result = bars;
        }

        /// <summary>
        /// Simulation loop. This override's bar function returns void.
        /// Therefore, the algorithm's output series is generated from
        /// the trading activity in the algorithm's Account object.
        /// </summary>
        /// <param name="barFun"></param>
        public void SimLoop(Action barFun)
        {
            _simLoop(() =>
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
            _simLoop(() =>
            {
                var bar = barFun();
                Account.ProcessBar();
                return bar;
            });
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
        private Dictionary<string, object> _cache = new Dictionary<string, object>();
        /// <summary>
        /// Retrieve object from cache, or calculate in new task.
        /// </summary>
        /// <param name="cacheId">cache id</param>
        /// <param name="missFun">retrieval function for cache miss</param>
        /// <returns>cached object</returns>
        public Task<T> Cache<T>(string cacheId, Func<T> missFun)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(cacheId))
                    _cache[cacheId] = Task.Run(() => missFun());

                return (Task<T>)_cache[cacheId];
            }
        }
        #endregion
        #region assets & universes
        /// <summary>
        /// Load quotations for tradeable asset. Subsequent calls to
        /// this method with the same name will be served from a cache.
        /// </summary>
        /// <param name="name">name of asset</param>
        /// <returns>asset</returns>
        public TimeSeriesAsset Asset(string name) => DataSource.LoadAsset(this, name);

        /// <summary>
        /// Run v2 algorithm and bring its results in as an asset.
        /// </summary>
        /// <param name="algo"></param>
        /// <returns></returns>
        public TimeSeriesAsset Asset(Algorithm algo) => DataSource.LoadAsset(this, algo);

        /// <summary>
        /// Load quotations or run algorithm.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public TimeSeriesAsset Asset(object obj) => obj as string != null
                ? Asset(obj as string)
                : Asset(obj as Algorithm);

        /// <summary>
        /// Return constituents of universe at current simulator timestamp.
        /// Please note that not all data feeds support this feature. For those
        /// feeds, the list of symbols returned might be inaccurate or incomplete.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HashSet<string> Universe(string name)
        {
            return DataSourceV1.GetConstituents(this, name);
        }
        #endregion
        #region reporting
        public Plotter Plotter = null; // instantiated in constructor
        public virtual void Report() => Plotter.OpenWith("SimpleReport");
        #endregion
        #region orders & accounting
        public Account Account { get; set; } = null; // instantiated in constructor
        public Dictionary<string, double> Positions { get => Account.Positions; }
        public double NetAssetValue { get => Account.NetAssetValue; }
        public double Cash { get => Account.Cash; }
        #endregion

        public virtual void Run() { }
    }
}

//==============================================================================
// end of file
