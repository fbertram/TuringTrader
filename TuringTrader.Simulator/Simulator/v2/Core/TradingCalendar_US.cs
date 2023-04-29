//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        TradingCalendar_US
// Description: Trading calendar for U.S. stock exchanges.
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Trading calendar for U.S. stock exchanges.
    /// </summary>
    public class TradingCalendar_US : ITradingCalendar
    {
        #region holiday list
        // NOTE: this is a rather disappointing solution. however, 
        // it is good enough to keep us going for the next 2 years
        // see here: https://www.nyse.com/markets/hours-calendars

        // additional info:
        // markets close early (1:00 pm Eastern) on
        // July 3rd, if July 4th is a weekday
        // Friday after Thanksgiving
        // 12/24, if 12/25 is a weekday

        private static readonly List<DateTime> _holidays = new List<DateTime>
        {
            // see https://www.nyse.com/markets/hours-calendars
            // NYSE Saturday trading retired in 1952
            // 10/10 Columbus Day: NYSE open, NASDAQ open, Bond market closed (all closed 1909-1953)
#if true
            #region 1950
            //========== these dates have been derived from SPX quotes
            //--- 1950
            DateTime.Parse("01/02/1950", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/20/1950", CultureInfo.InvariantCulture), // Presidents Day
            DateTime.Parse("04/07/1950", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1950", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/03/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/10/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/17/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/24/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/01/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/04/1950", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/08/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/15/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/22/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/29/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/05/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/12/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/19/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/26/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/02/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/04/1950", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/09/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/16/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/23/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/30/1950", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("10/09/1959", CultureInfo.InvariantCulture), // Columbus Day
            DateTime.Parse("11/10/1950", CultureInfo.InvariantCulture), // Veterans Day
            DateTime.Parse("11/23/1950", CultureInfo.InvariantCulture), // Thankgsgiving Day
            DateTime.Parse("12/25/1950", CultureInfo.InvariantCulture), // Christmas Day
            #endregion
#endif
#if true
            #region 1951 - 1960
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-1951-1960.html
            //--- 1951
            DateTime.Parse("01/01/1951", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/12/1951", CultureInfo.InvariantCulture), // Lincoln's Birthday
            DateTime.Parse("02/22/1951", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/23/1951", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1951", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/02/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/09/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/14/1951", CultureInfo.InvariantCulture), // Flag Day
            DateTime.Parse("06/16/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/23/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/30/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/04/1951", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/07/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/14/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/21/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/28/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/04/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/11/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/18/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/25/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/01/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/03/1951", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/08/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/15/1951", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("10/12/1951", CultureInfo.InvariantCulture), // Columbus Day
            DateTime.Parse("11/06/1951", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/12/1951", CultureInfo.InvariantCulture), // Veteran's Day
            DateTime.Parse("11/22/1951", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1951", CultureInfo.InvariantCulture), // Christmas
            //--- 1952
            DateTime.Parse("01/01/1952", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/12/1952", CultureInfo.InvariantCulture), // Lincoln's Birthday
            DateTime.Parse("02/22/1952", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/11/1952", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1952", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("05/31/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/07/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/13/1952", CultureInfo.InvariantCulture), // Flag Day
            DateTime.Parse("06/14/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/21/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("06/28/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/04/1952", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/05/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/12/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/19/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("07/26/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/02/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/09/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/16/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/23/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("08/30/1952", CultureInfo.InvariantCulture), // Saturday summertime 
            DateTime.Parse("09/01/1952", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/06/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/13/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/20/1952", CultureInfo.InvariantCulture), // Saturday summertime
            DateTime.Parse("09/27/1952", CultureInfo.InvariantCulture), // ??? Saturday summertime
            DateTime.Parse("10/13/1952", CultureInfo.InvariantCulture), // Columbus Day
            DateTime.Parse("11/04/1952", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/11/1952", CultureInfo.InvariantCulture), // Veterans Day
            DateTime.Parse("11/27/1952", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1952", CultureInfo.InvariantCulture), // Christmas
            //--- 1953
            DateTime.Parse("01/01/1953", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/12/1953", CultureInfo.InvariantCulture), // Lincoln's Birthday
            DateTime.Parse("02/23/1953", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/03/1953", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1953", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/15/1953", CultureInfo.InvariantCulture), // Flag Day
            DateTime.Parse("07/03/1953", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1953", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("10/12/1953", CultureInfo.InvariantCulture), // Columbus Day
            DateTime.Parse("11/03/1953", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/11/1953", CultureInfo.InvariantCulture), // Veterans Day
            DateTime.Parse("11/26/1953", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1953", CultureInfo.InvariantCulture), // Christmas
            //--- 1954
            DateTime.Parse("01/01/1954", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1954", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/16/1954", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1954", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1954", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1954", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/02/1954", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/25/1954", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1954", CultureInfo.InvariantCulture), // Christmas
            //--- 1955
            DateTime.Parse("12/31/1954", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1955", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/08/1955", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1955", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1955", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1955", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/08/1955", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/24/1955", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1955", CultureInfo.InvariantCulture), // Christmas
            //--- 1956
            DateTime.Parse("01/02/1956", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1956", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/30/1956", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1956", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1956", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1956", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/06/1956", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/22/1956", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1956", CultureInfo.InvariantCulture), // Christmas Eve
            DateTime.Parse("12/25/1956", CultureInfo.InvariantCulture), // Christmas
            //--- 1957
            DateTime.Parse("01/01/1957", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1957", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/19/1957", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1957", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1957", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1957", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/05/1957", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/28/1957", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1957", CultureInfo.InvariantCulture), // Christmas
            //--- 1958
            DateTime.Parse("01/01/1958", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/21/1958", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/04/1958", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1958", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1958", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/1958", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/04/1958", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/27/1958", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1958", CultureInfo.InvariantCulture), // Christmas
            DateTime.Parse("12/26/1958", CultureInfo.InvariantCulture), // Day After Christmas
            //--- 1959
            DateTime.Parse("01/01/1959", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/23/1959", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/27/1959", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1959", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1959", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1959", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/03/1959", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/26/1959", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1959", CultureInfo.InvariantCulture), // Christams
            //--- 1960
            DateTime.Parse("01/01/1960", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1960", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/15/1960", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1960", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1960", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1960", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/08/1960", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/24/1960", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1960", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 1961 - 1970
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-1961-1970.html
            //--- 1961
            DateTime.Parse("01/02/1961", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1961", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/31/1961", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1961", CultureInfo.InvariantCulture), // Day Before Decoration Day
            DateTime.Parse("05/30/1961", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1961", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1961", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/07/1961", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/23/1961", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1961", CultureInfo.InvariantCulture), // Christmas
            //--- 1962
            DateTime.Parse("01/01/1962", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1962", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/20/1962", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1962", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1962", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1962", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/06/1962", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/22/1962", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1962", CultureInfo.InvariantCulture), // Christmas
            //--- 1963
            DateTime.Parse("01/01/1963", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1963", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/12/1963", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1963", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1963", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1963", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/05/1963", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/25/1963", CultureInfo.InvariantCulture), // Presidential Funeral - John F Kennedy
            DateTime.Parse("11/28/1963", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1963", CultureInfo.InvariantCulture), // Christmas
            //--- 1964
            DateTime.Parse("01/01/1964", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/21/1964", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/27/1964", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1964", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1964", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1964", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/03/1964", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/26/1964", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1964", CultureInfo.InvariantCulture), // Christmas
            //--- 1965
            DateTime.Parse("01/01/1965", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1965", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/16/1965", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1965", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1965", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1965", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/02/1965", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/25/1965", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1965", CultureInfo.InvariantCulture), // Christmas
            //--- 1966
            DateTime.Parse("02/22/1966", CultureInfo.InvariantCulture), // Washington's Birtyday
            DateTime.Parse("04/08/1966", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1966", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1966", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1966", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/08/1966", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/24/1966", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1966", CultureInfo.InvariantCulture), // Christmas
            //--- 1967
            DateTime.Parse("01/02/1967", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/22/1967", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/24/1967", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1967", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1967", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1967", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/07/1967", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/23/1967", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1967", CultureInfo.InvariantCulture), // Christmas
            //--- 1968
            DateTime.Parse("01/01/1968", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/12/1968", CultureInfo.InvariantCulture), // Lincoln's Birthday
            DateTime.Parse("02/22/1968", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/09/1968", CultureInfo.InvariantCulture), // Day of Mourning - Martin Luther King
            DateTime.Parse("04/12/1968", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1968", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/12/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("06/19/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("06/26/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("07/03/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("07/04/1968", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/05/1968", CultureInfo.InvariantCulture), // Day after Independence Day
            DateTime.Parse("07/10/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("07/17/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("07/24/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("07/31/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("08/07/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("08/14/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("08/21/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("08/28/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("09/02/1968", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/04/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("09/11/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("09/18/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("09/25/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("10/02/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("10/09/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("10/16/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("10/23/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("10/30/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("11/05/1968", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/06/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("11/13/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("11/20/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("11/27/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("11/28/1968", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/04/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("12/11/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("12/18/1968", CultureInfo.InvariantCulture), // Paper Crisis
            DateTime.Parse("12/25/1968", CultureInfo.InvariantCulture), // Christmas
            //--- 1969
            DateTime.Parse("01/01/1969", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/10/1969", CultureInfo.InvariantCulture), // Weather - Snow
            DateTime.Parse("02/21/1969", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/31/1969", CultureInfo.InvariantCulture), // Presidential Funeral - Dwight Eisenhower
            DateTime.Parse("04/04/1969", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1969", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1969", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/21/1969", CultureInfo.InvariantCulture), // First Lunar Landing
            DateTime.Parse("09/01/1969", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/1969", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1969", CultureInfo.InvariantCulture), // Christmas
            //--- 1970
            DateTime.Parse("01/01/1970", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/23/1970", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/27/1970", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1970", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1970", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1970", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/1970", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1970", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 1971 - 1980
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-1971-1980.html
            //--- 1971
            DateTime.Parse("01/01/1971", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/15/1971", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/09/1971", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1971", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1971", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1971", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/1971", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1971", CultureInfo.InvariantCulture), // Christmas
            //--- 1972
            DateTime.Parse("02/21/1972", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/31/1972", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1972", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1972", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1972", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/07/1972", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/23/1972", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1972", CultureInfo.InvariantCulture), // Christmas
            DateTime.Parse("12/28/1972", CultureInfo.InvariantCulture), // Presidential Funeral - Harry Truman
            //--- 1973
            DateTime.Parse("01/01/1973", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/25/1973", CultureInfo.InvariantCulture), // Presidential Funeral - Lyndon Johnson
            DateTime.Parse("02/19/1973", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/20/1973", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/1973", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1973", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1973", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/1973", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1973", CultureInfo.InvariantCulture), // Christmas
            //--- 1974
            DateTime.Parse("01/01/1974", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/18/1974", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/12/1974", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/1974", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1974", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1974", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/1974", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1974", CultureInfo.InvariantCulture), // Christmas
            //--- 1975
            DateTime.Parse("01/01/1975", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/17/1975", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/28/1975", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/1975", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1975", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/1975", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/1975", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1975", CultureInfo.InvariantCulture), // Christmas
            //--- 1976
            DateTime.Parse("01/01/1976", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/16/1976", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/16/1976", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1976", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1976", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1976", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/02/1976", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/25/1976", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1976", CultureInfo.InvariantCulture), // Christmas
            //--- 1977
            DateTime.Parse("02/21/1977", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/08/1977", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1977", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1977", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("07/14/1977", CultureInfo.InvariantCulture), // New York City Blackout
            DateTime.Parse("09/05/1977", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/1977", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1977", CultureInfo.InvariantCulture), // Christmas
            //--- 1978
            DateTime.Parse("01/02/1978", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/20/1978", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/24/1978", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1978", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1978", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1978", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/1978", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1978", CultureInfo.InvariantCulture), // Christmas
            //--- 1979
            DateTime.Parse("01/01/1979", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/19/1979", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/13/1979", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/1979", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1979", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1979", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/1979", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1979", CultureInfo.InvariantCulture), // Christmas
            //--- 1980
            DateTime.Parse("01/01/1980", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/18/1980", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/04/1980", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/1980", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1980", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/1980", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/04/1980", CultureInfo.InvariantCulture), // Election Day
            DateTime.Parse("11/27/1980", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1980", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 1981 - 1990
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-1981-1990.html
            //--- 1981
            DateTime.Parse("01/01/1981", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/16/1981", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/17/1981", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/1981", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1981", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1981", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/1981", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1981", CultureInfo.InvariantCulture), // Christmas
            //--- 1982
            DateTime.Parse("01/01/1982", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/15/1982", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/09/1982", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1982", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1982", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1982", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/1982", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1982", CultureInfo.InvariantCulture), // Christmas
            //--- 1983
            DateTime.Parse("02/21/1983", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/01/1983", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1983", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1983", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1983", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/1983", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1983", CultureInfo.InvariantCulture), // Christmas
            //--- 1984
            DateTime.Parse("01/02/1984", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/20/1984", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/20/1984", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/1984", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1984", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1984", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/1984", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1984", CultureInfo.InvariantCulture), // Christmas
            //--- 1985
            DateTime.Parse("01/01/1985", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/18/1985", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/05/1985", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/1985", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1985", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1985", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/27/1985", CultureInfo.InvariantCulture), // Weather - Hurricane Gloria
            DateTime.Parse("11/28/1985", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1985", CultureInfo.InvariantCulture), // Christmas
            //--- 1986
            DateTime.Parse("01/01/1986", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/17/1986", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/28/1986", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/1986", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1986", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/1986", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/1986", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1986", CultureInfo.InvariantCulture), // Christmas
            //--- 1987
            DateTime.Parse("01/01/1987", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/16/1987", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/17/1987", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/1987", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1987", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1987", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/1987", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1987", CultureInfo.InvariantCulture), // Christmas
            //--- 1988
            DateTime.Parse("01/01/1988", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/15/1988", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/01/1988", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/1988", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1988", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1988", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/1988", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1988", CultureInfo.InvariantCulture), // Christmas
            //--- 1989
            DateTime.Parse("01/02/1989", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/20/1989", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/24/1989", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1989", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1989", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1989", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/1989", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1989", CultureInfo.InvariantCulture), // Christmas
            //--- 1990
            DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/19/1990", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/13/1990", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/1990", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1990", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/1990", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/1990", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1990", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 1991 - 2000
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-1991-2000.html
            //--- 1991
            DateTime.Parse("01/01/1991", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/18/1991", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/29/1991", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/1991", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1991", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1991", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/1991", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1991", CultureInfo.InvariantCulture), // Christmas
            //--- 1992
            DateTime.Parse("01/01/1992", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/17/1992", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/17/1992", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/1992", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1992", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1992", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/1992", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1992", CultureInfo.InvariantCulture), // Christmas
            //--- 1993
            DateTime.Parse("01/01/1993", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/15/1993", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/09/1993", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1993", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1993", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1993", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/1993", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1993", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("02/21/1994", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/01/1994", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("04/27/1994", CultureInfo.InvariantCulture), // Presidential Funeral - Richard Nixon
            DateTime.Parse("05/30/1994", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1994", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/1994", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/1994", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/1994", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/02/1995", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/20/1995", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/14/1995", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/1995", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1995", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/1995", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/1995", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1995", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/1996", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/19/1996", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/05/1996", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/1996", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1996", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/1996", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/1996", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1996", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/1997", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("02/17/1997", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/28/1997", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/1997", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/1997", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/1997", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/1997", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1997", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/1998", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/19/1998", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/16/1998", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/10/1998", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/1998", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/1998", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/1998", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/1998", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/1998", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/1999", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/18/1999", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/15/1999", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/02/1999", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/1999", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/1999", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/1999", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/1999", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/1999", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/17/2000", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/21/2000", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/21/2000", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/2000", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2000", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/2000", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/2000", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2000", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 2001 - 2010
            // see http://nyseholidays.blogspot.com/2012/11/nyse-holidays-from-2000-2010.html
            //---
            DateTime.Parse("01/01/2001", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/15/2001", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/19/2001", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/13/2001", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/2001", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2001", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/2001", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("09/11/2001", CultureInfo.InvariantCulture), // World Trade Center Event
            DateTime.Parse("09/12/2001", CultureInfo.InvariantCulture), // World Trade Center Event
            DateTime.Parse("09/13/2001", CultureInfo.InvariantCulture), // World Trade Center Event
            DateTime.Parse("09/14/2001", CultureInfo.InvariantCulture), // World Trade Center Event
            DateTime.Parse("11/22/2001", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2001", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2002", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/21/2002", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/18/2002", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/29/2002", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/2002", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2002", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/2002", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/2002", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2002", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2003", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/20/2003", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/17/2003", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/18/2003", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/2003", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2003", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/2003", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/2003", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2003", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2004", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/19/2004", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/16/2004", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/09/2004", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/2004", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/11/2004", CultureInfo.InvariantCulture), // Presidential Funeral - Ronald Reagan
            DateTime.Parse("07/05/2004", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/2004", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/2004", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/24/2004", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/17/2005", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/21/2005", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/25/2005", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/2005", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2005", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/2005", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/2005", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/2005", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/02/2006", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/16/2006", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/20/2006", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/14/2006", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/2006", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2006", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/2006", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/2006", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2006", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/02/2007", CultureInfo.InvariantCulture), // Day of Mourning - Gerald Ford
            DateTime.Parse("01/15/2007", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/19/2007", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/06/2007", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/2007", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2007", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/2007", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/2007", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2007", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2008", CultureInfo.InvariantCulture), // New Year sDay
            DateTime.Parse("01/21/2008", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/18/2008", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/21/2008", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/2008", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2008", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/2008", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/2008", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2008", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2009", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/19/2009", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/16/2009", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/10/2009", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/2009", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/2009", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/2009", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/2009", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2009", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2010", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/18/2010", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/15/2010", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/02/2010", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/2010", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/2010", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/2010", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/2010", CultureInfo.InvariantCulture), // Thanksgivinig
            DateTime.Parse("12/24/2010", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 2011 - 2020
            // see http://nyseholidays.blogspot.com/search/label/NYSE%20Holidays
            //---
            DateTime.Parse("01/17/2011", CultureInfo.InvariantCulture), // Martin Luther King Day
            DateTime.Parse("02/21/2011", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/22/2011", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/2011", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2011", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/2011", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/2011", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/2011", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/02/2012", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/16/2012", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/20/2012", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/06/2012", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/2012", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2012", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/2012", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("10/29/2012", CultureInfo.InvariantCulture), // Weather - Hurricane Sandy
            DateTime.Parse("10/30/2012", CultureInfo.InvariantCulture), // Weather - Hurricane Sandy
            DateTime.Parse("11/22/2012", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2012", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2013", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/21/2013", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/18/2013", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/29/2013", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/2013", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2013", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/2013", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/2013", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2013", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2014", CultureInfo.InvariantCulture), // New Years day
            DateTime.Parse("01/20/2014", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/17/2014", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/18/2014", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/2014", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2014", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/2014", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/2014", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2014", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2015", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/19/2015", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/16/2015", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/03/2015", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/2015", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/2015", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/2015", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/2015", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2015", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2016", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/18/2016", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/15/2016", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/25/2016", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/2016", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2016", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/2016", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/2016", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/26/2016", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/02/2017", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/16/2017", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/20/2017", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/14/2017", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/2017", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2017", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/2017", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/2017", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/25/2017", CultureInfo.InvariantCulture), // Christmas
            //---
            DateTime.Parse("01/01/2018", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/15/2018", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/19/2018", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/30/2018", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/28/2018", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2018", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/03/2018", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/22/2018", CultureInfo.InvariantCulture), // Thanksgiving
            DateTime.Parse("12/05/2018", CultureInfo.InvariantCulture), // Presidential Funeral - George HW Bush
            DateTime.Parse("12/25/2018", CultureInfo.InvariantCulture), // Christmas
            //--- 2019
            DateTime.Parse("01/01/2019", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/21/2019", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/18/2019", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/19/2019", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/2019", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2019", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/2019", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/2019", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2019", CultureInfo.InvariantCulture), // Christmas
            //--- 2020
            DateTime.Parse("01/01/2020", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/20/2020", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/17/2020", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/10/2020", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/2020", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/2020", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/2020", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/2020", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2020", CultureInfo.InvariantCulture), // Christmas
            #endregion
#endif
#if true
            #region 2021 - present
            //--- 2021
            DateTime.Parse("01/01/2021", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/18/2021", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/15/2021", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/02/2021", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/2021", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/2021", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/2021", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/2021", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/24/2021", CultureInfo.InvariantCulture), // Christmas
            //--- 2022
            //DateTime.Parse("---", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/17/2022", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/21/2022", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/15/2022", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/30/2022", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/20/2022", CultureInfo.InvariantCulture), // Juneteenth National Independence Day
            DateTime.Parse("07/04/2022", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/05/2022", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/24/2022", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/26/2022", CultureInfo.InvariantCulture), // Christmas Day
            //--- 2023
            DateTime.Parse("01/02/2023", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/16/2023", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/20/2023", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/07/2023", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/2023", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/19/2023", CultureInfo.InvariantCulture), // Juneteenth National Independence Day
            DateTime.Parse("07/04/2023", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/2023", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/2023", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2023", CultureInfo.InvariantCulture), // Christmas Day
            //--- 2024
            DateTime.Parse("01/01/2024", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/15/2024", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/19/2024", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("03/29/2024", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/2024", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/19/2024", CultureInfo.InvariantCulture), // Juneteenth National Independence Day
            DateTime.Parse("07/04/2024", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/2024", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/2024", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2024", CultureInfo.InvariantCulture), // Christmas Day
            //--- 2025
            DateTime.Parse("01/01/2025", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/20/2025", CultureInfo.InvariantCulture), // Martin Luther King, Jr. Day
            DateTime.Parse("02/17/2025", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/18/2025", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/26/2025", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/19/2025", CultureInfo.InvariantCulture), // Juneteenth National Independence Day
            DateTime.Parse("07/04/2025", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/01/2025", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/27/2025", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2025", CultureInfo.InvariantCulture), // Christmas Day
            //---
            #endregion
#endif
        };
        #endregion
        #region internal helpers
        //private readonly DateTime _earliestTime = new DateTime(1990, 1, 1, 0, 1, 0, DateTimeKind.Utc);
        private readonly DateTime _earliestTime = new DateTime(1950, 1, 1, 0, 1, 0, DateTimeKind.Utc);

        /// <summary>
        /// Check for weekends and holidays at exchange.
        /// </summary>
        /// <param name="exchangeDateTime">date and time to check, in exchange time zone</param>
        /// <returns>true, if weekend or holiday</returns>
        private bool _isHoliday(DateTime exchangeDateTime)
        {
            if ((exchangeDateTime.DayOfWeek == DayOfWeek.Saturday && exchangeDateTime.Year >= 1953)
            || exchangeDateTime.DayOfWeek == DayOfWeek.Sunday)
                return true;

            if (_holidays.Contains(exchangeDateTime.Date))
                return true;

            return false;
        }

        /// <summary>
        /// Determine previous exchange close.
        /// </summary>
        /// <param name="localDateTime">date time, in local time zone</param>
        /// <returns>previous close, in local time zone</returns>
        private DateTime _previousExchangeClose(DateTime localDateTime)
        {
            // convert to exchange time zone and set time to 4pm
            var exchangeTime = TimeZoneInfo.ConvertTime(localDateTime, ExchangeTimeZone);
            var exchangeAtClose = exchangeTime.Date + TimeOfClose.ToTimeSpan();

            // make sure time is in the past, skip weekends and holidays
            while (exchangeAtClose > exchangeTime || _isHoliday(exchangeAtClose))
                exchangeAtClose -= TimeSpan.FromDays(1);

            // convert back to local time zone
            var localAtClose = TimeZoneInfo.ConvertTimeToUtc(exchangeAtClose, ExchangeTimeZone)
                .ToLocalTime();

            return localAtClose;
        }
        #endregion

        public TimeZoneInfo ExchangeTimeZone => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); // New York, USA
        public TimeOnly TimeOfClose => TimeOnly.Parse("16:00"); // NYSE closes at 4pm
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<DateTime> TradingDays
        {
            get
            {
                var startDate = StartDate > _earliestTime ? StartDate : _earliestTime;
                var endDate = EndDate > startDate ? EndDate : startDate + TimeSpan.FromDays(5);

#if true
                // new 11/04/2022
                var previousClose = default(DateTime);

                var tradingDays = new List<DateTime>();
                // NOTE: We cannot stop the loop when date > endDate, because we might
                //       still find close < endDate. To fix this, we loop one day further.
                for (var date = startDate; date <= endDate + TimeSpan.FromDays(1); date += TimeSpan.FromDays(1))
                {
                    var close = _previousExchangeClose(date);

                    if (close >= startDate && close <= endDate && close != previousClose)
                    {
                        tradingDays.Add(close);
                        previousClose = close;
                    }
                }

                return tradingDays;
#else
                // retired 11/04/2022
                var date = startDate;
                var previousClose = default(DateTime);

                var tradingDays = new List<DateTime>();
                while (date <= endDate)
                {
                    var close = PreviousExchangeClose(date);

                    if (close > endDate)
                        break;

                    if (close != previousClose && close >= startDate)
                    {
                        tradingDays.Add(close);
                        date = close; // make sure date is well-aligned w/ exchange's closing time
                        previousClose = close;
                    }

                    // NOTE: we add 26 hours here, as we might start/ end daylight 
                    // saving time in either the local or the exchange's time zone
                    date += TimeSpan.FromHours(26);
                }

                return tradingDays;
#endif
            }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

//==============================================================================
// end of file
