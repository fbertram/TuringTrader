//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A08_Orders
// Description: Develop & test order placement.
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TuringTrader.Simulator.v2;
#endregion

// NOTE: creating reports works the same as with the v1 engine

namespace TuringTrader.Simulator.v2.Demo
{
    public class A08_Orders : Algorithm
    {
        public override string Name => "A08_Orders";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndDate = DateTime.Now;

            SimLoop(() =>
            {
                var ticker = "$SPXTR";
                var asset = Asset(ticker);
                var price = asset.Close;
                var ema50 = price.EMA(50);
                var ema200 = price.EMA(200);

                var weight = ema50[0] > ema200[0] ? 1.0 : 0.0;
                asset.Allocate(weight, OrderType.closeThisBar);

                Plotter.SelectChart(string.Format("Moving Average Crossover on {0}", ticker), "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot("Trading", NetAssetValue);
                Plotter.Plot("Buy & Hold", price[0]);

                Plotter.SelectChart(string.Format("{0} Moving Averages", ticker), "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot(price.Name, price[0]);
                Plotter.Plot(ema50.Name, ema50[0]);
                Plotter.Plot(ema200.Name, ema200[0]);
            });
        }

        public override void Report() => Plotter.OpenWith("SimpleReport");
    }
}

//==============================================================================
// end of file
