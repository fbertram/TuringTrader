//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo03_Portfolio
// Description: portfolio trading demo
// History:     2018xii10, FUB, created
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
    public class Demo03_Portfolio: Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            // note that our simulation starts long before inception 
            // of the ETFs thanks to TuringTrader's backfills
            StartDate = DateTime.Parse("1990-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");
            WarmupPeriod = TimeSpan.FromDays(365);

            // setup the trading universe. note that we are 
            // not using strings here, but pre-defined constants,
            // so that we can benefit from TuringTrader's backfills
            var tickers = new List<string>{
                ETF.XLY, ETF.XLV, ETF.XLK,
                ETF.XLP, ETF.XLE, ETF.XLI,
                ETF.XLF, ETF.XLU, ETF.XLB,
            };

            //---------- simulation

            SimLoop(() =>
            {
                // this algorithm trades only once per week
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    // pick the top 3 assets with the highest 1-year momentum
                    var topAssets = tickers
                        .OrderByDescending(ticker => Asset(ticker).Close[0] / Asset(ticker).Close[252])
                        .Take(3);

                    // let's first assume we close all open positions
                    var weights = Positions
                        .ToDictionary(
                            kv => kv.Key,
                            kv => 0.0);

                    // now allocate capital equally to the top assets
                    foreach (var ticker in topAssets)
                        weights[ticker] = 1.0 / topAssets.Count();

                    // place orders
                    foreach (var kv in weights)
                        Asset(kv.Key).Allocate(kv.Value, OrderType.openNextBar);
                }

                // create a simple report comparing the
                // trading strategy to the S&P 500
                Plotter.SelectChart("simple sector rotation", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("trading strategy", NetAssetValue);
                Plotter.Plot("s&P 500", Asset(MarketIndex.SPX).Close[0]);
            });

            //---------- post-processing

            // add some optional information to the report
            Plotter.AddTargetAllocation();
            Plotter.AddHistoricalAllocations();
            Plotter.AddTradeLog();
        }
    }
}

//==============================================================================
// end of file