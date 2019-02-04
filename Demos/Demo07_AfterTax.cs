//==============================================================================
// Project:     Trading Simulator
// Name:        Demo07_AfterTax
// Description: Demonstrate after-tax simulation
// History:     2019ii02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace Demos
{
    public class Demo07_AfterTax : Algorithm
    {
        private static readonly string STOCKS = "VTI.etf";
        private static readonly string BONDS = "BND.etf";
        private static readonly string DEBUG_REPORT = "SimpleChart";
        private Plotter _plotter = new Plotter();

        override public void Run()
        {
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("12/31/2018, 4pm");

            Deposit(25000);

            AddDataSource(STOCKS);
            AddDataSource(BONDS);

            foreach (DateTime simTime in SimTimes)
            {
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    Deposit(500);
                }

                foreach (Instrument instrument in Instruments)
                {
                    double targetPercentage = 0.0;
                    if (instrument.Nickname == STOCKS) targetPercentage = 0.6;
                    if (instrument.Nickname == BONDS) targetPercentage = 0.4;

                    int targetShares = (int)Math.Floor(targetPercentage * NetAssetValue[0] / instrument.Close[0]);
                    int deltaShares = targetShares - instrument.Position;
                    instrument.Trade(deltaShares);
                }

                _plotter.SelectChart("Before tax", "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot("NAV", NetAssetValue[0]);
                _plotter.Plot("DD", 1.0 - NetAssetValue[0] / NetAssetValueHighestHigh);
            }

            AfterTaxSimulation.Run(this);
        }

        override public void Report()
        {
            _plotter.OpenWith(DEBUG_REPORT);
        }
    }
}

//==============================================================================
// end of file