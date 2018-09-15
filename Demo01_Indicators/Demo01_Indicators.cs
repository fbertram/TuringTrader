//==============================================================================
// Project:     Trading Simulator
// Name:        Demo01_Indicators
// Description: demonstrate use of indicators
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
#endregion

namespace FUB_TradingSim
{
    class Demo01_Indicators : Algorithm
    {
        #region internal data
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly double _initialCash = 100000.00;
        private readonly string _instrumentNick = "^GSPC.Index";
        private readonly double _offsetPrice = -1800.0;
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2015");
            EndTime = DateTime.Parse("12/31/2016");

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

                // calculate simple indicators
                // the output of an indicator is a time series
                ITimeSeries<double> ema26 = instrument.Close.EMA(26);
                ITimeSeries<double> ema12 = instrument.Close.EMA(12);

                // indicators can be calculated on top indicators
                ITimeSeries<double> macd = ema12.Subtract(ema26);
                ITimeSeries<double> signal = macd.EMA(9);

                // plot our data
                _plotter.SelectPlot("indicator vs time", "date");
                _plotter.SetX(simTime);
                _plotter.Log(instrument.Symbol, instrument.Close[0] + _offsetPrice);
                _plotter.Log("ema26", ema26[0] + _offsetPrice);
                _plotter.Log("ema12", ema12[0] + _offsetPrice);
                _plotter.Log("macd", macd[0]);
                _plotter.Log("signal", signal[0]);
            }
        }

        #region miscellaneous stuff
        public Demo01_Indicators()
        {
        }

        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelPath);
        }

        static void Main(string[] args)
        {
            var algo = new Demo01_Indicators();
            algo.Run();
            algo.CreateChart();
        }
        #endregion
    }
}

//==============================================================================
// end of file