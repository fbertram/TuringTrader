//==============================================================================
// Project:     Trading Simulator
// Name:        Cache
// Description: data cache, to reduce memory footprint and cpu use
// History:     2018ix21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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

namespace FUB_TradingSim
{
    public static class Cache
    {
        #region static public int UniqueId(params int[] parameterIds)
        static public int UniqueId(params int[] parameterIds)
        {
            // on top of the parameter ids, we also need to uniquely identify the call stack
            // currently, we use only the native offset for this
            // do we need to use the method name as well?
            IEnumerable<int> stackFrames = new System.Diagnostics.StackTrace().GetFrames()
                .Select(f => f.GetNativeOffset().GetHashCode());

            var subIds = parameterIds.AsEnumerable()
                .Concat(stackFrames);

            // see https://stackoverflow.com/questions/7278136/create-hash-value-on-a-list
            const int seed = 487;
            const int modifier = 31;
            unchecked
            {
                return subIds
                    .Aggregate(seed, (current, item) => (current * modifier) + item);
            }
        }
        #endregion
    }

    public static class Cache<T>
    {
        #region internal data
        private static Dictionary<int, T> _cache = new Dictionary<int, T>();
        private static object _lockCache = new object();
        #endregion
        #region static public T GetData(int key, Func<T> initialRetrieval)
        static public T GetData(int key, Func<T> initialRetrieval)
        {
#if DISABLE_CACHE
            return initialRetrieval();
#else
            lock(_lockCache)
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