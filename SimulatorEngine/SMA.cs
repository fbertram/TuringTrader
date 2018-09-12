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
    public partial class Algorithm
    {

    }

    public class SMA : TimeSeries<double>
    {
        public static double CalcSMA(ITimeSeries<double> series, int length)
        {
            double retval = 0.0;
            for (int t = 0; t < length; t++)
            {
                try
                {
                    retval += series[t];
                }
                catch (Exception)
                {
                    return retval / (length - 1);
                }
            }

            return retval / length;
        }
    }
}

//==============================================================================
// end of file