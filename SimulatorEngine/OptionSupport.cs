//==============================================================================
// Project:     Trading Simulator
// Name:        OptionSupport
// Description: option support functionality
// History:     2018ix21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    public static class OptionSupport
    {
        #region internal helpers
        /// <summary>
        /// Probability Density Function
        /// </summary>
        private static double PDF(double zScore)
        {
            // see https://en.wikipedia.org/wiki/Normal_distribution
            double x = Math.Abs(zScore);
            return 1.0 / Math.Sqrt(2 * Math.PI) * Math.Exp(-x * x / 2.0);
        }

        /// <summary>
        /// Cumulative Distribution Function
        /// </summary>
        public static double CDF(double zScore)
        {
            double x = Math.Abs(zScore);

            // see https://en.wikipedia.org/wiki/Normal_distribution#Numerical_approximations_for_the_normal_CDF
            // => Zelen & Severo (1964),
            // => algorithm 26.2.17 http://people.math.sfu.ca/~cbm/aands/page_932.htm
            const double p = 0.2316419;
            double t = 1.0 / (1.0 + p * x);
            const double b1 = 0.31938153;
            const double b2 = -0.356563782;
            const double b3 = 1.781477937;
            const double b4 = -1.821255978;
            const double b5 = 1.330274429;
            double Px = 1.0 - PDF(x) * (((((b5 * t) + b4) * t + b3) * t + b2) * t + b1) * t;

            return zScore < 0.0
                ? 1.0 - Px
                : Px;
        }
        #endregion

        #region public static double BlackScholesPrice(this Instrument contract, double volatility, double riskFreeRate)
        public static double BlackScholesPrice(this Instrument contract, double volatility, double riskFreeRate)
        {
            if (!contract.IsOption)
                return 0.00;

            // see https://en.wikipedia.org/wiki/Black–Scholes_model

            DateTime today = contract.Simulator.SimTime[0];
            double T = (contract.OptionExpiry - today).Duration().Days / 365.25;
            double S = contract.Simulator.Instruments[contract.OptionUnderlying].Close[0];
            double K = contract.OptionStrike;
            double r = riskFreeRate;
            double v = volatility;

            double d1 = (Math.Log(S / K) + (r + v * v / 2.0) * T) / (v * Math.Sqrt(T));
            double d2 = d1 - v * Math.Sqrt(T);

            return contract.OptionIsPut
                    ? CDF(-d2) * K * Math.Exp(-r * T) - CDF(-d1) * S // put
                    : CDF(d1) * S - CDF(d2) * K * Math.Exp(-r * T);  // call
        }
        #endregion
    }
}

//==============================================================================
// end of file
              