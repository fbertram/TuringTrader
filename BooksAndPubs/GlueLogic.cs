//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        GlueLogic
// Description: some glue to help re-using algorithms for other applications
// History:     2019x02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2098, Bertram Solutions LLC
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

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    class Globals
    {
        public static DateTime WARMUP_START_TIME = DateTime.Parse("01/01/2006", CultureInfo.InvariantCulture);
        public static DateTime START_TIME = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
        public static DateTime END_TIME = DateTime.Now.Date + TimeSpan.FromHours(16);

        public static double COMMISSION = 0.015;
    }

    class AllocationTracker
    {
        public DateTime LastUpdate { get; set; }

        public Dictionary<Instrument, double> Allocation  = new Dictionary<Instrument, double>();

        public void ToPlotter(Plotter plotter)
        {
            plotter.SelectChart(string.Format("Asset Allocation as of {0:MM/dd/yyyy}", LastUpdate), "Name");

            foreach (Instrument i in Allocation.Keys.OrderByDescending(k => Allocation[k]))
            {
                plotter.SetX(i.Name);
                plotter.Plot("Symbol", i.Symbol);
                plotter.Plot("Allocation", string.Format("{0:P2}", Allocation[i]));
            }
        }
    }
}

//==============================================================================
// end of file