//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo08_CustomData
// Description: demonstrate custom data sources
// History:     2019v21, FUB, created
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

namespace Demos
{
    // algorithm acting as a custom data source
    // we are aware that this solution is not ideal
    // and will be releasing a more streamlined solution soon
    class CustomData : Algorithm
    {
        public override void Run()
        {
            //---------- initialization

            // note that we are not setting the simulation period  here
            // the parent algorithm will set StartDate and EndDate for us

            //---------- simulation

            SimLoop(() =>
            {
                var t = (SimDate - DateTime.Parse("1970-01-01")).TotalDays;                
                var v = Math.Sin(Math.PI * t / 180.0);
                return new OHLCV(v, v, v, v, 0.0);
            });
        }
    }

    public class Demo07_ChildAlgos : Algorithm
    {
        public override void Run()
        {
            //---------- initialization

            // set the simulation period
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

            //---------- simulation
                
            SimLoop(() =>
            {
                Plotter.SelectChart("custom data source", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("custom data", Asset("algorithm:CustomData").Close[0]);
            });
        }

        // minimalistic chart
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file