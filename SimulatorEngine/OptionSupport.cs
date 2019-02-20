//==============================================================================
// Project:     Trading Simulator
// Name:        OptionSupport
// Description: option support functionality
// History:     2019i29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
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
        #region from Espen Gaarder Haug: The Complete Guide to Option Pricing Formulas, Second Edition

        #region private static double ND(double x)
        /// <summary>
        /// Normal distribution function.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double ND(double x)
        {
            /*
                '// The normal distribution function
                Public Function ND(x As Double) As Double
                    ND = 1 / Sqr(2 * Pi) * Exp(-x ^ 2 / 2)
                End Function
            */
            return 1 / Math.Sqrt(2 * Math.PI) * Math.Exp(-x * x / 2);
        }
        #endregion
        #region public static double CND(double x)
        /// <summary>
        /// Cumulative normal distribution, see chapter 13.1.1
        /// </summary>
        /// <param name="x">number of standard-deviations away from average</param>
        /// <returns>probability, that result is greater or equal to z-score</returns>
        public static double CND(double x)
        {
            /*
                '// Cummulative double precision algorithm based on Hart 1968
                '// Based on implementation by Graeme West
                Function CND(x As Double) As Double
                    Dim y As Double, Exponential As Double, SumA As Double, SumB As Double

                    y = Abs(x)
                    If y > 37 Then
                        CND = 0
                    Else
                        Exponential = Exp(-y ^ 2 / 2)
                        If y < 7.07106781186547 Then
                            SumA = 3.52624965998911E-02 * y + 0.700383064443688
                            SumA = SumA * y + 6.37396220353165
                            SumA = SumA * y + 33.912866078383
                            SumA = SumA * y + 112.079291497871
                            SumA = SumA * y + 221.213596169931
                            SumA = SumA * y + 220.206867912376
                            SumB = 8.83883476483184E-02 * y + 1.75566716318264
                            SumB = SumB * y + 16.064177579207
                            SumB = SumB * y + 86.7807322029461
                            SumB = SumB * y + 296.564248779674
                            SumB = SumB * y + 637.333633378831
                            SumB = SumB * y + 793.826512519948
                            SumB = SumB * y + 440.413735824752
                            CND = Exponential * SumA / SumB
                        Else
                            SumA = y + 0.65
                            SumA = y + 4 / SumA
                            SumA = y + 3 / SumA
                            SumA = y + 2 / SumA
                            SumA = y + 1 / SumA
                            CND = Exponential / (SumA * 2.506628274631)
                        End If
                  End If

                  If x > 0 Then CND = 1 - CND

                End Function
            */

            double y = Math.Abs(x);
            double CND;

            if (y > 37)
            {
                CND = 0;
            }
            else
            {
                double Exponential = Math.Exp(-y * y / 2);
                if (y < 7.07106781186547)
                {
                    double SumA = 3.52624965998911E-02 * y + 0.700383064443688;
                    SumA = SumA * y + 6.37396220353165;
                    SumA = SumA * y + 33.912866078383;
                    SumA = SumA * y + 112.079291497871;
                    SumA = SumA * y + 221.213596169931;
                    SumA = SumA * y + 220.206867912376;
                    double SumB = 8.83883476483184E-02 * y + 1.75566716318264;
                    SumB = SumB * y + 16.064177579207;
                    SumB = SumB * y + 86.7807322029461;
                    SumB = SumB * y + 296.564248779674;
                    SumB = SumB * y + 637.333633378831;
                    SumB = SumB * y + 793.826512519948;
                    SumB = SumB * y + 440.413735824752;
                    CND = Exponential * SumA / SumB;
                }
                else
                {
                    double SumA = y + 0.65;
                    SumA = y + 4 / SumA;
                    SumA = y + 3 / SumA;
                    SumA = y + 2 / SumA;
                    SumA = y + 1 / SumA;
                    CND = Exponential / (SumA * 2.506628274631);
                }
            }

            if (x > 0)
                CND = 1 - CND;

            return CND;
        }
        #endregion

        #region private static double GBlackScholes(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        /// <summary>
        /// Generalized Black-Scholes-Merton Option Pricing, see chapter 1.1.6
        /// </summary>
        /// <param name="CallPutFlag">true for calls, false for puts</param>
        /// <param name="S">price of underlying asset</param>
        /// <param name="X">strike price of option</param>
        /// <param name="T">time to expiration in years</param>
        /// <param name="r">risk-free interest rate</param>
        /// <param name="b">cost of carry rate</param>
        /// <param name="v">volatility</param>
        /// <returns></returns>
        public static double GBlackScholes(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        {
            // some additions to make Espen Gaarder Haug's code more robust
            if (T <= 0.0)
                return CallPutFlag
                    ? Math.Max(S - X, 0.0)  // call
                    : Math.Max(X - S, 0.0); // put
            else
                return _GBlackScholes(CallPutFlag, S, X, T, r, b, v);
        }
        private static double _GBlackScholes(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        {
            /*
                '//  The generalized Black and Scholes formula
                Public Function GBlackScholes(CallPutFlag As String, S As Double, x _
                                As Double, T As Double, r As Double, b As Double, v As Double) As Double

                    Dim d1 As Double, d2 As Double
                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    d2 = d1 - v * Sqr(T)

                    If CallPutFlag = "c" Then
                        GBlackScholes = S * Exp((b - r) * T) * CND(d1) - x * Exp(-r * T) * CND(d2)
                    ElseIf CallPutFlag = "p" Then
                        GBlackScholes = x * Exp(-r * T) * CND(-d2) - S * Exp((b - r) * T) * CND(-d1)
                    End If
    
                End Function
             */

            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            double d2 = d1 - v * Math.Sqrt(T);

            double GBlackScholes = CallPutFlag
                ? S * Math.Exp((b - r) * T) * CND(d1) - X * Math.Exp(-r * T) * CND(d2)    // call
                : X * Math.Exp(-r * T) * CND(-d2) - S * Math.Exp((b - r) * T) * CND(-d1); // put

            return GBlackScholes;
        }
        #endregion
        #region private static double GDelta(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        private static double GDelta(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        {
            /*
                Public Function GDelta(CallPutFlag As String, S As Double, x As Double, T As Double, r As Double, b As Double, v As Double) As Double

                    Dim d1 As Double

                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    If CallPutFlag = "c" Then
                        GDelta = Exp((b - r) * T) * CND(d1)
                    Else
                        GDelta = -Exp((b - r) * T) * CND(-d1)
                    End If

                End Function
            */
            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            return CallPutFlag
                ? Math.Exp((b - r) * T) * CND(d1) // call
                : -Math.Exp((b - r) * T) * CND(-d1); // put
        }
        #endregion
        #region private static double GGamma(double S, double X, double T, double r, double b, double v)
        private static double GGamma(double S, double X, double T, double r, double b, double v)
        {
            /*
                '// Gamma for the generalized Black and Scholes formula
                Public Function GGamma(S As Double, x As Double, T As Double, r As Double, b As Double, v As Double) As Double
    
                    Dim d1 As Double
    
                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    GGamma = Exp((b - r) * T) * ND(d1) / (S * v * Sqr(T))
                End Function
            */
            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            return Math.Exp((b - r) * T) * ND(d1) / (S * v * Math.Sqrt(T));
        }
        #endregion
        #region private static double GVega(double S, double X, double T, double r, double b, double v)
        private static double GVega(double S, double X, double T, double r, double b, double v)
        {
            /*
                Public Function GVega(S As Double, x As Double, T As Double, r As Double, b As Double, v As Double) As Double
    
                    Dim d1 As Double
    
                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    GVega = S * Exp((b - r) * T) * ND(d1) * Sqr(T)

                End Function
            */

            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            return S * Math.Exp((b - r) * T) * ND(d1) * Math.Sqrt(T);
        }
        #endregion
        #region private static double GRho(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        private static double GRho(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        {
            /*
                '// Rho for the generalized Black and Scholes formula for all options except futures
                Public Function GRho(CallPutFlag As String, S As Double, x As Double, T As Double, r As Double, b As Double, v As Double) As Double
    
                   Dim d1 As Double, d2 As Double
    
                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    d2 = d1 - v * Sqr(T)
                    If CallPutFlag = "c" Then
                            GRho = T * x * Exp(-r * T) * CND(d2)
                    ElseIf CallPutFlag = "p" Then
                            GRho = -T * x * Exp(-r * T) * CND(-d2)
                    End If

                End Function
            */
            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            double d2 = d1 - v * Math.Sqrt(T);

            return CallPutFlag
                ? T * X * Math.Exp(-r * T) * CND(d2) // call
                : -T * X * Math.Exp(-r * T) * CND(-d2); // put
        }
        #endregion
        #region private static double GTheta(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        private static double GTheta(bool CallPutFlag, double S, double X, double T, double r, double b, double v)
        {
            /*
                Public Function GTheta(CallPutFlag As String, S As Double, x As Double, T As Double, r As Double, b As Double, v As Double) As Double
    
                    Dim d1 As Double, d2 As Double
    
                    d1 = (Log(S / x) + (b + v ^ 2 / 2) * T) / (v * Sqr(T))
                    d2 = d1 - v * Sqr(T)

                    If CallPutFlag = "c" Then
                        GTheta = -S * Exp((b - r) * T) * ND(d1) * v / (2 * Sqr(T)) - (b - r) * S * Exp((b - r) * T) * CND(d1) - r * x * Exp(-r * T) * CND(d2)
                    ElseIf CallPutFlag = "p" Then
                        GTheta = -S * Exp((b - r) * T) * ND(d1) * v / (2 * Sqr(T)) + (b - r) * S * Exp((b - r) * T) * CND(-d1) + r * x * Exp(-r * T) * CND(-d2)
                    End If

                End Function
            */
            double d1 = (Math.Log(S / X) + (b + v * v / 2) * T) / (v * Math.Sqrt(T));
            double d2 = d1 - v * Math.Sqrt(T);

            return CallPutFlag
                ? -S * Math.Exp((b - r) * T) * ND(d1) * v / (2 * Math.Sqrt(T)) - (b - r) * S * Math.Exp((b - r) * T) * CND(d1) - r * X * Math.Exp(-r * T) * CND(d2) // call
                : -S * Math.Exp((b - r) * T) * ND(d1) * v / (2 * Math.Sqrt(T)) + (b - r) * S * Math.Exp((b - r) * T) * CND(-d1) + r * X * Math.Exp(-r * T) * CND(-d2); // put
        }
        #endregion

        #region private static double GImpliedVolatility(bool CallPutFlag, double S, double X, double T, double r, double b, double cm, double epsilon)
        private static double GImpliedVolatility(bool CallPutFlag, double S, double X, double T, double r, double b, double cm, double epsilon)
        {
            // some additions to make Espen Gaarder Haug's code more robust
            // use put-call parity to convert ITM options to OTM
            // this helps making sure the calculation of implied volatility
            // converges, by enforcing a positive value for cm2

            double minValue = 0.005;

            if (CallPutFlag && X < S)
            {
                // ITM call => OTM put
                double cm2 = Math.Max(minValue, cm - S + X * Math.Exp(-r * T));
                return _GImpliedVolatility(!CallPutFlag, S, X, T, r, b, cm2, epsilon);
            }
            else if (!CallPutFlag && X > S)
            {
                // ITM put => OTM call
                double cm2 = Math.Max(minValue, cm + S - X * Math.Exp(-r * T));
                return _GImpliedVolatility(!CallPutFlag, S, X, T, r, b, cm2, epsilon);
            }
            else
            {
                // OTM put or call => as is
                return _GImpliedVolatility(CallPutFlag, S, X, T, r, b, cm, epsilon);
            }
        }
        private static double _GImpliedVolatility(bool CallPutFlag, double S, double X, double T, double r, double b, double cm, double epsilon)
        {
            /*
                Public Function GImpliedVolatilityNR(CallPutFlag As String, S As Double, x _
                As Double, T As Double, r As Double, b As Double, cm As Double, epsilon As Double)

                    Dim vi As Double, ci As Double
                    Dim vegai As Double
                    Dim minDiff As Double

                    'Manaster and Koehler seed value (vi)
                    vi = Sqr(Abs(Log(S / x) + r * T) * 2 / T)
                    ci = GBlackScholes(CallPutFlag, S, x, T, r, b, vi)
                    vegai = GVega(S, x, T, r, b, vi)
                    minDiff = Abs(cm - ci)

                    While Abs(cm - ci) >= epsilon And Abs(cm - ci) <= minDiff
                        vi = vi - (ci - cm) / vegai
                        ci = GBlackScholes(CallPutFlag, S, x, T, r, b, vi)
                        vegai = GVega(S, x, T, r, b, vi)
                        minDiff = Abs(cm - ci)
                    Wend

                    If Abs(cm - ci) < epsilon Then GImpliedVolatilityNR = vi Else GImpliedVolatilityNR = "NA"
                End Function
            */

            // Manaster and Koehler seed value (vi)
            double vi = Math.Sqrt(Math.Abs(Math.Log(S / X) + r * T) * 2 / T);
            vi = Math.Max(1e-5, vi); // FUB addition: vi must never become zero
            double ci = GBlackScholes(CallPutFlag, S, X, T, r, b, vi);
            double vegai = GVega(S, X, T, r, b, vi);
            double minDiff = Math.Abs(cm - ci);

            while (Math.Abs(cm - ci) >= epsilon && Math.Abs(cm - ci) <= minDiff)
            {
                vi = vi - (ci - cm) / vegai;
                vi = Math.Max(1e-5, vi); // FUB addition: vi must never become zero
                ci = GBlackScholes(CallPutFlag, S, X, T, r, b, vi);
                vegai = GVega(S, X, T, r, b, vi);
                minDiff = Math.Abs(cm - ci);
            }

            if (Math.Abs(cm - ci) < epsilon)
                return vi;
            else
                throw new Exception("failed to calculate implied volatility");
        }
        #endregion

        #endregion

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
        /// <param name="dividendYield">annualized continuous dividend yield</param>
        /// <returns>container w/ price, volatility, and greeks</returns>
        public static OptionPriceVolGreeks BlackScholes(this Instrument contract, double volatility, double riskFreeRate, double dividendYield = 0.0)
        {
            if (!contract.IsOption)
                throw new Exception("BlackScholes: input not an option contract");

            DateTime today = contract.Simulator.SimTime[0];
            Instrument underlying = contract.Simulator.Instruments.Where(i => i.Symbol == contract.OptionUnderlying).First();

            bool CallPutFlag = !contract.OptionIsPut;
            double S = underlying.Close[0];
            double X = contract.OptionStrike;
            double T = (contract.OptionExpiry - today).TotalDays / 365.25;
            double r = riskFreeRate;
            double q = dividendYield;
            //double b = r;        // Black and Scholes (1973) stock option model
            double b = r - q;    // Merton (1973) stock option model w/ continuous dividend yield q
            //double b = 0;        // Black (1976) futures option model
            //double b = 0; r = 0; // Asay (1982) margined futures option model
            //double b = r - rf;   // Garman and Kohlhagen (1983) currency option model
            double v = volatility;

            return new OptionPriceVolGreeks
            {
                Price = GBlackScholes(CallPutFlag, S, X, T, r, b, v),
                Volatility = v,
                Delta = GDelta(CallPutFlag, S, X, T, r, b, v),
                Gamma = GGamma(S, X, T, r, b, v),
                Vega = GVega(S, X, T, r, b, v),
                Rho = GRho(CallPutFlag, S, X, T, r, b, v),
                Theta = GTheta(CallPutFlag, S, X, T, r, b, v),
            };
        }
        #endregion
        #region public static OptionPriceVolGreeks BlackScholesImplied(this Instrument contract, double riskFreeRate)
        /// <summary>
        /// Calculate implied volatility from Black-Scholes arbitrage-free price for European-style options,
        /// plus the common option greeks.
        /// <see href="https://en.wikipedia.org/wiki/Black–Scholes_model"/>
        /// </summary>
        /// <param name="contract">option contract to calculate</param>
        /// <param name="riskFreeRate">annualized risk-free rate of return</param>
        /// <param name="dividendYield">annualized continuous dividend yield</param>
        /// <returns>container w/ price, volatility, and greeks</returns>
        public static OptionPriceVolGreeks BlackScholesImplied(this Instrument contract, double riskFreeRate, double dividendYield = 0.0)
        {
            if (!contract.IsOption)
                throw new Exception("BlackScholes: input not an option contract");

            DateTime today = contract.Simulator.SimTime[0];
            Instrument underlying = contract.Simulator.Instruments.Where(i => i.Symbol == contract.OptionUnderlying).First();

            bool CallPutFlag = !contract.OptionIsPut;
            double S = underlying.Close[0];
            double X = contract.OptionStrike;
            double T = (contract.OptionExpiry - today).TotalDays / 365.25;
            double r = riskFreeRate;
            double q = dividendYield;
            //double b = r;        // Black and Scholes (1973) stock option model
            double b = r - q;    // Merton (1973) stock option model w/ continuous dividend yield q
            //double b = 0;        // Black (1976) futures option model
            //double b = 0; r = 0; // Asay (1982) margined futures option model
            //double b = r - rf;   // Garman and Kohlhagen (1983) currency option model
            double cm = 0.5 * (contract.Bid[0] + contract.Ask[0]);
            double epsilon = 0.001;

            double volatility = GImpliedVolatility(CallPutFlag, S, X, T, r, b, cm, epsilon);

            return BlackScholes(contract, volatility, riskFreeRate, dividendYield);
        }
        #endregion

        #region public class RiskGraph
        /// <summary>
        /// Class to calculate the risk graph for option strategies.
        /// </summary>
        public class RiskGraph
        {
            #region internal stuff
            private Dictionary<Instrument, OptionPriceVolGreeks> _greeks = new Dictionary<Instrument, OptionPriceVolGreeks>();
            private double _riskFreeRate;
            private double _todaysTrueValue;

            private double GetTrueValue(Instrument instrument)
            {
                double price = instrument.HasBidAsk
                    ? 0.5 * (instrument.Bid[0] + instrument.Ask[0])
                    : instrument.Close[0];

                return instrument.IsOption
                    ? 100.0 * price
                    : price;
            }

            private double GetHypotheticalValue(Instrument instrument, double hypotheticalUnderlyingPrice, DateTime date)
            {
                if (instrument.IsOption)
                {
                    double r = _riskFreeRate;
                    double q = 0.0; // dividend yield
                    double b = r - q;

                    return 100.0 * GBlackScholes(
                        !instrument.OptionIsPut,
                        hypotheticalUnderlyingPrice,
                        instrument.OptionStrike,
                        (instrument.OptionExpiry - date).TotalDays / 365.25,
                        r,
                        b,
                        _greeks[instrument].Volatility);          
                }
                else
                {
                    return hypotheticalUnderlyingPrice;
                }
            }
            #endregion

            #region public Dictionary<Instrument, int> Positions
            /// <summary>
            /// Dictionary holding the positions for this risk graph
            /// </summary>
            public Dictionary<Instrument, int> Positions = new Dictionary<Instrument, int>();
            #endregion
            #region public void Calc(double riskFreeRate)
            /// <summary>
            /// Calculate option greeks, set reference value.
            /// </summary>
            /// <param name="riskFreeRate"></param>
            public void Calc(double riskFreeRate)
            {
                _riskFreeRate = riskFreeRate;

                foreach (Instrument i in Positions.Keys)
                    if (i.IsOption)
                        _greeks[i] = i.BlackScholesImplied(_riskFreeRate);

                _todaysTrueValue = Positions.Keys
                    .Sum(p => Positions[p] * GetTrueValue(p));
            }
            #endregion
            #region public double PnL(double hypotheticalUnderlyingPrice, DateTime date)
            /// <summary>
            /// Return profit and loss for hypothetical underlying price and target date
            /// </summary>
            /// <param name="hypotheticalUnderlyingPrice"></param>
            /// <param name="date"></param>
            /// <returns></returns>
            public double PnL(double hypotheticalUnderlyingPrice, DateTime date)
            {
                double hypotheticalValue = Positions.Keys
                    .Sum(p => Positions[p]
                        * GetHypotheticalValue(p, hypotheticalUnderlyingPrice, date));
                return hypotheticalValue - _todaysTrueValue;
            }
            #endregion
        }
        #endregion
    }
}

//==============================================================================
// end of file
              