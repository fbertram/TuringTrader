//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo01_Indicators
// Description: demonstrate use of indicators
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
    public class Demo01_Indicators : Algorithm
    {
        #region internal data
        private Plotter _plotter = new Plotter();
        private readonly string _template = "SimpleChart";
        private readonly string _instrumentNick = "$SPX.index";
        private readonly double _offsetPrice = -1800.0;
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2015");
            EndTime = DateTime.Parse("12/31/2016");

            // add instruments
            AddDataSource(_instrumentNick);

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                // find our instrument. if we have only one instrument, 
                // we could also just use Instruments.Values.First()
                Instrument instrument = FindInstrument(_instrumentNick);

                // calculate simple indicators
                // the output of an indicator is a time series
                ITimeSeries<double> ema26 = instrument.Close.EMA(26);
                ITimeSeries<double> ema12 = instrument.Close.EMA(12);

                // therefore, indicators can be calculated on top of indicators
                ITimeSeries<double> macd = ema12.Subtract(ema26);
                ITimeSeries<double> signal = macd.EMA(9);

                // plot our data
                _plotter.SelectChart("indicators vs time", "date");
                _plotter.SetX(simTime.Date);
                _plotter.Plot(instrument.Symbol, instrument.Close[0] + _offsetPrice);
                _plotter.Plot("ema26", ema26[0] + _offsetPrice);
                _plotter.Plot("ema12", ema12[0] + _offsetPrice);
                _plotter.Plot("macd", macd[0]);
                _plotter.Plot("signal", signal[0]);
            }
        }

        public override void Report()
        {
            // open the plot with Excel, or R
            _plotter.OpenWith(_template);
        }
    }
}

//==============================================================================
// end of file