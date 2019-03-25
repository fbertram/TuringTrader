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
        #region internal data
        private const int MODIFIER = 31;
        private const int SEED = 487;
        #endregion
        #region internal helpers
        private static int AddId(int current, int item)
        {
            // this is unchecked, as an arithmetic
            // overflow will occur here
            unchecked
            {
                return current * MODIFIER + item;
            }
        }

        private static int StackTraceId()
        {
            var stackTrace = new System.Diagnostics.StackTrace(2, false); // skip 2 frames
            var numFrames = stackTrace.FrameCount;

            int id = SEED;
            for (int i = 0; i < numFrames; i++)
                id = AddId(id, stackTrace.GetFrame(i).GetNativeOffset());

            return id;
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
            Key = StackTraceId();

            foreach (var p in parameterIds)
                Key = AddId(Key, p);
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
            Key = StackTraceId();

            foreach (var p in parameterIds)
                Key = AddId(Key, p);
        }
        #endregion
        #region public CacheId(CacheId existing, int parameterId)
        /// <summary>
        /// Create unique cryptographic key from a existing key, and 
        /// a single additional parameter id. This ID is used to uniquely identify 
        /// auto-magically created indicator functors.
        /// </summary>
        /// <param name="existing">existing cache id</param>
        /// <param name="parameterId">additional paramter id</param>
        public CacheId(CacheId existing, int parameterId)
        {
            Key = AddId(existing.Key, parameterId);
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