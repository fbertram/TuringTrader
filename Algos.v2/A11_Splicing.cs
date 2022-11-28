//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A011_Splicing
// Description: Develop & test data splicing.
// History:     2022xi28, FUB, created
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
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
#endregion

// NOTE: creating reports works the same as with the v1 engine

namespace TuringTrader.DemoV2
{
    public class A11_Splicing : Algorithm
    {
        public override string Name => "A11_Splicing";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture);
            EndDate = DateTime.Now - TimeSpan.FromDays(5);

            SimLoop(() =>
            {
                var ticker = "splice:VOO,$SPXTR";
                var price = Asset(ticker).Close;
                var ema50 = price.EMA(50);
                var ema200 = price.EMA(200);

                Plotter.SelectChart("Backfill/ Splicing", "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot(price.Name, price[0]);
            });
        }

        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file
