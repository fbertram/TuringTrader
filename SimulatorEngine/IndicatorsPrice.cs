//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsPrice
// Description: collection of price indicators
// History:     2018x31, FUB, created
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
    /// <summary>
    /// Collection of price indicators.
    /// </summary>
    public static class IndicatorsPrice
    {
        #region public static ITimeSeries<double> TypicalPrice(this Instrument series)
        /// <summary>
        /// Calculate Typical Price as described here:
        /// <see href="https://en.wikipedia.org/wiki/Typical_price"/>
        /// </summary>
        /// <param name="series">input instrument</param>
        /// <returns>typical price as time series</returns>
        public static ITimeSeries<double> TypicalPrice(this Instrument series)
        {
            var functor = Cache<FunctorTypicalPrice>.GetData(
                    Cache.UniqueId(series.GetHashCode()),
                    () => new FunctorTypicalPrice(series));

            return functor;
        }

        private class FunctorTypicalPrice : ITimeSeries<double>
        {
            public Instrument Series;

            public FunctorTypicalPrice(Instrument series)
            {
                Series = series;
            }

            public double this[int daysBack]
            {
                get
                {
                    return (Series.High[daysBack] + Series.Low[daysBack] + Series.Close[daysBack]) / 3.0;
                }
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file