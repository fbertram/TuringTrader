//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Cache
// Description: data cache, to reduce memory footprint and cpu use
// History:     2018ix21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

//#define DISABLE_CACHE
// with DISABLE_CACHE defined, no data will be cached, 
// and retrieval function will be called directly
// please note that indicators require caching to
// be enabled

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    #region public class CacheId
    /// <summary>
    /// Unique ID to identify cache objects.
    /// </summary>
    public class CacheId
    {
        #region internal helpers
        private void CalcKey(IEnumerable<int> parameterIds)
        {
            // on top of the parameter ids, we also need to uniquely identify the call stack
            // currently, we use only the native offset for this
            // do we need to use the method name as well?
            IEnumerable<int> stackFrames = new System.Diagnostics.StackTrace().GetFrames()
                .Select(f => f.GetNativeOffset().GetHashCode());

            var subIds = parameterIds
                .Concat(stackFrames);

            // see https://stackoverflow.com/questions/7278136/create-hash-value-on-a-list
            const int seed = 487;
            const int modifier = 31;
            unchecked
            {
                Key = subIds
                    .Aggregate(seed, (current, item) => (current * modifier) + item);
            }
        }
        #endregion

        #region public int Key
        /// <summary>
        /// Cryptographic key
        /// </summary>
        public int Key
        {
            get;
            private set;
        }
        #endregion

        #region public CacheId(IEnumerable<int> parameterIds)
        /// <summary>
        /// Create unique cryptographic key from a list of integer parameters, as
        /// well as the current stack trace. This ID is used to uniquely identify 
        /// auto-magically created indicator functors.
        /// </summary>
        /// <param name="parameterIds">list of integer parameters</param>
        /// <returns>unique id</returns>
        public CacheId(IEnumerable<int> parameterIds)
        {
            CalcKey(parameterIds);
        }
        #endregion
        #region public CacheId(params int[] parameterIds)
        /// <summary>
        /// Create unique cryptographic key from a list of integer parameters, as
        /// well as the current stack trace. This ID is used to uniquely identify 
        /// auto-magically created indicator functors.
        /// </summary>
        /// <param name="parameterIds">list of integer parameters</param>
        /// <returns>unique id</returns>
        public CacheId(params int[] parameterIds)
        {
            CalcKey(parameterIds.AsEnumerable());
        }
        #endregion
    }

    #endregion

    /// <summary>
    /// Cache template class. The cache is at the core of TuringTrader's
    /// auto-magic indicator objects, as well as the the data sources. Cache
    /// objects are accessed via a cryptographic key, which is typically
    /// created with either the object.GetHashCode() method, or the
    /// Cache.UniqueId() method.
    /// </summary>
    /// <typeparam name="T">type of cache</typeparam>
    public static class Cache<T>
    {

        #region internal data
        private static Dictionary<int, T> _cache = new Dictionary<int, T>();
        #endregion
        #region static public T GetData(CacheId key, Func<T> initialRetrieval)
        /// <summary>
        /// Retrieve data from cache.
        /// </summary>
        /// <param name="id">unique ID of data</param>
        /// <param name="initialRetrieval">lambda to retrieve data not found in cache</param>
        /// <returns>cached data</returns>
        static public T GetData(CacheId id, Func<T> initialRetrieval)
        {
#if DISABLE_CACHE
            return initialRetrieval();
#else
            int key = id.Key;

            //lock (_lockCache)
            lock(_cache)
            {
                if (!_cache.ContainsKey(key))
                    _cache[key] = initialRetrieval();

                return _cache[key];
            }
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file