//==============================================================================
// Project:     Trading Simulator
// Name:        SMA
// Description: simple moving average
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
    public static class MovingAverages
    {
        public static double SMA(this ITimeSeries<double> series, int N)
        {
            int num = 0;
            double sum = 0.0;
            try
            {
                for (int t = 0; t < N; t++)
                {
                    sum += series[t];
                    num++;
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return sum / num;
        }
    }
}

//==============================================================================
// end of file