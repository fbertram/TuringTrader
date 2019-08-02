//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo06_OrderTypes
// Description: demonstrate various order types & trade log
// History:     2018ix29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2098, Bertram Solutions LLC
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Demos
{
    public class Demo06_OrderTypes : Algorithm
    {
        #region internal data
        private Plotter _plotter = new Plotter();
        private readonly string _template = "SimpleChart";
        private readonly double _initialCash = 100000.00;
        private readonly string _instrumentNick = "$SPX";
        #endregion

        override public void Run()
        {
            //---------- initialization

            StartTime = DateTime.Parse("01/01/2018", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("08/01/2018", CultureInfo.InvariantCulture);

            // set account value
            Deposit(_initialCash);
            CommissionPerShare = 0.015;

            // add instruments
            AddDataSource(_instrumentNick);

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                Instrument instrument = FindInstrument(_instrumentNick);

                //===== market order, open next bar
                // put in on Friday, this will execute Monday morning
                if (simTime.Date == DateTime.Parse("01/05/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.openNextBar);

                //===== market order, close this bar
                // put in on on Friday, this will execute Friday
                // be mindful of data-snooping when using this!
                if (simTime.Date == DateTime.Parse("01/12/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(-100, OrderType.closeThisBar);

                //===== stop order
                // will turn into market order, if stop price is hit
                // stop price is always 'worse' than current price
                if (simTime.Date == DateTime.Parse("01/12/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.stopNextBar, 3000); // won't trigger

                if (simTime.Date == DateTime.Parse("01/16/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.stopNextBar, 2780); // triggers at open

                if (simTime.Date == DateTime.Parse("01/17/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(-100, OrderType.stopNextBar, 2000); // won't trigger

                if (simTime.Date == DateTime.Parse("01/18/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(-100, OrderType.stopNextBar, 2799); // triggers mid day

                //===== limit order
                if (simTime.Date == DateTime.Parse("01/19/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.limitNextBar, 2750); // won't trigger

                if (simTime.Date == DateTime.Parse("01/22/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.limitNextBar, 2850); // triggers at open

                if (simTime.Date == DateTime.Parse("01/23/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(-100, OrderType.limitNextBar, 2860); // won't trigger

                if (simTime.Date == DateTime.Parse("01/24/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(-100, OrderType.limitNextBar, 2840); // triggers mid day

                //===== conditional order
                if (simTime.Date == DateTime.Parse("01/25/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.openNextBar, 0.0, i => true); // will be executed 01/26

                if (simTime.Date == DateTime.Parse("01/26/2018", CultureInfo.InvariantCulture))
                    instrument.Trade(100, OrderType.openNextBar, 0.0, i => false); // won't trigger
            }

            //---------- post-processing

            _plotter.SelectChart("trades", "time");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("order", entry.OrderTicket.Type.ToString());
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("price", entry.OrderTicket.Price);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                _plotter.Plot("commission", -entry.Commission);
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