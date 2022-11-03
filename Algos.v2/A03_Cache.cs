//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A03_Cache
// Description: Develop & test cache.
// History:     2021vi05, FUB, created
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

#region libraries
using System;
using System.Threading;
using TuringTrader.SimulatorV2;
#endregion

// NOTE: The cache is a central feature of TuringTrader. It is used to
// store asset quotes and indicators. Objects in the cache are referred
// to by an id. On a cache miss, a method is called to retrieve the result.

namespace TuringTrader.DemoV2
{
    public class A03_Cache : Algorithm
    {
        public override string Name => "A03_Cache";

        public override void Run()
        {
            string toDo()
            {
                Thread.Sleep(2000);
                return string.Format("completed my todos at {0}", DateTime.Now);
            }

            string cacheId = "unique id";

            var cache1 = Cache(cacheId, toDo);
            Output.WriteLine(cache1.Result);

            var cache2 = Cache(cacheId, toDo);
            Output.WriteLine(cache2.Result);
        }

        public override void Report() => Output.WriteLine("Here is your report");
    }
}

//==============================================================================
// end of file
