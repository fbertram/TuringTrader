//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Support/Bonds
// Description: Bond support functionality
// History:     2019v19, FUB, created
//              2023ii17, FUB, adapted for V2 engine
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

using System;

namespace TuringTrader.SimulatorV2.Support
{
    /// <summary>
    /// Collection of bond support functionality.
    /// <see href="https://www.youtube.com/watch?v=cuzRSLc9Cy0"/>
    /// </summary>
    public static class BondSupport
    {
        /// <summary>
        /// Convert periodically compounded rate to its continuously compounded equivalent
        /// with yieldPerPeriod = yieldPerYear / numPaymentsPerYear.
        /// </summary>
        /// <param name="ratePerPeriod">periodically compounded yield per period</param>
        /// <param name="numPeriodsPerYear"># of payments per year</param>
        /// <returns>continuously compounded annual yield</returns>
        public static double ConvertRateToContinuous(double ratePerPeriod, int numPeriodsPerYear)
            => numPeriodsPerYear * Math.Log(1.0 + ratePerPeriod);

        /// <summary>
        /// Convert continuously compounded yield to its periodically compounded equivalent
        /// with yieldPerPeriod = yieldPerYear / numPaymentsPerYear.
        /// </summary>
        /// <param name="rateContinuous">continuously compounded annual yield</param>
        /// <param name="numPeriodsPerYear"># of payments per year</param>
        /// <returns>periodically compounded yield per period</returns>
        public static double ConvertRateToPeriodical(double rateContinuous, int numPeriodsPerYear)
            => Math.Exp(rateContinuous / numPeriodsPerYear) - 1.0;

        /// <summary>
        /// Converte rate from one periodicity to another.
        /// </summary>
        /// <param name="ratePerPeriod">current rate per period</param>
        /// <param name="numPeriodsPerYear">current periods per year</param>
        /// <param name="newNumPeriodsPerYear">new periods per year</param>
        /// <returns>new rate per period</returns>
        public static double ConvertRatePeriodicy(double ratePerPeriod, int numPeriodsPerYear, int newNumPeriodsPerYear)
            => Math.Pow(1.0 + ratePerPeriod, (double)numPeriodsPerYear / newNumPeriodsPerYear) - 1.0;

        /// <summary>
        /// Calculate present value of future payment, periodical compounding.
        /// </summary>
        /// <param name="futureValue">future cash flow</param>
        /// <param name="discountPerPeriod">discount rate per period</param>
        /// <param name="numPeriods"># of periods until payment</param>
        /// <returns>present value of future cash flow</returns>
        public static double PresentValue(double futureValue, double discountPerPeriod, int numPeriods)
            => futureValue / Math.Pow(1.0 + discountPerPeriod, numPeriods);

        /// <summary>
        /// Calculate present value of future payment, continuously compounded.
        /// </summary>
        /// <param name="futureValue">future cash flow</param>
        /// <param name="discountPerYear">annual discount rate</param>
        /// <param name="numYears">years until payment</param>
        /// <returns>present value of future cash flow</returns>
        public static double PresentValue(double futureValue, double discountPerYear, double numYears)
            => futureValue / Math.Exp(discountPerYear * numYears);

        /// <summary>
        /// Calculate future value of present payment, periodical compounding.
        /// </summary>
        /// <param name="presentValue">present value</param>
        /// <param name="compoundPerPeriod">compound rate per period</param>
        /// <param name="numPeriods"># of periods until future date</param>
        /// <returns></returns>
        public static double FutureValue(double presentValue, double compoundPerPeriod, int numPeriods)
            => presentValue * Math.Pow(1.0 + compoundPerPeriod, numPeriods);

        /// <summary>
        /// Calculate future value of present payment, continuously compounded.
        /// </summary>
        /// <param name="presentValue">present value</param>
        /// <param name="compoundPerYear">annual compound rate</param>
        /// <param name="numYears">years until future date</param>
        /// <returns></returns>
        public static double FutureValue(double presentValue, double compoundPerYear, double numYears)
            => presentValue * Math.Exp(compoundPerYear * numYears);

        /// <summary>
        /// Calculate bond price, periodical payments.
        /// </summary>
        /// <param name="faceValue">bond's face (par) value</param>
        /// <param name="couponPerPeriod">coupon payment per period</param>
        /// <param name="yieldPerPeriod">yield per period</param>
        /// <param name="numPeriods"># of periods until maturity</param>
        /// <returns>bond price</returns>
        public static double BondValuation(double faceValue, double couponPerPeriod, double yieldPerPeriod, int numPeriods)
        {
            // repayment of principal at maturity
            double presentValue = PresentValue(faceValue, yieldPerPeriod, numPeriods);

            // periodic payment of coupon
            double couponPayment = faceValue * couponPerPeriod;
            for (int p = 1; p <= numPeriods; p++)
                presentValue += PresentValue(couponPayment, yieldPerPeriod, p);

            return presentValue;
        }

        /// <summary>
        /// Calculate bond price, continuously compounded.
        /// </summary>
        /// <param name="faceValue">bond's face (par) value</param>
        /// <param name="coupon">annual coupon</param>
        /// <param name="yield">annual yield</param>
        /// <param name="numYears">years until maturity</param>
        /// <returns>bond price</returns>
        public static double BondValuation(double faceValue, double coupon, double yield, double numYears)
        {
#if true
            // FIXME: this the brute-force method,
            //        approximating continuous compounding through
            //        a sufficiently large number of periodical payments
            var periodsPerYear = 365;

            return BondValuation(
                faceValue,
                coupon / periodsPerYear,
                yield / periodsPerYear,
                (int)Math.Round(numYears * periodsPerYear));
#else
            // repayment of principal at maturity
            double presentValue = PresentValue(faceValue, yield, numYears);

            // periodic payment of coupon
            // TODO: this needs to be adapted to continuous compounding
            var daysPerYear = 365;
            double couponPayment = faceValue * coupon / daysPerYear;
            var numDays = (int)Math.Floor(numYears * daysPerYear);
            for (int d = 1; d <= numDays; d++)
                presentValue += PresentValue(couponPayment, yield, (double)d / daysPerYear);

            return presentValue;
#endif
        }
    }
}

//==============================================================================
// end of file