//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Algorithm
// Description: Algorithm base class/ simulator core.
// History:     2021iv23, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Enterprises LLC
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

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// Base class for trading algorithms.
    /// </summary>
    public abstract class Algorithm : AlgorithmApi
    {
        #region simulation range & loop
        /// <summary>
        /// Simulation start date.
        /// </summary>
        public DateTime StartDate { get => TradingCalendar.StartDate; set => TradingCalendar.StartDate = value; }

        /// <summary>
        /// Simulation end date.
        /// </summary>
        public DateTime EndDate { get => TradingCalendar.EndDate; set => TradingCalendar.EndDate = value; }

        /// <summary>
        /// Trading calendar, converting simulation date range to
        /// enumerable of valid trading days.
        /// </summary>
        public ITradingCalendar TradingCalendar { get; set; } = new TradingCalendar_US();

        /// <summary>
        /// Current simulation timestamp.
        /// </summary>
        public DateTime SimDate { get; private set; } = default;
        public DateTime NextSimDate { get; private set; } = default;
        public bool IsLastBar { get => NextSimDate == SimDate; }

        /// <summary>
        /// Simulation loop.
        /// </summary>
        /// <param name="barFun"></param>
        public void SimLoop(Action barFun)
        {
            var tradingDays = TradingCalendar.TradingDays
                .ToList();

            for (int idx = 0; idx < tradingDays.Count; idx++)
            {
                SimDate = tradingDays[idx];
                NextSimDate = idx < tradingDays.Count - 1 ? tradingDays[idx + 1] : SimDate;

                barFun();

                Account.ProcessOrders();
            }

            SimDate = default;
        }
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
        public TimeSeriesOHLCV Asset(string name)
        {
            return V1DataInterface.LoadAsset(this, name, TradingCalendar);
        }

        /// <summary>
        /// Return constituents of universe at current simulator timestamp.
        /// Please note that not all data feeds support this feature. For those
        /// feeds, the list of symbols returned might be inaccurate or incomplete.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<string> Universe(string name)
        {
            return V1DataInterface.GetConstituents(this, name, SimDate);
        }
        #endregion
        #region reporting
        public Plotter Plotter = new Plotter();
        public override void Report() => Plotter.OpenWith("SimpleChart");
        #endregion
        #region orders & accounting
        public Account Account { get; set; } = null; // instantiated in constructor
        public double NetAssetValue { get => Account.NetAssetValue; }
        #endregion

        #region constructor
        public Algorithm()
        {
            Account = new Account(this);
        }
        #endregion
    }
}

//==============================================================================
// end of file
