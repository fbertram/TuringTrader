//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMomentum
// Description: collection of momentum-based indicators
// History:     2018ix15, FUB, created
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
    public static class IndicatorsMomentum
    {
        // - Stochastic Oscillator
        // - Commodity Channel Index
        // - Relative Strength Index

        #region public static ITimeSeries<double> LinearRegression(this ITimeSeries<double> series, int n)
        public static ITimeSeries<double> LinearRegression(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorLinearRegression>.GetData(
                    Tuple.Create(series, n).GetHashCode(),
                    () => new FunctorLinearRegression(series, n));

            functor.Calc();

            return functor;
        }
        private class FunctorLinearRegression : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorLinearRegression(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = Math.Max(2, n);
            }

            public void Calc()
            {
                double sx = 0.0;
                double sy = 0.0;
                double sxx = 0.0;
                double sxy = 0.0;
                int n = 0;

                try
                {
                    for (int t = 0; t < N; t++)
                    {
                        double x = -t;
                        double y = Math.Log(Series[t]);
                        sx += x;
                        sy += y;
                        sxx += x * x;
                        sxy += x * y;
                        n++;
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                // simple linear regression
                // see https://en.wikipedia.org/wiki/Simple_linear_regression
                // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                //   = (n * Sxy - Sx * Sy) / (n * Sxx - Sx * Sx)
                // a = avg(y) - b * avg(x)
                //   = 1 / n * Sy - b /n * Sx
                if (n > 1)
                {
                    double b = (n * sxy - sx * sy) / (n * sxx - sx * sx);
                    double a = sy / n - b * sx / n;
                    Value = Math.Exp(252.0 * b) - 1.0;
                }
                else
                {
                    Value = 0.0;
                }

                // coefficient of determination
                // see https://en.wikipedia.org/wiki/Coefficient_of_determination
                // f = a + b * x
                // SSreg = sum((f - avg(y))^2)
                // SSres = sum((y - f)^2)
            }
        }
        #endregion
        #region public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        public static ITimeSeries<double> LogRegression(this ITimeSeries<double> series, int n)
        {
            var functor = Cache<FunctorLogRegression>.GetData(
                    Tuple.Create(series, n).GetHashCode(),
                    () => new FunctorLogRegression(series, n));

            functor.Calc();

            return functor;
        }
        private class FunctorLogRegression : TimeSeries<double>
        {
            public ITimeSeries<double> Series;
            public int N;

            public FunctorLogRegression(ITimeSeries<double> series, int n)
            {
                Series = series;
                N = Math.Max(2, n);
            }

            public void Calc()
            {
                double sx = 0.0;
                double sy = 0.0;
                double sxx = 0.0;
                double sxy = 0.0;
                int n = 0;

                try
                {
                    for (int t = 0; t < N; t++)
                    {
                        double x = -t;
                        double y = Math.Log(Series[t]);
                        sx += x;
                        sy += y;
                        sxx += x * x;
                        sxy += x * y;
                        n++;
                    }
                }
                catch (Exception)
                {
                    // we get here when we access bars too far in the past
                }

                // simple linear regression
                // see https://en.wikipedia.org/wiki/Simple_linear_regression
                // b = sum((x - avg(x)) * (y - avg(y)) / sum((x - avg(x))^2)
                //   = (n * Sxy - Sx * Sy) / (n * Sxx - Sx * Sx)
                // a = avg(y) - b * avg(x)
                //   = 1 / n * Sy - b /n * Sx
                if (n > 1)
                {
                    double b = (n * sxy - sx * sy) / (n * sxx - sx * sx);
                    double a = sy / n - b * sx / n;
                    Value = 252.0 * b;
                }
                else
                {
                    Value = 0.0;
                }

                // coefficient of determination
                // see https://en.wikipedia.org/wiki/Coefficient_of_determination
                // f = a + b * x
                // SSreg = sum((f - avg(y))^2)
                // SSres = sum((y - f)^2)
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file