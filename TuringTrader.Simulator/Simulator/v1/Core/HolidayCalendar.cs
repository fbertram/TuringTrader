//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SimulatorCore
// Description: Simulator engine core
// History:     2018ix10, FUB, created
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
using System.Globalization;

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Holiday calendar for U.S. stock exchanges
    /// </summary>
    static public class HolidayCalendar
    {
        // NOTE: this is a rather disappointing solution. however, 
        // it is good enough to keep us going for the next 2 years
        // see here: https://www.nyse.com/markets/hours-calendars
        private static readonly HashSet<DateTime> holidays = new HashSet<DateTime>
        {
            // additional info:
            // markets close early (1:00 pm Eastern) on
            // July 3rd, if July 4th is a weekday
            // Friday after Thanksgiving
            // 12/24, if 12/25 is a weekday

            //--- 2019
            DateTime.Parse("01/01/2019", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/21/2019", CultureInfo.InvariantCulture), // Martin Luther Kind, Jr. Day
            DateTime.Parse("02/18/2019", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/19/2019", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/27/2019", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/04/2019", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/02/2019", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/28/2019", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2019", CultureInfo.InvariantCulture), // Christmas
            //--- 2020
            DateTime.Parse("01/01/2020", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/20/2020", CultureInfo.InvariantCulture), // Martin Luther Kind, Jr. Day
            DateTime.Parse("02/17/2020", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/10/2020", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/25/2020", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/03/2020", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/07/2020", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/26/2020", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2020", CultureInfo.InvariantCulture), // Christmas
            //--- 2021
            DateTime.Parse("01/01/2021", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/18/2021", CultureInfo.InvariantCulture), // Martin Luther Kind, Jr. Day
            DateTime.Parse("02/15/2021", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/02/2021", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/31/2021", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("07/05/2021", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/06/2021", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/25/2021", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/24/2021", CultureInfo.InvariantCulture), // Christmas
            //--- 2022
            //DateTime.Parse("---", CultureInfo.InvariantCulture), // New Years Day
            DateTime.Parse("01/17/2022", CultureInfo.InvariantCulture), // Martin Luther Kind, Jr. Day
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
            DateTime.Parse("01/16/2023", CultureInfo.InvariantCulture), // Martin Luther Kind, Jr. Day
            DateTime.Parse("02/20/2023", CultureInfo.InvariantCulture), // Washington's Birthday
            DateTime.Parse("04/07/2023", CultureInfo.InvariantCulture), // Good Friday
            DateTime.Parse("05/29/2023", CultureInfo.InvariantCulture), // Memorial Day
            DateTime.Parse("06/19/2023", CultureInfo.InvariantCulture), // Juneteenth National Independence Day
            DateTime.Parse("07/04/2023", CultureInfo.InvariantCulture), // Independence Day
            DateTime.Parse("09/04/2023", CultureInfo.InvariantCulture), // Labor Day
            DateTime.Parse("11/23/2023", CultureInfo.InvariantCulture), // Thanksgiving Day
            DateTime.Parse("12/25/2023", CultureInfo.InvariantCulture), // Christmas Day
        };

        /// <summary>
        /// check, if bar time is holiday.
        /// </summary>
        /// <param name="barTime">time to check</param>
        /// <returns>true, if holiday</returns>
        static public bool IsTradingDay(DateTime barTime)
        {
            if (barTime.DayOfWeek == DayOfWeek.Saturday
            || barTime.DayOfWeek == DayOfWeek.Sunday)
                return false;

            return !holidays.Contains(barTime.Date);
        }

        /// <summary>
        /// find next bar time that is not a holiday
        /// </summary>
        /// <param name="currentBarTime">current simulator timestamp</param>
        /// <returns>next simulator timestamp</returns>
        static public DateTime NextLiveSimTime(DateTime currentBarTime)
        {
            DateTime nextBarTime = currentBarTime;
            do
            {
                nextBarTime += TimeSpan.FromDays(1);
            } while (!IsTradingDay(nextBarTime));

            return nextBarTime;
        }
    }
}

//==============================================================================
// end of file