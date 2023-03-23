//==============================================================================
// Project:     TuringTrader, BSOL Algorithms
// Name:        BondETFs
// Description: Collection of Stock-Market ETFs
// History:     2020xii10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
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
        public static readonly string SPX = "splice:$SPX,csv:backfills/$SPX.csv";
        /// <summary>
        /// S&amp;P 500 Total Return Index
        /// </summary>
        public static readonly string SPXTR = "splice:$SPXTR,csv:backfills/$SPXTR.csv";
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
        public static readonly string PORTF_0 = "algorithm:Benchmark_Zero";
        public static readonly string PORTF_60_40 = "algorithm:Benchmark_60_40";
        #endregion
    }
}

//==============================================================================
// end of file
