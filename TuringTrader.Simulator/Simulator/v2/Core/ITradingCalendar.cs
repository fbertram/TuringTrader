//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        ITradingCalendar
// Description: Trading calendar interface.
// History:     2021iv23, FUB, created
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

using System;
using System.Collections.Generic;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Trading calendar class to convert a date range to
    /// an enumerable of valid trading days.
    /// </summary>
    public interface ITradingCalendar
    {
        /// <summary>
        /// Start of date range.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End of date range.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// List of trading days between start and end dates,
        /// in the local time zone.
        /// </summary>
        public List<DateTime> TradingDays { get; }

        /// <summary>
        /// Time zone info for exchange.
        /// </summary>
        public TimeZoneInfo ExchangeTimeZone { get; }

        /// <summary>
        /// Time of close, in exchange's time zone.
        /// </summary>
        public TimeOnly TimeOfClose { get; }
    }
}

//==============================================================================
// end of file
