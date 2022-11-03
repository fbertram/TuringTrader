//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A05_Universes
// Description: Develop & test universes.
// History:     2022x27, FUB, created
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

#region libraries
using System;
using System.Globalization;
using System.Linq;
using TuringTrader.SimulatorV2;
#endregion

// NOTE: indicators work the same way as assets. They can be introduced at
// any point of the simulation, and their results are cached. Because
// indicators are processed in their separate tasks, they can run in
// parallel, making good use of your CPU's cores.

namespace TuringTrader.DemoV2
{
    public class A05_Universe : Algorithm
    {
        public override string Name => "A05_Universe";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture);
            EndDate = DateTime.Parse("09/15/2020", CultureInfo.InvariantCulture);

            SimLoop(() =>
            {
                var test = Universe("$DJI"); // Dow-Jones
                Output.WriteLine("{0:MM/dd/yyyy}, {1} constituents: {2}",
                    SimDate, test.Count(),
                    test.Aggregate("", (acc, it) => acc + ", " + it.Replace("norgate#accept_no_data:", "")));
            });
        }

        public override void Report() => Output.WriteLine("Here is your report");
    }
}

//==============================================================================
// end of file
