//==============================================================================
// Project:     TuringTrader, BSOL Algorithms
// Name:        BondETFs
// Description: Collection of Stock-Market ETFs
// History:     2020xii10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
//              http://www.bertram.solutions
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace TuringTrader.Algorithms.Glue
{
    public partial class Indices
    {
        #region U.S. Stock Markets
        /// <summary>
        /// S&amp;P 500 Index
        /// </summary>
        public static readonly string SPX = "splice:$SPX,$SPX++";
        /// <summary>
        /// S&amp;P 500 Total Return Index
        /// </summary>
        public static readonly string SPXTR = "splice:$SPXTR,$SPXTR++";
        // Cboe Volatility Index
        public static readonly string VIX = "$VIX";
        /// <summary>
        /// Nasdaq Composite Index
        /// </summary>
        public static readonly string COMP = "$COMP";
        /// <summary>
        /// Nasdaq-100 Index
        /// </summary>
        public static readonly string NDX = "$NDX";
        /// <summary>
        /// Nasdaq-100 Total Return Index
        /// </summary>
        public static readonly string NDXTR = "$NDXTR";
        #endregion
        #region benchmarks
        public static readonly string PORTF_0 = "algorithm:ZeroReturn";
        public static readonly string PORTF_60_40 = "algorithm:Benchmark_60_40";
        #endregion
    }
}

//==============================================================================
// end of file
