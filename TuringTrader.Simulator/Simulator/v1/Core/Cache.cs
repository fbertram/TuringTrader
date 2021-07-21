//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Cache
// Description: data cache, to reduce memory footprint and cpu use
// History:     2018ix21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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

// NOTE:
// This cache implementation will leak memory, as global objects are never
// freed. Typical global objects include data from the various data feeds.
// By default, objects are stored in thread-local memory, which should be
// freed when the thread terminates. This includes objects created for
// time series and indicators.

#define THREAD_LOCAL
// with THREAD_LOCAL defined, data will be cached 
// in thread-local storage, except when the 'global'
// flag is set

//#define DISABLE_CACHE
// with DISABLE_CACHE defined, no data will be cached, 
// and retrieval function will be called directly
// please note that indicators require caching to
// be enabled

#region libraries
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        private int keyCallStack;
        private int keyParameters;
        #endregion
        #region internal helpers
        private static int CombineId(int current, int item)
        {
            // this is unchecked, as an arithmetic
            // overflow will occur here
            unchecked
            {
                return current * MODIFIER + item;
            }
        }

        private void ApplyCallStack(CacheId parentId, string memberName, int lineNumber)
        {
            keyCallStack = parentId != null ? parentId.keyCallStack : SEED;
            keyCallStack = CombineId(keyCallStack, memberName.GetHashCode());
            keyCallStack = CombineId(keyCallStack, lineNumber);

            // this is safer, as we don't need to keep track of the stack trace
            // however, this is also really slow, especially when running
            // indicator-rich algorithms in the optimizer
            // keyCallStack = StackTraceId();
        }

        private void ApplyParameters(CacheId parentId, params int[] parameterIds)
        {
            keyParameters = parentId != null ? parentId.keyParameters : SEED;

            foreach (var id in parameterIds)
                keyParameters = CombineId(keyParameters, id);
        }

        /*private static int StackTraceId()
        {
            var stackTrace = new System.Diagnostics.StackTrace(2, false); // skip 2 frames
            var numFrames = stackTrace.FrameCount;

            int id = SEED;
            for (int i = 0; i < numFrames; i++)
                id = CombineId(id, stackTrace.GetFrame(i).GetNativeOffset());

            return id;
        }*/
        #endregion

        #region public int Key
        /// <summary>
        /// Cryptographic key
        /// </summary>
        public int Key
        {
            get
            {
                return CombineId(keyCallStack, keyParameters);
            }
        }
        #endregion

        #region ctor
        /// <summary>
        /// Create new cache id. Typically, this constructor is used without parameters,
        /// resulting in a new top-level cache id, specific to the current member function
        /// and line number. 
        /// </summary>
        /// <param name="parentId">parent cache id. Typically not required. Pass in value when nesting indicators.</param>
        /// <param name="memberName">member function name. Typically filled in by compiler services. Pass in value when creating indicators.</param>
        /// <param name="lineNumber">line number. Typically filled in by compiler services. Pass in value when creating indicators.</param>
        public CacheId(CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            ApplyCallStack(parentId, memberName, lineNumber);
            ApplyParameters(parentId);
        }

#if true
        /// <summary>
        /// Create new cache id. This constructor is specifically aimed at building indicators. Therefore,
        /// all parameters are mandatory. The cache id created by this constructor is idential to one
        /// created using new CacheId(...).AddParameters(...)
        /// </summary>
        /// <param name="parentId">parent cache id</param>
        /// <param name="memberName">member function name, or ""</param>
        /// <param name="lineNumber">line number or 0</param>
        /// <param name="parameterIds">list of parameter ids</param>
        public CacheId(CacheId parentId, string memberName, int lineNumber, params int[] parameterIds)
        {
            ApplyCallStack(parentId, memberName, lineNumber);
            ApplyParameters(parentId, parameterIds);
        }
#endif

        /// <summary>
        /// Clone parent cache id. This constructor is only used for internal purposes.
        /// </summary>
        /// <param name="parentId">parent to clone</param>
        private CacheId(CacheId parentId)
        {
            keyCallStack = parentId.keyCallStack;
            keyParameters = parentId.keyParameters;
        }
        #endregion

        #region public CacheId AddParameters(params int[] parameterIds)
        /// <summary>
        /// Create new cache id, adding a number of parameters to the previous
        /// cache id.
        /// </summary>
        /// <param name="parameterIds"></param>
        /// <returns>new cache id</returns>
        public CacheId AddParameters(params int[] parameterIds)
        {
            var id = new CacheId(this);
            id.ApplyParameters(id, parameterIds);
            return id;
        }
        #endregion
    }
    #endregion

    /// <summary>
    /// Cache template class. The cache is at the core of TuringTrader's
    /// auto-magic indicator objects, as well as the the data sources. Cache
    /// objects are accessed via a cryptographic key, which is created by
    /// the CacheId class.
    /// </summary>
    /// <typeparam name="T">type of cache</typeparam>
    public static class Cache<T>
    {

        #region internal data
        private static Dictionary<int, T> _globalCache = new Dictionary<int, T>();

        [ThreadStatic]
        private static Dictionary<int, T> _threadCache = null;
        #endregion
        #region static public T GetData(CacheId key, Func<T> initialRetrieval)
        /// <summary>
        /// Retrieve data from cache.
        /// </summary>
        /// <param name="id">unique ID of data</param>
        /// <param name="initialRetrieval">lambda to retrieve data not found in cache</param>
        /// <param name="global">set to true, to cache globally across all threads</param>
        /// <returns>cached data</returns>
        static public T GetData(CacheId id, Func<T> initialRetrieval, bool global = false)
        {
#if DISABLE_CACHE
            return initialRetrieval();
#else
            int key = id.Key;

#if THREAD_LOCAL
            // separate thread-local and global caches
            if (global)
            {
                lock (_globalCache)
                {
                    if (!_globalCache.ContainsKey(key))
                        _globalCache[key] = initialRetrieval();

                    return _globalCache[key];
                }
            }
            else
            {
                if (_threadCache == null)
                    _threadCache = new Dictionary<int, T>();

                if (!_threadCache.ContainsKey(key))
                    _threadCache[key] = initialRetrieval();

                return _threadCache[key];
            }
#else
            // unified cache across all threads
            lock (_globalCache)
            {
                if (!_globalCache.ContainsKey(key))
                    _globalCache[key] = initialRetrieval();

                return _globalCache[key];
            }
#endif
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file