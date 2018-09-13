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
                // ignore
            }

            return 1.0;
        }
    }
}
