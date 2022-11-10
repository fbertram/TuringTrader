﻿//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Assets/Indices
// Description: Definitions for common Indices.
// History:     2022x29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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

namespace TuringTrader.SimulatorV2.Assets
{
    public class Index
    {
        /// <summary>
        /// Dow Jones Industrial Average Index. Available since
        /// 05/26/1896, backfill truncated at 01/02/1970.
        /// </summary>
        public const string DJI = "splice:$DJI,$DJI#";

        /// <summary>
        /// Dow Jones Industrial Average TR Index. Available since
        /// 09/30/1987, backfilled to 01/02/1970.
        /// </summary>
        public const string DJITR = "splice:$DJITR,$DJITR#";

        /// <summary>
        /// S&amp;P 500 Index. Available since 01/03/1928, backfill
        /// truncated at 01/02/1970.
        /// </summary>
        public const string SPX = "splice:$SPX,$SPX#";

        /// <summary>
        /// S&amp;P 500 Total Return Index. Available since 01/04/1988,
        /// backfilled to 01/30/1970.
        /// </summary>
        public const string SPXTR = "splice:$SPXTR,$SPXTR#";

        // OEX
        // OEXTR

        // NDX
        // NDXTR
    }
}

//==============================================================================
// end of file
