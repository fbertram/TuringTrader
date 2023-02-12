//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account
// Description: Account class.
// History:     2022x25, FUB, created
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

namespace TuringTrader.SimulatorV2
{
    public interface ICache
    {
        public T Fetch<T>(string cacheId, Func<T> missFun);
    }

    /// <summary>
    /// Simple cache. This class is essential to TuringTrader's 
    /// automagic indicators.
    /// </summary>
    public class Cache : ICache
    {
        private Dictionary<string, object> _cache = new Dictionary<string, object>();

        /// <summary>
        /// Retrieve object from cache, or calculate result.
        /// </summary>
        /// <param name="cacheId">cache id</param>
        /// <param name="missFun">retrieval function for cache miss</param>
        /// <returns>cached object</returns>
        public T Fetch<T>(string cacheId, Func<T> missFun)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(cacheId))
                {
                    _cache[cacheId] = missFun();
                }

                return (T)_cache[cacheId];
            }
        }

        /// <summary>
        /// Clear cache contents.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Dummy cache. This class does not cache, but forward requests
    /// directly to the miss-function. This is the default for the
    /// data cache.
    /// </summary>
    public class DummyCache : ICache
    {
        public T Fetch<T>(string cacheId, Func<T> missFun) => missFun();
    }
}

//==============================================================================
// end of file
