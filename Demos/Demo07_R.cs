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
#endregion

namespace FUB_TradingSim
{
    class Demo07_R : Algorithm
    {
        #region internal data
        private Logger _plotter = new Logger();
        private readonly double _initialCash = 100000.00;
        private readonly string _instrumentNick = "^SPX.Index";
        #endregion

        public double FractionalYears(DateTime time)
        {
            int year = time.Date.Year;
            DateTime jan1 = new DateTime(year, 1, 1);
            DateTime dec31 = new DateTime(year, 12, 31);
            double fraction = (time - jan1).TotalDays / (dec31 - jan1).TotalDays;

            return year + fraction;
        }

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2015");
            EndTime = DateTime.Parse("12/31/2016");

            // set account value
            Cash = _initialCash;

            // add instruments
            DataSources.Add(DataSource.New(_instrumentNick));

            // reset plotters
            _plotter.Clear();

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                Instrument instr = FindInstrument(_instrumentNick);

                _plotter.SelectPlot("Price", "date");
                _plotter.SetX(FractionalYears(SimTime[0]));
                _plotter.Log("price", instr.Close[0]);
                _plotter.Log("ema", instr.Close.EMA(200)[0]);

                _plotter.SelectPlot("Drawdown", "date");
                _plotter.SetX(FractionalYears(SimTime[0]));
                _plotter.Log("dd", instr.Close[0] / instr.Close.Highest(252)[0] - 1.0);
            }
        }

        override public void Report()
        {
            _plotter.OpenWithR();
        }
    }
}

//==============================================================================
// end of file