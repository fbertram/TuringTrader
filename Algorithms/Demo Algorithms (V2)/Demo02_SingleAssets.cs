//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo02_SingleAssets
// Description: demonstrate simple single-asset trading
// History:     2018ix15, FUB, created
//              2023iii02, FUB, updated for v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;
#endregion

namespace TuringTrader.Demos
{
    public class Demo02_SingleAssets : Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2018-12-31T16:00-05:00");
            // note that the warmup period is specified in calendar days
            // while most indicators express their parameters in trading days
            WarmupPeriod = TimeSpan.FromDays(90);            

            // set the account friction to 0.5% of transaction volume
            ((Account_Default)Account).Friction = 0.005;

            //---------- simulation

            SimLoop(() =>
            {
                // first, we load the asset quotes
                // then, we calculate slow and fast moving averages
                // note that this code can live inside or outside of SimLoop
                var asset = Asset("AAPL");
                var slow = asset.Close.EMA(63);
                var fast = asset.Close.EMA(21);

                // set the asset allocation as a percentage of the NAV
                // no need to worry about number of shares, trade direction
                asset.Allocate(
                    // hold the asset while the fast MA is above the slow MA,
                    fast[0] > slow[0] ? 1.0 : 0.0,
                    // we set the order to fill on tomorrow's open
                    OrderType.openNextBar);

                // create a simple report comparing the
                // trading strategy to buy & hold
                Plotter.SelectChart("moving average crossover", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("trading strategy", NetAssetValue);
                Plotter.Plot("buy & hold", asset.Close[0]);

                // on the last bar of the simulation,
                // we print the asset allocation to the console
                if (IsLastBar)
                {
                    Output.WriteLine("Final Asset Allocation");
                    foreach (var position in Positions)
                        Output.WriteLine("{0} ({1})= {2:P2}", 
                            Asset(position.Key).Description, 
                            Asset(position.Key).Ticker, 
                            position.Value);
                    Output.WriteLine("Idle Cash = {0:P2}", Cash);
                }
            });

            // this code adds the target allocation, historical
            // allocations, and the trade log to the report
            Plotter.AddTargetAllocation();
            Plotter.AddHistoricalAllocations();
            Plotter.AddTradeLog();
        }

        // if we don't override Report here, a default report
        // using the SimpleReport template will be created
    }
}

//==============================================================================
// end of file