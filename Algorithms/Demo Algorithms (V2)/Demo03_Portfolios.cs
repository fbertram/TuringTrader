//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo03_Portfolios
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

using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;

namespace TuringTrader.Demos
{
    public class Demo03_Portfolios: Algorithm
    {
        override public string Name => "Simple Asset Rotation";

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            // note that our simulation starts long before inception 
            // of the ETFs thanks to TuringTrader's backfills
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Now;
            WarmupPeriod = TimeSpan.FromDays(365);

            //---------- simulation

            SimLoop(() =>
            {
#if true
                // retrieve the constituents of the Dow-30
                // the consituents will change over time,
                // if your data feed supports it
                var universe = Universe("$DJI");
                var benchmark = Asset("$DJI");
#else
                // alternatively, we can use a static list
                // note that this introduces survivorship bias
                var universe = new List<string>{
                    "UNH",   // 1   UnitedHealth Group Incorporated
                    "GS",    // 2   Goldman Sachs Group Inc.
                    "HD",    // 3   Home Depot Inc.
                    "AMGN",  // 4   Amgen Inc.
                    "MCD",   // 5   McDonald's Corporation
                    "MSFT",  // 6   Microsoft Corporation
                    "CAT",   // 7   Caterpillar Inc.
                    "HON",   // 8   Honeywell International Inc.
                    "V",     // 9   Visa Inc. Class A
                    "TRV",   // 10  Travelers Companies Inc.
                    "CVX",   // 11  Chevron Corporation
                    "BA",    // 12  Boeing Company
                    "JNJ",   // 13  Johnson & Johnson
                    "CRM",   // 14  Salesforce Inc.
                    "AXP",   // 15  American Express Company
                    "WMT",   // 16  Walmart Inc.
                    "PG",    // 17  Procter & Gamble Company
                    "IBM",   // 18  International Business Machines Corporation
                    "AAPL",  // 19  Apple Inc.
                    "JPM",   // 20  JPMorgan Chase & Co.
                    "MMM",   // 21  3M Company
                    "MRK",   // 22  Merck & Co. Inc.
                    "NKE",   // 23  NIKE Inc. Class B
                    "DIS",   // 24  Walt Disney Company
                    "KO",    // 25  Coca-Cola Company
                    "DOW",   // 26  Dow Inc.
                    "CSCO",  // 27  Cisco Systems Inc.
                    "WBA",   // 28  Walgreens Boots Alliance Inc.
                    "VZ",    // 29  Verizon Communications Inc.
                    "INTC",  // 30  Intel Corporation
                };
                var benchmark = Asset("$DJI");
#endif

                // this strategy trades only once per week
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    // pick the top 33%  of assets with the highest 6-month momentum
                    var topAssets = universe
                        .OrderByDescending(name => Asset(name).Close.LogReturn().EMA(126)[0])
                        .Take((int)Math.Round(universe.Count * 0.33));

                    // we need to create a list of all assets, as
                    // constituents of the universe can change over time
                    var allAssets = universe
                        .Concat(Positions.Keys)
                        .Distinct()
                        .ToList();

                    // hold only the top-ranking assets, flatten all other positions
                    foreach (var name in allAssets)
                        Asset(name).Allocate(
                            topAssets.Contains(name) ? 1.0 / topAssets.Count() : 0.0,
                            OrderType.openNextBar);
                }

                // create a simple report comparing the
                // trading strategy to its benchmark
                Plotter.SelectChart("simple asset rotation", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot(Name, NetAssetValue);
                Plotter.Plot(benchmark.Description, benchmark.Close[0]);
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