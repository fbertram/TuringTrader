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

//#define DISABLE_STACK_ID
// with DISABLE_STACK_ID defined, no stack id will be
// calculated. Therefore, there might be a one to many
// relationship between functor objects, and indicator
// calls, leading to incorrect results

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    public class Cache<T>
    {
        #region internal data
        private static Dictionary<int, T> _cache = new Dictionary<int, T>();
        private static object _lockCache = new object();
        #endregion

        #region static public int GetStackId()
        static public int GetStackId()
        {
#if DISABLE_STACK_ID
            return 0;
#else
            string uniqueId = "";

            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            foreach (System.Diagnostics.StackFrame stackFrame in stackTrace.GetFrames())
            {
                uniqueId += stackFrame.GetMethod() + ":";
                uniqueId += stackFrame.GetNativeOffset().ToString() + "/";
            }

            // 2 identical strings will have the same hash code
            return uniqueId.GetHashCode();
#endif
        }
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