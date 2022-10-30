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

        #region inputs
        [OptimizerParam(5, 50, 5)]
        public int FAST_PERIOD { get; set; } = 50;

        [OptimizerParam(63, 252, 21)]
        public int SLOW_PERIOD { get; set; } = 200;

#if true
        private const string UNIVERSE = "$DJI";
        private const string BENCH = "$DJITR";
#else
        private const string UNIVERSE = "$SPX";
        private const string BENCH = "$SPXTR";
#endif
        private const string SAFE = "BIL";
        #endregion
        #region strategy logic
        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndDate = DateTime.Now;

            SimLoop(() =>
            {
                if (!IsOptimizing)
                {
                    // main strategy chart
                    Plotter.SelectChart(string.Format("Moving Average Crossover on {0}", UNIVERSE), "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot(BENCH, Asset(BENCH).Close[0]);
                }

                // rebalance once per week
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    // initially assume we close all current positions
                    var weights = Positions
                        .ToDictionary(kv => kv.Key, kv => 0.0);

                    // adjust positions for all constituents
                    var constituents = Universe(UNIVERSE);
                    foreach (var ticker in constituents)
                    {
                        var price = Asset(ticker).Close;
                        var maFast = price.EMA(FAST_PERIOD);
                        var maSlow = price.EMA(SLOW_PERIOD);

                        if (maFast[0] > maSlow[0])
                            weights[ticker] = 1.0 / constituents.Count;
                    }

                    // set position for safe asset
                    var totalWeight = weights.Sum(kv => kv.Value);
                    if (SAFE != null)
                        weights[SAFE] = 1.0 - totalWeight;

                    // submit orders
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, OrderType.openNextBar);

                    if (!IsOptimizing)
                    {
                        // asset allocation
                        Plotter.SelectChart("Asset Allocation", "Date");
                        Plotter.SetX(SimDate);
                        foreach (var ticker in Positions.Keys)
                            Plotter.Plot(Asset(ticker).Description, Asset(ticker).Position);
                        Plotter.Plot("Cash", Cash);
                    }
                }
            });
        }
        public override void Report() => Plotter.OpenWith("SimpleReport");
        #endregion
    }
}

//==============================================================================
// end of file
