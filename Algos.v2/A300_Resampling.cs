//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        A300_Resampling
// Description: Develop & test time series resampling.
// History:     2022xi20, FUB, created
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

namespace TuringTrader.DemoV2
{
    public class A300_Resampling : Algorithm
    {
        public override string Name => "A300_Resampling";

        public override void Run()
        {
            StartDate = DateTime.Parse("01/01/2022", CultureInfo.InvariantCulture);
            EndDate = DateTime.Parse("12/31/2022", CultureInfo.InvariantCulture);

            SimLoop(() =>
            {
                Plotter.SelectChart(Name, "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot("Daily", Asset("SPY").Close[0]);
                Plotter.Plot("Weekly", Asset("SPY").Close.Weekly()[0]);
                Plotter.Plot("Monthly", Asset("SPY").Close.Monthly()[0]);
                Plotter.Plot("3-mo Avg Daily", Asset("SPY").Close.EMA(63)[0]);
                Plotter.Plot("3-mo Avg Weekly", Asset("SPY").Weekly().Close.EMA(12)[0]);
                Plotter.Plot("3-mo Avg Monthly", Asset("SPY").Monthly().Close.EMA(3)[0]);

                Output.WriteLine("sim: {0:MM/dd/yyyy}, daily: {1:MM/dd/yyyy}={2:C2}, weekly: {3:MM/dd/yyyy}={4:C2}, monthly: {5:MM/dd/yyyy}={6:C2}",
                    SimDate,
                    Asset("SPY").Close.Time[0], Asset("SPY").Close[0],
                    Asset("SPY").Close.Weekly().Time[0], Asset("SPY").Close.Weekly()[0],
                    Asset("SPY").Close.Monthly().Time[0], Asset("SPY").Close.Monthly()[0]);
            });
        }

        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}