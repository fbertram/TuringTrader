//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        BondSupport
// Description: unit test for bond support package
// History:     2019v19, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class BondSupport
    {
#if false
        [TestMethod]
        public void Test_Duration()
        {
            {
                // see https://www.wikihow.com/Calculate-Bond-Duration
                double duration = TuringTrader.Simulator.BondSupport.Duration(
                3.0,
                0.03,
                1,
                1000,
                1000);

                Assert.IsTrue(Math.Abs(duration - 2.914) < 1e-2);
            }
            {
                // see https://www.wallstreetmojo.com/convexity-of-a-bond-formula-duration/
                // or a Bond of Face Value USD 1,000 with a semi-annual coupon of 8.0% 
                // and a Yield of 10% and 6 years to maturity  and a present price 
                // of 911.37, the duration is 4.82 years, the modified duration is 4.59
                double duration = TuringTrader.Simulator.BondSupport.Duration(
                    6.0, // 6 years to maturity
                    0.08, 2, // semi-annual coupon of 8%
                    1000,
                    911.37);

                Assert.IsTrue(Math.Abs(duration - 2.914) < 1e-2);

                double modifiedDuration = TuringTrader.Simulator.BondSupport.ModifiedDuration(
                    6.0, // 6 years to maturity
                    0.08, 2, // semi-annual coupon of 8%
                    1000,
                    911.37);

                Assert.IsTrue(Math.Abs(duration - 2.914) < 1e-2);
            }
        }
#endif
        [TestMethod]
        public void Test_PresentValue_FutureValue()
        {
            {
                // annual compunding
                double pv = TuringTrader.Support.BondSupport.PresentValue(179.08, 0.06, 10);
                Assert.IsTrue(Math.Abs(pv - 100.00) < 1e-2);

                double fv = TuringTrader.Support.BondSupport.FutureValue(100, 0.06, 10);
                Assert.IsTrue(Math.Abs(fv - 179.08) < 1e-2);
            }
            {
                // annual compounding
                double pv = TuringTrader.Support.BondSupport.PresentValue(100.00, 0.06, 10);
                Assert.IsTrue(Math.Abs(pv - 55.8395) < 1e-2);

                double fv = TuringTrader.Support.BondSupport.FutureValue(55.8395, 0.06, 10);
                Assert.IsTrue(Math.Abs(fv - 100.00) < 1e-2);
            }
            {
                // semiannual compounding
                double pv = TuringTrader.Support.BondSupport.PresentValue(100.00, 0.0591 / 2.0, 20);
                Assert.IsTrue(Math.Abs(pv - 55.8536) < 1e-2);

                double fv = TuringTrader.Support.BondSupport.FutureValue(55.8395, 0.0591 / 2.0, 20);
                Assert.IsTrue(Math.Abs(fv - 99.9748) < 1e-2);
            }
            {
                // continuous compounding
                double pv = TuringTrader.Support.BondSupport.PresentValueContinuouslyCompounded(100.00, 0.0583, 10);
                Assert.IsTrue(Math.Abs(pv - 55.8221) < 1e-2);

                double fv = TuringTrader.Support.BondSupport.FutureValueContinuouslyCompunded(55.8395, 0.0583, 10);
                Assert.IsTrue(Math.Abs(fv - 100.0311) < 1e-2);
            }
        }

        [TestMethod]
        public void Test_Price()
        {
            {
                // annual compounding
                double price6 = TuringTrader.Support.BondSupport.Price(100.00, 0.06, 0.06, 10);
                Assert.IsTrue(Math.Abs(price6 - 100.00) < 1e-2);

                double price5 = TuringTrader.Support.BondSupport.Price(100.00, 0.06, 0.05, 10);
                Assert.IsTrue(Math.Abs(price5 - 107.7217) < 1e-2);
            }
        }
    }
}

//==============================================================================
// end of file