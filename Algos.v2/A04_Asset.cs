//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A04_Asset
// Description: Develop & test asset import and alignment w/ trading calendar.
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

#region libraries
using System;
using System.Globalization;
using TuringTrader.SimulatorV2;
#endregion

// NOTE: with v2, it is now possible to add a new data source at any point in
// the simulation. This will ease the implementation of universes later on.
// The example below shows how the asset is brought in on every bar - but it
// will be served from the cache in all but the very first call.

namespace TuringTrader.DemoV2
{
    public class A04_Asset : Algorithm
    {
        public override string Name => "A04_Asset";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2021", CultureInfo.InvariantCulture);
            EndDate = DateTime.Parse("05/01/2021", CultureInfo.InvariantCulture);

            SimLoop(() =>
            {
                var test = Asset("SPY");
                Output.WriteLine("{0:MM/dd/yyyy}, {1}: {2}", SimDate, test.Name, test[0]);
            });
        }

        public override void Report() => Output.WriteLine("Here is your report");
    }
}

//==============================================================================
// end of file
