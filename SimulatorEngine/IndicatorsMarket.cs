//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMarket
// Description: collection of market indicators
// History:     2018ix17, FUB, created
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

namespace TuringTrader.Simulator
{
    public static class IndicatorsMarket
    {
        public static ITimeSeries<double> AdvanceDecline(this IEnumerable<Instrument> market, int n)
        {
            throw new Exception("IndicatorsMarket: AdvanceDecline not implemented.");
        }
    }
}

//==============================================================================
// end of file