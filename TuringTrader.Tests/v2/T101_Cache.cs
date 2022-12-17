//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T101_Cache
// Description: Unit test for cache.
// History:     2022xi30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T101_Cache
    {
        private class Testbed : Algorithm
        {
            private int counter = 0;
            public int TestResult;
            public override void Run()
            {
                int toDo()
                {
                    Thread.Sleep(2000);
                    return ++counter;
                }

                string cacheId = "unique id";
                var cache1 = Cache(cacheId, toDo);
                var cache2 = Cache(cacheId, toDo);

                TestResult = cache2;
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var algo = new Testbed();
            algo.Run();
            var result = algo.TestResult;

            Assert.IsTrue(result == 1);
        }
    }
}

//==============================================================================
// end of file