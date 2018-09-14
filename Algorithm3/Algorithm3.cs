//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm1
// Description: Sample Algorithm #1
// History:     2018ix10, FUB, created
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
#endregion

namespace FUB_TradingSim
{
    class Algorithm3 : Algorithm
    {
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly double _initialCash = 100000.00;
        private readonly string _instrumentNick = "^GSPC.Index";

        public Algorithm3()
        { }

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("08/01/2018");

            // set account value
            Cash = _initialCash;

            // add instruments
            DataPath = _dataPath;
            DataSources.Add(DataSource.New(_instrumentNick));

            //---------- simulation

            foreach (DateTime simTime in SimTime)
            {
                // find our instrument. if we have only one instrument, 
                // we could also just use Instrument[0]
                Instrument instrument = FindInstruments(_instrumentNick).First();

                // calculate a simple indicator
                // note that the output is again a time series
                ITimeSeries<double> indicatorSeries = instrument.Close.EMA(126);
                double indicatorValue = indicatorSeries[0];

                // calculate an indicator on top of another indicator
                // we use the output of a previous indicator as input to the next
                ITimeSeries<double> indicatorOnIndicatorSeries = indicatorSeries.EMA(126);
                double indicatorOnIndicatorValue = indicatorOnIndicatorSeries[0];

                // plot our data
                _plotter.SelectPlot("indicator vs time", "date");
                _plotter.SetX(simTime);
                _plotter.Log(instrument.Symbol, instrument.Close[0]);
                _plotter.Log("Indicator", indicatorValue);
                _plotter.Log("Indicator on Indicator", indicatorOnIndicatorValue);
            }
        }

        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelPath);
        }

        static void Main(string[] args)
        {
            var algo = new Algorithm3();
            algo.Run();
            algo.CreateChart();
        }
    }
}

//==============================================================================
// end of file