//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo06_OrderTypes
// Description: demonstrate various order types & trade log
// History:     2018ix29, FUB, created
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
    public class Demo06_OrderTypes : Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            StartDate = DateTime.Parse("2018-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2018-01-31T16:00-05:00");

            //---------- simulation

            var numDays = 0;
            SimLoop(() =>
            {
                var asset = Asset("$SPX");

                numDays++;
                Output.WriteLine("day #{0} = {1}", numDays, SimDate);
                switch(numDays)
                {
                    //--- market orders
                    case 1: // Tue, 01/02/2018
                        asset.Allocate(0.5, OrderType.closeThisBar); // fill at close of Tue, 01/02
                        break;

                    case 2: // Wed, 01/03/2018
                        asset.Allocate(1.0, OrderType.openNextBar); // fill at open of Thu, 01/04
                        break;

                    //--- stop sell orders
                    case 3: // Thu, 01/04/2018
                        // won't trigger
                    case 4: // Fri, 01/05/2018
                        // fill at open of Mon, 01/08
                    case 5: // Mon, 01/08/2018
                        // fill mid-day of Tue, 01/09

                    //--- stop buy orders
                    case 6: // Tue, 01/09/2018
                        // won't trigger
                    case 7: // Wed, 01/10/2018
                        // fill at open of Thu, 01/11
                    case 8: // Thu, 01/11/2018
                        // fill mid-day of Fri, 01/12
                    case 9: // Fri, 01/12/2018
                        break;

                    //--- limit sell orders
                    case 10: // Mon, 01/16/2018
                        // won't trigger
                    case 11: // Tue, 01/17/2018
                        // fill at open of Wed 01/18
                    case 12: // Wed, 01/18/2018
                        // fill mid-day of Thu, 01/19
                        break;

                    //--- limit buy orders
                    case 13: // Thu, 01/19/2018
                        // won't trigger
                    case 14: // Fri, 01/22/2018
                        // fill at open of Mon, 01/23
                    case 15: // Mon, 01/23/2018
                        // fill mid-day of Tue, 01/24
                        break;

                    //--- short positions
                    case 16: // Tue, 01/24/2018
                        // fill at close of Tue, 01/24
                    case 17: // Wed, 01/25/2018
                        // fill at close of Wed, 01/25

                    //--- spare dates
                    case 18: // Thu, 01/26/2018
                    case 19: // Fri, 01/29/2018
                    case 20: // Mon, 01/30/2018
                    case 21: // Tue, 01/31/2018
                        break;
                }
            });

            //---------- post-processing

            Plotter.AddTradeLog();
        }

        // keep the report to the bare minimum
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}

//==============================================================================
// end of file