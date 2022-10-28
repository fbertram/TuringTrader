//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A09_Orders2
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
using System.Globalization;
using System.Linq;
#endregion

// NOTE: creating reports works the same as with the v1 engine

namespace TuringTrader.Simulator.v2.Demo
{
    public class A09_Orders2 : Algorithm
    {
        public override string Name => "A09_Orders2";

        [OptimizerParam(5, 50, 5)]
        public int FAST_PERIOD { get; set; } = 50;

        [OptimizerParam(63, 252, 21)]
        public int SLOW_PERIOD { get; set; } = 200;

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndDate = DateTime.Now;

            var universe = "$DJI";

            SimLoop(() =>
            {
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    var constituents = Universe(universe);

                    foreach (var position in Positions)
                    {
                        if (constituents.Where(t => t == position.Key).Count() == 0)
                            Asset(position.Key).Allocate(0.0, OrderType.BuySellThisClose);
                    }

                    foreach (var ticker in constituents)
                    {
                        var asset = Asset(ticker);
                        var price = asset.Close;
                        var maFast = price.EMA(FAST_PERIOD);
                        var maSlow = price.EMA(SLOW_PERIOD);
                        var weight = maFast[0] > maSlow[0] ? 1.0 / constituents.Count() : 0.0;
                        asset.Allocate(weight, OrderType.BuySellThisClose);
                    }
                }

                if (!IsOptimizing)
                {
                    Plotter.SelectChart(string.Format("Moving Average Crossover on {0}", universe), "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot("$SPXTR", Asset("$SPXTR").Close[0]);
                }
            });
        }

        public override void Report() => Plotter.OpenWith("SimpleReport");
    }
}

//==============================================================================
// end of file
