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

        private CacheId()
        {

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

        #region static public CacheId NewFromStackTraceParameters(params int[] parameterIds)
        /// <summary>
        /// Create cryptographic key to uniquely identify auto-magically 
        /// created indicator functors.
        /// This overload considers the stack trace, as well as parameter IDs.
        /// </summary>
        /// <param name="parameterIds">list of integer parameter ids</param>
        /// <returns>cache id</returns>
        static public CacheId NewFromStackTraceParameters(params int[] parameterIds)
        {
            var key = StackTraceId();

            foreach (var i in parameterIds)
                key = AddId(key, i);

            return new CacheId
            {
                Key = key,
            };
        }
        #endregion
        #region static public CacheId NewFromParameters(params int[] parameterIds)
        /// <summary>
        /// Create cryptographic key to uniquely identify auto-magically 
        /// created indicator functors.
        /// This overload only considers the parameter IDs, and does not include
        /// the stack trace.
        /// </summary>
        /// <param name="id">existing cache id</param>
        /// <param name="parameterIds">list of integer parameter ids</param>
        /// <returns>cache id</returns>
        static public CacheId NewFromParameters(params int[] parameterIds)
        {
            var key = SEED;

            foreach (var i in parameterIds)
                key = AddId(key, i);

            return new CacheId
            {
                Key = key,
            };
        }
        #endregion
        #region static public CacheId NewFromIdParameters(CacheId id, params int[] parameterIds)
        /// <summary>
        /// Create cryptographic key to uniquely identify auto-magically 
        /// created indicator functors.
        /// This overload start with an existing cache id, and 
        /// adds a number of parameter IDs.
        /// </summary>
        /// <param name="id">existing cache id</param>
        /// <param name="parameterIds">list of integer parameter ids</param>
        /// <returns>cache id</returns>
        static public CacheId NewFromIdParameters(CacheId id, params int[] parameterIds)
        {
            var key = id.Key;

            foreach (var i in parameterIds)
                key = AddId(key, i);

            return new CacheId
            {
                Key = key,
            };
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