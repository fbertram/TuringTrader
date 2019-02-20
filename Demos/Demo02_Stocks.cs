//==============================================================================
// Project:     Trading Simulator
// Name:        Demo02_Stocks
// Description: demonstrate simple stock trading
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Demos
{
    public class Demo02_Stocks : Algorithm
    {
        #region internal data
        private Plotter _plotter = new Plotter();
        private readonly string _template = "SimpleChart";
        private readonly double _initialCash = 100000.00;
        private double? _initialPrice = null;
        private readonly string _instrumentNick = "AAPL.Stock";
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("08/01/2018");

            // add instruments
            AddDataSource(_instrumentNick);

            // set account value
            Deposit(_initialCash);

            // clear plotters
            _plotter.Clear();

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                // find our instrument. 
                Instrument instrument = FindInstrument(_instrumentNick);

                // calculate moving averages
                ITimeSeries<double> slow = instrument.Close.EMA(63);
                ITimeSeries<double> fast = instrument.Close.EMA(21);

                // determine current and target position size,
                // based on a simple moving average crossover strategy
                int currentPosition = instrument.Position;
                int targetPosition = fast[0] > slow[0]
                    ? (int)Math.Floor(NetAssetValue[0] / instrument.Close[0]) // go long
                    : 0;                                                      // short... disabled for this demo

                // place trades
                instrument.Trade(targetPosition - currentPosition, OrderType.openNextBar);

                // plot net asset value versus benchmark
                if (_initialPrice == null) _initialPrice = instrument.Close[0];

                _plotter.SelectChart("nav & benchmark vs time", "date");
                _plotter.SetX(simTime);
                _plotter.Plot(instrument.Symbol, instrument.Close[0] / (double)_initialPrice);
                _plotter.Plot("MA Crossover", NetAssetValue[0] / _initialCash);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith(_template);
        }
    }
}

//==============================================================================
// end of file