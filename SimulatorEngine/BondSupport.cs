//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        BondSupport
// Description: bond support functionality
// History:     2019v19, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of bond support functionality.
    /// </summary>
    public static class BondSupport
    {
        /// <summary>
        /// Calculate present value of future payment.
        /// </summary>
        /// <param name="futureValue">future cash flow</param>
        /// <param name="discountPerPeriod">discount rate per period</param>
        /// <param name="numPeriods"># of periods until payment</param>
        /// <returns>present value of future cash flow</returns>
        public static double PresentValue(double futureValue, double discountPerPeriod, double numPeriods)
            => futureValue / Math.Pow(1.0 + discountPerPeriod, numPeriods);

        /// <summary>
        /// Calculate present value of future payment.
        /// </summary>
        /// <param name="futureValue">future cash flow</param>
        /// <param name="discountPerYear">discount rate per year</param>
        /// <param name="numYears"># of years until payment</param>
        /// <returns>present value of future cash flow</returns>
        public static double PresentValueContinuouslyCompounded(double futureValue, double discountPerYear, double numYears)
            => futureValue / Math.Exp(discountPerYear * numYears);

        /// <summary>
        /// Calculate future value of present payment.
        /// </summary>
        /// <param name="presentValue">present value</param>
        /// <param name="discountPerPeriod">discount rate per period</param>
        /// <param name="numPeriods"># of periods until future date</param>
        /// <returns></returns>
        public static double FutureValue(double presentValue, double discountPerPeriod, double numPeriods)
            => presentValue * Math.Pow(1.0 + discountPerPeriod, numPeriods);

        /// <summary>
        /// Calculate future value of present payment.
        /// </summary>
        /// <param name="presentValue">present value</param>
        /// <param name="discountPerYear">discount rate per year</param>
        /// <param name="numYears"># of years until future date</param>
        /// <returns></returns>
        public static double FutureValueContinuouslyCompunded(double presentValue, double discountPerYear, double numYears)
            => presentValue * Math.Exp(discountPerYear * numYears);


#if false
        /// <summary>
        /// Calculate a bond's Macaulay Duration.
        /// see https://www.wikihow.com/Calculate-Bond-Duration
        /// see https://en.wikipedia.org/wiki/Bond_duration
        /// </summary>
        /// <param name="maturity">years to maturity</param>
        /// <param name="couponRate">annual coupon rate</param>
        /// <param name="paymentsPerYear"># of payments per year</param>
        /// <param name="parValue">par value</param>
        /// <param name="marketPrice">current market price</param>
        /// <returns></returns>
        public static double Duration(double maturity, double couponRate, double paymentsPerYear, double parValue = 1000.0, double marketPrice = 1000.0)
        {
            double couponPayment = parValue * couponRate / paymentsPerYear;
            double weightedCashFlows = 0.0;

            // sum up present values of interest payments, weighted with time to payment
            for (int i = 1; i <= maturity * paymentsPerYear; i++)
            {
                double t = i / paymentsPerYear;
                weightedCashFlows += t * PresentValue(t, couponRate, couponPayment);
            }

            // add principal payment, weighted with time to payment
            weightedCashFlows += maturity * PresentValue(maturity, couponRate, parValue);

            return weightedCashFlows / marketPrice;
        }

        /// <summary>
        /// Calculate a bond's modified duration.
        /// </summary>
        /// <param name="maturity">years to maturity</param>
        /// <param name="couponRate">annual coupon rate</param>
        /// <param name="paymentsPerYear"># of payments per year</param>
        /// <param name="parValue">par value</param>
        /// <param name="marketPrice">current market price</param>
        /// <returns></returns>
        public static double ModifiedDuration(double maturity, double couponRate, double paymentsPerYear, double parValue = 1000.0, double marketPrice = 1000.0)
        {
            double macaulayDuration = Duration(maturity, couponRate, paymentsPerYear, parValue, marketPrice);

            return macaulayDuration / (1.0 + couponRate);
        }

        /// <summary>
        /// Calculate a bond's Convexity.
        /// see https://www.wallstreetmojo.com/convexity-of-a-bond-formula-duration/
        /// </summary>
        /// <param name="maturity"></param>
        /// <param name="couponRate"></param>
        /// <param name="paymentsPerYear"></param>
        /// <param name="parValue"></param>
        /// <param name="marketPrice"></param>
        /// <returns></returns>
        public static double Convexity(double maturity, double couponRate, double paymentsPerYear, double parValue = 1000.0, double marketPrice = 1000.0)
        {
            return 0.0;
        }
#endif
        public static double Price(double faceValue, double couponPerPeriod, double yieldPerPeriod, double numPeriods)
        {
            // repayment of principal at maturity
            double presentValue = PresentValue(faceValue, yieldPerPeriod, numPeriods);

            // periodic payment of coupon
            double couponPayment = faceValue * couponPerPeriod;
            for (int t = 1; t <= numPeriods; t++)
                presentValue += PresentValue(couponPayment, yieldPerPeriod, t);

            return presentValue;
        }
    }
}

//==============================================================================
// end of file