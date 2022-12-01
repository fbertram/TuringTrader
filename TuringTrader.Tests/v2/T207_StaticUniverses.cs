//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T207_StaticUniverses
// Description: Unit test for static universes.
// History:     2022xii01, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T207_StaticUniverses
    {
        private class Testbed : Algorithm
        {
            public string Universe;
            public int TestResult;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-11-01T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-01T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                var alive = new Dictionary<string, bool>();

                SimLoop(() =>
                {
                    var constituents = Universe(Universe);

                    foreach (var constituent in constituents)
                        if (Asset(constituent).Close[0] != Asset(constituent).Close[1])
                            alive[constituent] = true;
                });

                TestResult = alive.Count;
            }
        }

        [TestMethod]
        public void Test_DJI()
        {
            var algo = new Testbed();
            algo.Universe = "tiingo:$DJI";
            algo.Run();

            var numMembers = algo.TestResult;
            Assert.IsTrue(numMembers == 30);
        }
    }
}

//==============================================================================
// end of file