//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A03_Quotes
// Description: Develop & test quote import and alignment w/ trading calendar.
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
using System.Globalization;
using System.Text;
using TuringTrader.Simulator.v2;

namespace TuringTrader.Simulator.v2.Demo
{
    public class A03_Quotes : Algorithm
    {
        public override string Name => "A03_Quotes";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2021", CultureInfo.InvariantCulture);
            EndDate = DateTime.Parse("05/01/2021", CultureInfo.InvariantCulture);

            var goldName = "fred:GOLDPMGBD228NLBM";

            foreach (var date in TradingDays)
            {
                // NOTE: the first call loads the quotations,
                // subsequent calls are served from a cache.
                var goldAsset = Asset(goldName);
                Output.WriteLine("{0:ddd, MM/dd/yyyy}: {1:C2}", date, 0.0);
            }
        }

        public override void Report() => Output.WriteLine("Here is your report");
    }
}

//==============================================================================
// end of file
