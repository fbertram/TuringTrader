//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        TradingCalendar_US
// Description: Trading calendar for U.S. stock exchanges.
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TuringTrader.Simulator.v2
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

        private static readonly List<DateTime> _Holidays = new List<DateTime>
        {
            //--- 1990
            DateTime.Parse("1/1/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/19/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/13/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/28/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/3/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/22/1990	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1990	", CultureInfo.InvariantCulture),
            //--- 1991
            DateTime.Parse("1/1/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/18/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/29/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/27/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/2/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/28/1991	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1991	", CultureInfo.InvariantCulture),
            //--- 1992
            DateTime.Parse("1/1/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/17/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/17/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/25/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/3/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/7/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/26/1992	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1992	", CultureInfo.InvariantCulture),
            //--- 1993
            DateTime.Parse("1/1/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/15/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/9/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/31/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/5/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/6/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/25/1993	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/24/1993	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("2/21/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/1/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/27/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/30/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/5/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/24/1994	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/26/1994	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/2/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/20/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/14/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/29/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/4/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/23/1995	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1995	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/19/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/5/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/27/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/2/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/28/1996	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1996	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/17/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/28/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/26/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/1/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/27/1997	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1997	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/19/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/16/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/10/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/25/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/3/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/7/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/26/1998	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/1998	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/18/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/15/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/2/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/31/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/5/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/6/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/25/1999	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/24/1999	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/17/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/21/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/21/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/29/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/4/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/23/2000	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2000	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/15/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/19/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/13/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/28/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/3/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/11/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/12/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/13/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/14/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/22/2001	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2001	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/21/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/18/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/29/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/27/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/2/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/28/2002	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2002	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/20/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/17/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/18/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/26/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/1/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/27/2003	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2003	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/19/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/16/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/9/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/31/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("6/11/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/5/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/6/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/25/2004	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/24/2004	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/17/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/21/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/25/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/30/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/5/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/24/2005	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/26/2005	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/2/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/16/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/20/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/14/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/29/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/4/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/23/2006	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2006	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/2/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/15/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/19/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/6/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/28/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/3/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/22/2007	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2007	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/21/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/18/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/21/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/26/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/1/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/27/2008	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2008	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/19/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/16/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/10/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/25/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/3/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/7/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/26/2009	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2009	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/18/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/15/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/2/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/31/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/5/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/6/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/25/2010	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/24/2010	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/17/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/21/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/22/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/30/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/5/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/24/2011	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/26/2011	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/2/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/16/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/20/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/6/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/28/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/3/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("10/29/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("10/30/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/22/2012	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2012	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/21/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/18/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/29/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/27/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/2/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/28/2013	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2013	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/20/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/17/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/18/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/26/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/1/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/27/2014	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2014	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/19/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/16/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/3/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/25/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/3/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/7/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/26/2015	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2015	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/18/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/15/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/25/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/30/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/5/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/24/2016	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/26/2016	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/2/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/16/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/20/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("4/14/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/29/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/4/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/23/2017	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2017	", CultureInfo.InvariantCulture),
            //---
            DateTime.Parse("1/1/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("1/15/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("2/19/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("3/30/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("5/28/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("7/4/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("9/3/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("11/22/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/5/2018	", CultureInfo.InvariantCulture),
            DateTime.Parse("12/25/2018	", CultureInfo.InvariantCulture),
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

        };
        #endregion
        #region internal helpers
        private readonly DateTime _earliestTime = new DateTime(1990, 1, 1, 0, 1, 0, DateTimeKind.Utc);
        private readonly TimeZoneInfo _exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); // New York, USA

        /// <summary>
        /// Check for weekends and holidays at exchange.
        /// </summary>
        /// <param name="exchangeDateTime">date and time to check, in exchange time zone</param>
        /// <returns>true, if weekend or holiday</returns>
        private bool IsHoliday(DateTime exchangeDateTime)
        {
            if (exchangeDateTime.DayOfWeek == DayOfWeek.Saturday 
            || exchangeDateTime.DayOfWeek == DayOfWeek.Sunday)
                return true;

            if (_Holidays.Contains(exchangeDateTime.Date))
                return true;

            return false;
        }

        /// <summary>
        /// Determine previous exchange close.
        /// </summary>
        /// <param name="localDateTime">date time, in local time zone</param>
        /// <returns>previous close, in local time zone</returns>
        private DateTime PreviousExchangeClose(DateTime localDateTime)
        {
            // convert to exchange time zone and set time to 4pm
            var exchangeTime = TimeZoneInfo.ConvertTime(localDateTime, _exchangeTimeZone);
            var exchangeAtClose = exchangeTime.Date + TimeSpan.FromHours(16);

            // make sure time is in the past, skip weekends and holidays
            while (exchangeAtClose > exchangeTime || IsHoliday(exchangeAtClose))
                exchangeAtClose -= TimeSpan.FromDays(1);

            // convert back to local time zone
            var utcAtClose = TimeZoneInfo.ConvertTimeToUtc(exchangeAtClose, _exchangeTimeZone);
            var localAtClose = utcAtClose.ToLocalTime();

            return localAtClose;
        }
        #endregion

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public IEnumerable<DateTime> TradingDays
        {
            get
            {
                var startDate = StartDate > _earliestTime ? StartDate : _earliestTime;
                var endDate = EndDate < DateTime.Now ? EndDate : DateTime.Now;

                var date = StartDate;
                var previousClose = default(DateTime);

                while (date <= EndDate)
                {
                    var close = PreviousExchangeClose(date);
                    if (close != previousClose)
                    {
                        yield return close;
                        date = close; // make sure date is well-aligned w/ exchange's closing time
                        previousClose = close;
                    }

                    // NOTE: we add 26 hours here, as we might start/ end daylight 
                    // saving time in either the local or the exchange's time zone
                    date += TimeSpan.FromHours(26);
                }
                yield break;
            }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

//==============================================================================
// end of file
