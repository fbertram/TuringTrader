//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Algorithm
// Description: Algorithm base class/ simulator core.
// History:     2021iv23, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Enterprises LLC
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

using System;
using System.Collections.Generic;
using System.Text;
using TuringTrader.Simulator.Simulator.v2;

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// Base class for trading algorithms.
    /// </summary>
    public abstract class Algorithm : AlgorithmApi
    {
        #region simulation range
        /// <summary>
        /// Simulation start date.
        /// </summary>
        public DateTime StartDate { get => TradingCalendar.StartDate; set => TradingCalendar.StartDate = value; }

        /// <summary>
        /// Simulation end date.
        /// </summary>
        public DateTime EndDate { get => TradingCalendar.EndDate; set => TradingCalendar.EndDate = value; }

        /// <summary>
        /// Enumerable of valid trading days between StartDate and EndDate.
        /// </summary>
        public IEnumerable<DateTime> TradingDays { get => TradingCalendar.TradingDays; }

        /// <summary>
        /// Trading calendar, converting simulation date range to
        /// enumerable of valid trading days.
        /// </summary>
        public ITradingCalendar TradingCalendar { get; set; } = new TradingCalendar_US();
        #endregion
        #region assets
        /// <summary>
        /// Load quotations for tradeable asset. Subsequent calls to
        /// this method with the same name will be served from a cache.
        /// </summary>
        /// <param name="name">name of asset</param>
        /// <returns>asset</returns>
        public IAsset Asset(string name) => null;
        #endregion
    }
}

//==============================================================================
// end of file
