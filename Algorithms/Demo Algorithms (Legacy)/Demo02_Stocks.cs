//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo02_Stocks
// Description: demonstrate simple stock trading
// History:     2018ix15, FUB, created
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
using TuringTrader.Indicators;
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
        private readonly string _instrumentNick = "AAPL";
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("08/01/2018", CultureInfo.InvariantCulture);

            // add instruments
            AddDataSource(_instrumentNick);

            // set account value
            Deposit(_initialCash);

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
                _plotter.SetX(simTime.Date);
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