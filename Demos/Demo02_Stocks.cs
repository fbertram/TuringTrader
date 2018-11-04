//==============================================================================
// Project:     Trading Simulator
// Name:        Demo02_Stocks
// Description: demonstrate simple stock trading
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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
        private Logger _plotter = new Logger();
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
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
            DataSources.Add(DataSource.New(_instrumentNick));

            // set account value
            Cash = _initialCash;

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

                _plotter.SelectPlot("nav & benchmark vs time", "date");
                _plotter.SetX(simTime);
                _plotter.Log(instrument.Symbol, instrument.Close[0] / (double)_initialPrice);
                _plotter.Log("MA Crossover", NetAssetValue[0] / _initialCash);
            }
        }

        override public void Report()
        {
            _plotter.OpenWithExcel(_excelPath);
        }
    }
}

//==============================================================================
// end of file