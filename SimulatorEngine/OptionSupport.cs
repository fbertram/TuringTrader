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

// TODO: review this as a possible improvement
// Peter Jäckel's LetsBeRational is an extremely fast and accurate method for 
// obtaining Black's implied volatility from option prices with as little as 
// two iterations to maximum attainable precision on standard(64 bit floating point) 
// hardware for all possible inputs.
// https://pypi.org/project/lets_be_rational/#files

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of option support functionality.
    /// </summary>
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

#if false
        // this is the nicer code, but does not take dividend yield into account,
        // and doesn't calculate greeks
        // TODO: use this as basis for cleanup of methods further below
        #region public static double BlackScholesPrice(this Instrument contract, double volatility, double riskFreeRate)
        /// <summary>
        /// Calculate arbitrage-free price of European-style option,
        /// according to Black-Scholes equation as described here:
        /// <see href="https://en.wikipedia.org/wiki/Black–Scholes_model"/>
        /// </summary>
        /// <param name="contract">input option contract</param>
        /// <param name="volatility">annualized volatility of underlying asset</param>
        /// <param name="riskFreeRate">annualized risk free rate</param>
        /// <returns>option price</returns>
        public static double _old_BlackScholesPrice(this Instrument contract, double volatility, double riskFreeRate)
        {
            if (!contract.IsOption)
                return 0.00;

            // see https://en.wikipedia.org/wiki/Black–Scholes_model

            DateTime today = contract.Simulator.SimTime[0];
            Instrument underlying = contract.Simulator.Instruments.Where(i => i.Symbol == contract.OptionUnderlying).First();
            double T = (contract.OptionExpiry - today).TotalDays / 365.25;
            double S = underlying.Close[0];
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
#endif

        #region public class OptionPriceAndGreeks
        /// <summary>
        /// Container to hold option price and greeks.
        /// </summary>
        public class OptionPriceVolGreeks
        {
            /// <summary>
            /// option price
            /// </summary>
            public double Price;
            /// <summary>
            /// option volatility
            /// </summary>
            public double Volatility;
            /// <summary>
            /// option delta
            /// </summary>
            public double Delta;
            /// <summary>
            /// option gamma
            /// </summary>
            public double Gamma;
            /// <summary>
            /// option vega
            /// </summary>
            public double Vega;
            /// <summary>
            /// option rho
            /// </summary>
            public double Rho;
            /// <summary>
            /// option theta
            /// </summary>
            public double Theta;
        }
        #endregion
        #region public static OptionPriceAndGreeks BlackScholes(this Instrument contract, double volatility, double riskFreeRate)
        /// <summary>
        /// Calculate Black-Scholes arbitrage-free price for European-style options,
        /// plus the common option greeks.
        /// <see href="https://en.wikipedia.org/wiki/Black–Scholes_model"/>
        /// </summary>
        /// <param name="contract">option contract to calculate</param>
        /// <param name="volatility">annualized volatility of underlying asset</param>
        /// <param name="riskFreeRate">annualized risk-free rate of return</param>
        /// <returns>container w/ price and greeks</returns>
        public static OptionPriceVolGreeks BlackScholes(this Instrument contract, double volatility, double riskFreeRate, double dividendYield = 0.0)
        {
            if (!contract.IsOption)
                throw new Exception("BlackScholes: input not an option contract");

            DateTime today = contract.Simulator.SimTime[0];
            Instrument underlying = contract.Simulator.Instruments.Where(i => i.Symbol == contract.OptionUnderlying).First();
            double underlyingPrice = underlying.Close[0];
            double strike = contract.OptionStrike;
            double yearsToExpiry = (contract.OptionExpiry - today).TotalDays / 365.25;

            // see https://github.com/pelife/QuantRiskLib/blob/master/QuantRiskLib/QuantRiskLib/Options.cs

            //setting dividendYield = 0 gives the classic Black Scholes model
            //setting dividendYield = foreign risk-free rate gives a model for European currency options, see Garman and Kohlhagen (1983)

            double sqrtT = Math.Sqrt(yearsToExpiry);
            double d1 = (Math.Log(underlyingPrice / strike) + (riskFreeRate - dividendYield + 0.5 * volatility * volatility) * yearsToExpiry) / (volatility * sqrtT);
            double d2 = d1 - volatility * sqrtT;
            double N1 = CDF(d1);
            double N2 = CDF(d2);
            double n1 = PDF(d1);
            double n2 = PDF(d2);

            double eNegRiskFreeRateTimesYearsToExpiry = Math.Exp(-riskFreeRate * yearsToExpiry);
            double eNegDivYieldYearsToExpiry = Math.Exp(-dividendYield * yearsToExpiry);

            double price = contract.OptionIsPut
                ? (1 - N2) * strike * eNegRiskFreeRateTimesYearsToExpiry - (1 - N1) * underlyingPrice * eNegDivYieldYearsToExpiry // put
                : N1 * underlyingPrice *eNegDivYieldYearsToExpiry - N2 * strike * eNegRiskFreeRateTimesYearsToExpiry; // call                

            double delta = contract.OptionIsPut
                ? eNegDivYieldYearsToExpiry * (N1 - 1.0) // put
                : eNegDivYieldYearsToExpiry * N1; // call

            double gamma = eNegDivYieldYearsToExpiry * n1 / (underlyingPrice * volatility * sqrtT);
            double vega = underlyingPrice * Math.Exp(-dividendYield * yearsToExpiry) * n1 * sqrtT;

            double rho = contract.OptionIsPut
                ? -yearsToExpiry * strike * eNegRiskFreeRateTimesYearsToExpiry * (1 - N2) // put
                : yearsToExpiry* strike *eNegRiskFreeRateTimesYearsToExpiry * N2; // call

            double a = -underlyingPrice * eNegDivYieldYearsToExpiry * n1 * volatility / (2.0 * sqrtT);
            double b = dividendYield * underlyingPrice * eNegDivYieldYearsToExpiry;
            double theta = contract.OptionIsPut
                ? a - b * (1 - N1) + riskFreeRate * strike * eNegRiskFreeRateTimesYearsToExpiry * (1 - N2) // put
                : a + b * N1 - riskFreeRate * strike * eNegRiskFreeRateTimesYearsToExpiry * N2; // call

            double convexity = contract.OptionIsPut
                ? -rho * ((n2 * sqrtT) / ((1 - N2) * volatility) + yearsToExpiry) // put
                : rho * ((n2 * sqrtT) / (N2 * volatility) - yearsToExpiry); // call

            return new OptionPriceVolGreeks
            {
                Price = price,
                Volatility = volatility,
                Delta = delta,
                Gamma = gamma,
                Vega = vega,
                Rho = rho,
                Theta = theta
            };
        }
        #endregion
        #region public static OptionPriceVolGreeks BlackScholesImplied(this Instrument contract, double riskFreeRate)
        /// <summary>
        /// Calculate implied volatility from Black-Scholes arbitrage-free price for European-style options,
        /// plus the common option greeks.
        /// <see href="https://en.wikipedia.org/wiki/Black–Scholes_model"/>
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="riskFreeRate"></param>
        /// <param name="dividendYield"></param>
        /// <returns></returns>
        public static OptionPriceVolGreeks BlackScholesImplied(this Instrument contract, double riskFreeRate, double dividendYield = 0.0)
        {
            DateTime today = contract.Simulator.SimTime[0];
            Instrument underlying = contract.Simulator.Instruments.Where(i => i.Symbol == contract.OptionUnderlying).First();

            double underlyingPrice = underlying.Close[0];
            double strike = contract.OptionStrike;
            double yearsToExpiry = (contract.OptionExpiry - today).Duration().Days / 365.25;
            double midPrice = (contract.Bid[0] + contract.Ask[0]) / 2.0;

            // see https://github.com/pelife/QuantRiskLib/blob/master/QuantRiskLib/QuantRiskLib/Options.cs

            const double tolerance = 0.001;
            const int maxLoops = 16;

            double vol = Math.Sqrt(2 * Math.Abs(Math.Log(underlyingPrice / strike) / yearsToExpiry + riskFreeRate));    //Manaster and Koehler intial vol value
            vol = Math.Max(0.01, vol);
            var optionGreeks = contract.BlackScholes(vol, riskFreeRate, dividendYield);
            double impliedPrice = optionGreeks.Price;
            double vega = optionGreeks.Vega;

            // TODO: this loop is really ugly. Need to refactor after validation.

            // TODO: check if things are converging properly. It is recommended to calculate only
            // out-of-the-money options with this method. In case of in-the-money options, put-call
            // parity should be used to transform to the other case. see here:
            // https://quant.stackexchange.com/questions/15198/what-is-an-efficient-method-to-find-implied-volatility

            int nLoops = 0;
            while (Math.Abs(impliedPrice - midPrice) > tolerance)
            {
                nLoops++;
                if (nLoops > maxLoops)
                    throw new Exception("BlackScholesImpliedVolatility: implied volatility did not converge.");

                vol = vol - (impliedPrice - midPrice) / vega;
                if (vol <= 0)
                    vol = 0.5 * (vol + (impliedPrice - midPrice) / vega); //half way btwn previous estimate and zero

                optionGreeks = contract.BlackScholes(vol, riskFreeRate, dividendYield);
                impliedPrice = optionGreeks.Price;
                vega = optionGreeks.Vega;
            }
            return optionGreeks;
        }
        #endregion
    }
}

//==============================================================================
// end of file
              