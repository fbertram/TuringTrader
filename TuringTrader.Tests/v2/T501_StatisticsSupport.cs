//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T501_StatisticsSupport
// Description: Test statistics support package.
// History:     2023iv20, FUB, created
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.SimulatorV2.Support;

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T501_StatisticsSupport
    {
        [TestMethod]
        public void Test_TTest()
        {
            {
                // single-value two-tailed t-test
                // see https://www.youtube.com/watch?v=xuItoKmd7iQ
                var newbornBirthWeights = new List<double>
                {
                    2.6,
                    2.7,
                    3.2,
                    2.9,
                    3.0,
                    3.3,
                    5.2,
                    2.3,
                    4.1,
                    3.7,
                };

                var tValue = StatisticsSupport.StudentT.TValue(newbornBirthWeights, 3.5);
                Assert.AreEqual(-0.7430661069271054, tValue, 1e-5);

                var tCritical = StatisticsSupport.StudentT.CriticalValue(newbornBirthWeights, 0.05);
                Assert.AreEqual(2.262, tCritical, 1e-5);

                var rejectionRegionLower = newbornBirthWeights.Average()
                    - tCritical * newbornBirthWeights.StandardDeviation() / Math.Sqrt(newbornBirthWeights.Count);
                var rejectionRegionUpper = newbornBirthWeights.Average()
                    + tCritical * newbornBirthWeights.StandardDeviation() / Math.Sqrt(newbornBirthWeights.Count);
                Assert.AreEqual(2.6911712621762995, rejectionRegionLower, 1e-5);
                Assert.AreEqual(3.9088287378237, rejectionRegionUpper, 1e-5);
                Assert.AreEqual(true, 3.5 > rejectionRegionLower && 3.5 < rejectionRegionUpper);

                var tTest = StatisticsSupport.StudentT.TestH0(newbornBirthWeights, 3.5, 0.05);
                Assert.AreEqual(true, tTest);
            }
            {
                // single-value two-tailed t-test
                // see https://www.youtube.com/watch?v=vEG_MOnyMdE
                var systolicBP = new List<double>
                {
                    128,
                    118,
                    144,
                    133,
                    132,
                    111,
                    149,
                    139,
                    136,
                    126,
                    127,
                    115,
                    142,
                    140,
                    131, // 151?
                    132,
                    122,
                    119,
                    129,
                    128,
                };

                var tValue = StatisticsSupport.StudentT.TValue(systolicBP, 120);
                Assert.AreEqual(4.5124036593367167, tValue, 1e-5);

                var tCritical = StatisticsSupport.StudentT.CriticalValue(systolicBP, 0.05);
                Assert.AreEqual(2.093, tCritical, 1e-5);


                var tTest = StatisticsSupport.StudentT.TestH0(systolicBP, 120, 0.05);
                Assert.AreEqual(false, tTest);
            }
            {
                // single-value two-tailed t-test
                // see https://www.youtube.com/watch?v=VPd8DOL13Iw

                var tCritical = StatisticsSupport.StudentT.CriticalValue(30, 1, 0.05);
                Assert.AreEqual(2.045, tCritical, 1e-5);

                var tValue = StatisticsSupport.StudentT.TValue(
                    140, 20, 30,
                    100);
                Assert.AreEqual(10.954451150103322, tValue, 1e-5);

                var tTest = StatisticsSupport.StudentT.TestH0(
                    140, 20, 30,
                    100,
                    0.05);
                Assert.AreEqual(false, tTest);
            }
            {
                // double-sided independent t-test
                // see https://www.youtube.com/watch?v=pTmLQvMM-1M

                var field1 = new List<double>
                {
                    15.2,
                    15.3,
                    16.0,
                    15.8,
                    15.6,
                    14.9,
                    15.0,
                    15.4,
                    15.6,
                    15.7,
                    15.5,
                    15.2,
                    15.5,
                    15.1,
                    15.3,
                    15.0,
                };
                var field2 = new List<double>
                {
                    15.9,
                    15.9,
                    15.2,
                    16.6,
                    15.2,
                    15.8,
                    15.8,
                    16.2,
                    15.6,
                    15.6,
                    15.8,
                    15.5,
                    15.5,
                    15.5,
                    14.9,
                    15.9,
                };

                var tCritical = StatisticsSupport.StudentT.CriticalValue(field1, field2, 0.05);
                Assert.AreEqual(2.042, tCritical, 1e-5);

                var tValue = StatisticsSupport.StudentT.TValue(field1, field2);
                Assert.AreEqual(-2.3388213848187491, tValue, 1e-5);

                var tTest = StatisticsSupport.StudentT.TestH0(field1, field2, 0.05);
                Assert.AreEqual(false, tTest);
            }
        }
    }
}

//==============================================================================
// end of file
