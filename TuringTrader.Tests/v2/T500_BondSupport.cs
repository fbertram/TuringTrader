//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T401_EqualWeighted
// Description: Strategy test for equal-weighted index.
// History:     2022xii18, FUB, created
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
using TuringTrader.SimulatorV2.Support;

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T500_BondSupport
    {
        [TestMethod]
        public void Test_BondSupport()
        {
            // see examples here: https://www.youtube.com/watch?v=cuzRSLc9Cy0&t=1306s

            {
                var futureValue = BondSupport.FutureValue(
                        15000.00, // $15,000 initial investment
                        0.06 / 2, // 6% per year, semi-annually
                        5 * 2);   // 5 years, semi-annual payments
                Assert.AreEqual(futureValue, 20158.745690161832, 1e-5);

                var presentValue = BondSupport.PresentValue(
                    futureValue,
                    0.06 / 2,
                    5 * 2);
                Assert.AreEqual(presentValue, 14999.999999999998, 1e-5);
            }
            {
                var futureValue = BondSupport.FutureValue(
                        15000.00,   // $15,000 initial investment
                        0.055 / 12, // 5.5% per year, monthly payments
                        5 * 12);    // 5 years, monthly payments
                Assert.AreEqual(futureValue, 19735.55658804324, 1e-5);

                var presentValue = BondSupport.PresentValue(
                    futureValue,
                    0.055 / 12,
                    5 * 12);
                Assert.AreEqual(presentValue, 15000, 1e-5);
            }
            {
                var semiannualRate = BondSupport.ConvertRatePeriodicy(
                    0.05, // current rate: 5% per year, annually compounded
                    1,    // current rate: annually compounded
                    2);   // new rate: semiannually compounded
                Assert.AreEqual(semiannualRate, 0.024695076595959931, 1e-5);

                var annualRate = BondSupport.ConvertRatePeriodicy(
                    semiannualRate, // current rate: 5% per year, semiannually compounded
                    2,              // current rate: semiannually compounded
                    1);             // new rate: annually compounded
                Assert.AreEqual(annualRate, 0.050000000000000266, 1e-5);
            }
            {
                var annualFutureValue = BondSupport.FutureValue(
                    1000.0,
                    0.05,
                    10);
                var continousRate = BondSupport.ConvertRateToContinuous(
                    0.05, 1);
                var continuousFutureValue = BondSupport.FutureValue(
                    1000.0,
                    continousRate,
                    10.0);
                Assert.AreEqual(annualFutureValue, continuousFutureValue, 1e-5);

                var annualRate = BondSupport.ConvertRateToPeriodical(
                    continousRate, 1);
                Assert.AreEqual(annualRate, 0.05, 1e-5);
            }
            {
                var annualPrice = BondSupport.BondValuation(
                    1000.00,
                    0.075,
                    0.05,
                    2);
                Assert.AreEqual(annualPrice, 1046.4852607709749, 1e-5);
            }
            {
                var faceValue = 1000.0;
                var coupon = 0.075;
                var yield = 0.05;
                var periodsPerYear = 10000;
                var years = 10;

                var continuousPriceApprox = BondSupport.BondValuation(
                    faceValue,
                    coupon / periodsPerYear,
                    yield / periodsPerYear,
                    years * periodsPerYear);

                var continuousPrice = BondSupport.BondValuation(
                    1000.00,
                    coupon,
                    yield,
                    (double)years);

                // FIXME: improve accuracy here, once BondValuation for
                //        continous compounding is no longer using discrete
                //        approximation
                Assert.AreEqual(continuousPriceApprox, continuousPrice, 1e-1);
            }
        }
    }
}

//==============================================================================
// end of file
