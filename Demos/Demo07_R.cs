//==============================================================================
// Project:     Trading Simulator
// Name:        Demo07_R
// Description: demonstrate use of R
// History:     2018x17, FUB, created
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

namespace TuringTrader.Demos
{
    class Demo07_R : Algorithm
    {
        #region internal data
        private Logger _plotter = new Logger();
        private readonly double _initialCash = 100000.00;
        private readonly string _instrumentNick = "^SPX.Index";
        private readonly string _template = "SimpleChart";
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2015");
            EndTime = DateTime.Parse("12/31/2016");

            // set account value
            Deposit(_initialCash);

            // add instruments
            DataSources.Add(DataSource.New(_instrumentNick));

            // reset plotters
            _plotter.Clear();

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                Instrument instr = FindInstrument(_instrumentNick);

                _plotter.SelectPlot("Price", "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Log("price", instr.Close[0]);
                _plotter.Log("ema", instr.Close.EMA(200)[0]);

                _plotter.SelectPlot("Drawdown", "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Log("dd", instr.Close[0] / instr.Close.Highest(252)[0] - 1.0);
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