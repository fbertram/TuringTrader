//==============================================================================
// Project:     Trading Simulator
// Name:        Volatility
// Description: collection of volatility indicators
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public static class Volatility
    {
        public static double VolatilityCloseToClose(this Instrument instr, int N)
        {
            try
            {
                List<double> logReturns = Enumerable.Range(0, N)
                    .Select(t => Math.Log(instr.Close[t] / instr.Close[t + 1]))
                    .ToList();
                double avg = logReturns
                    .Sum(r => r)
                    / N;
                double sumOfSquares = logReturns
                    .Select(r => Math.Pow(r - avg, 2.0))
                    .Sum(s => s);
                double volatility = Math.Sqrt(252.0 * sumOfSquares / N);
                return volatility;
            }
            catch (Exception)
            {
                // we get here, when attempting to access fields further in the
                // past, than what's available in the instrument's time series
            }

            return 1.0;
        }
    }
}

//==============================================================================
// end of file