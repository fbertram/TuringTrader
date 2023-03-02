//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo01_Indicators
// Description: demonstrate use of indicators
// History:     2018ix15, FUB, created
//              2023iii02, FUB, updated for v2 engine
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC
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
    public class Demo01_Indicators : Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // we start by setting a simulation range
            // note that this range is specified in the local time zone
            StartDate = DateTime.Parse("2015-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2016-12-31T16:00-05:00");

            // the warmup period makes sure indicators have valid values
            // throughout the simulation range StartDate... EndDate
            WarmupPeriod = TimeSpan.FromDays(50);            

            //---------- simulation

            // SimLoop loops through all timestamps in the range
            SimLoop(() =>
            {
                // we bring in quotes for an instrument
                // the output of Asset is a time series
                // note that we can also use the Asset function 
                // outside the SimLoop
                var asset = Asset(ETF.SPY);

                // assets have open, high, low, and closing prices
                // these are, of course, also time series
                var prices = asset.Close;

                // indicators can be applied to any time series
                // they are calculated as separate tasks when first 
                // called, and cached for subsequent use
                // note that we can also calculate indicators
                // outside the SimLoop
                var ema26 = prices.EMA(26);
                var ema12 = prices.EMA(12);

                // indicators can also be applied on top of indicators
                var macd = ema12.Sub(ema26);
                var signal = macd.EMA(9);

                // data from time series can be accessed relative to the
                // simulator's current position using square brackets
                // we can create custom charts, with the Plotter object
                var offset = -150;
                Plotter.SelectChart("indicators vs time", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot(asset.Description, prices[0] + offset);
                Plotter.Plot("ema26", ema26[0] + offset);
                Plotter.Plot("ema12", ema12[0] + offset);
                Plotter.Plot("macd", macd[0]);
                Plotter.Plot("signal", signal[0]);
            });
        }

        // to render the charts, we use pre-defined templates
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file